module ProGuitarWriter

open Rocksmith2014.XML
open System
open System.IO
open BinaryFileWriter
open NoteConverter
open EOFTypes
open SectionWriter

let writeEmptyProGuitarTrack (name: string) =
    binaryWriter {
        name
        4uy // format
        5uy // behaviour
        9uy // type
        -1y // difficulty level
        4u // flags
        0us // compliance flags

        24uy // highest fret
        let strings = if name.Contains("BASS") then 4uy else 6uy
        strings // strings
        Array.replicate (int strings) 0uy // tuning

        0u // notes
        0us // number of sections
        0u // custom data blocks
    }

let customDataBlock (blockId: uint) (data: byte array) =
    binaryWriter {
        // Custom data block size (+ 4 bytes for block ID)
        data.Length + 4
        blockId
        data
    }

[<return: Struct>]
let (|Combinable|_|) (a: EOFNote) (b: EOFNote) =
    if a.Position = b.Position
        && a.BendStrength = b.BendStrength
        && a.SlideEndFret = b.SlideEndFret
        && a.UnpitchedSlideEndFret = b.UnpitchedSlideEndFret
        && a.Flags = b.Flags
        && a.ExtendedNoteFlags = b.ExtendedNoteFlags
    then
        ValueSome b
    else
        ValueNone

let combineTechNotes (techNotes: EOFNote array) =
    let combiner current acc =
        match acc with
        | (Combinable current prev) :: tail ->
            let combined =
                { current with
                    BitFlag = current.BitFlag ||| prev.BitFlag
                    Frets = current.Frets |> Array.append prev.Frets
                    ExtendedNoteFlags = current.ExtendedNoteFlags ||| prev.ExtendedNoteFlags }
            combined :: tail
        | [] ->
            [ current ]
        | _ ->
            current :: acc

    // TODO
    let separator a acc =
        match acc with
        | b :: tail when a.Position = b.Position ->
            let b2 =
                { b with Position = b.Position + 50u }
            a :: b2 :: tail
        | [] ->
            [ a ]
        | _ ->
            a :: acc

    techNotes
    |> Seq.sortBy (fun x -> x.Position)
    |> fun s -> Seq.foldBack combiner s []
    |> fun s -> Seq.foldBack separator s []
    |> List.toArray

let convertAnchors (level: Level) =
    level.Anchors.ToArray()
    |> Array.map (fun a -> EOFSection.Create(0uy, uint a.Time, uint a.Fret, 0u))

let handShapeNotNeeded isArpeggio (notesInHs: EOFNote array) =
    let b = notesInHs |> Array.tryHead |> Option.map (fun x -> x.BitFlag)
    match b with
    | None ->
        // Empty handshape: always include
        false
    | Some b ->
        // Include if arpeggio or all the chords are the same in the handshape
        not isArpeggio
        && notesInHs |> Array.forall (fun n -> n.BitFlag = b && n.Flags &&& EOFNoteFlag.SPLIT = EOFNoteFlag.ZERO)

type HsResult =
    | AdjustSustains of (uint * uint) array
    | SectionCreated of EOFSection

let convertHandShapes (inst: InstrumentalArrangement) (notes: EOFNote array) (level: Level) =
    level.HandShapes.ToArray()
    |> Array.map (fun hs ->
        let notesInHs =
            notes
            |> Array.filter (fun n -> int n.Position >= hs.StartTime && int n.Position < hs.EndTime)

        let isArpeggio = inst.ChordTemplates[int hs.ChordId].IsArpeggio

        if handShapeNotNeeded isArpeggio notesInHs then
            let updates =
                notesInHs
                |> Array.mapi (fun i n ->
                    match notesInHs |> Array.tryItem (i + 1) with
                    | Some next ->
                        n.Position, next.Position - n.Position - 5u
                    | None ->
                        n.Position, uint hs.EndTime - n.Position)

            AdjustSustains updates
        else
            SectionCreated <| EOFSection.Create(0uy, uint hs.StartTime, uint hs.EndTime, if isArpeggio then 0u else 2u)
    )

let convertTones (inst: InstrumentalArrangement) =
    inst.Tones.Changes.ToArray()
    |> Array.map (fun t ->
        let endTime = if t.Name = inst.Tones.BaseToneName then 1u else 0u
        { EOFSection.Create(255uy, uint t.Time, endTime, 0u) with Name = t.Name })

let getArrangementType (inst: InstrumentalArrangement) =
    match inst.MetaData.Arrangement.ToLowerInvariant() with
    | "combo" -> 1uy
    | "rhythm" -> 2uy
    | "lead" -> 3uy
    | "bass" -> 4uy
    | _ -> 0uy

type TempTremoloSection =
    { PrevIndex: int
      StartTime: uint
      EndTime: uint }

let createTremoloSections (notes: EOFNote array) =
    notes
    |> Array.indexed
    |> Array.fold (fun acc (i, note) ->
        if note.Flags &&& EOFNoteFlag.TREMOLO = EOFNoteFlag.ZERO then
            acc
        else
            match acc with
            | h :: t when h.PrevIndex = i - 1 ->
                // Extend previous tremolo section
                { h with PrevIndex = i; EndTime = note.Position + note.Length } :: t
            | _ ->
                // Create new tremolo section
                { StartTime = note.Position; PrevIndex = i; EndTime = note.Position + note.Length } :: acc
    ) []
    |> List.rev
    |> List.map (fun x -> EOFSection.Create(0uy, x.StartTime, x.EndTime, 0u))
    |> List.toArray

let writeProTrack (inst: InstrumentalArrangement) =
    let notes, fingeringData, techNotes =
        convertNotes inst inst.Levels[0]
        |> Array.unzip3

    let tones = convertTones inst
    let anchors = convertAnchors inst.Levels[0]
    let handShapeResult = convertHandShapes inst notes inst.Levels[0]
    let notes =
        let updates =
            handShapeResult
            |> Array.collect (function AdjustSustains s -> s | _ -> Array.empty)
            |> readOnlyDict

        notes
        |> Array.map (fun n ->
            match updates.TryGetValue n.Position with
            | true, length -> { n with Length = length }
            | false, _ -> n)

    let handShapes = handShapeResult |> Array.choose (function SectionCreated s -> Some s | _ -> None)

    let fingeringData = fingeringData |> Array.concat
    let techNotes =
        techNotes
        |> Array.concat
        |> combineTechNotes

    let techNotesData =
        if techNotes.Length > 0 then
            use m = new MemoryStream()
            binaryWriter { techNotes } |> toStream(m)
            m.ToArray()
        else
            Array.empty

    let tremoloSections = createTremoloSections notes

    let sectionCount =
        [ anchors; handShapes; tones; tremoloSections ]
        |> List.sumBy (fun x -> Convert.ToUInt16(x.Length > 0))

    let customDataBlockCount =
        let fingering = if fingeringData.Length > 0 then 1u else 0u
        let capo = if inst.MetaData.Capo > 0y then 1u else 0u
        let tech = if techNotesData.Length > 0 then 1u else 0u
        2u + fingering + capo + tech

    let trackFlag =
        let ap = inst.MetaData.ArrangementProperties
        flags {
            EOFTrackFlag.UNLIMITED_DIFFS
            if ap.BassPick then EOFTrackFlag.RS_PICKED_BASS
            if ap.BonusArrangement then
                EOFTrackFlag.RS_BONUS_ARR
            elif not ap.Represent then
                EOFTrackFlag.RS_ALT_ARR
        }

    binaryWriter {
        "PART REAL_GUITAR"
        4uy // format
        5uy // behaviour
        9uy // type
        -1y // difficulty level
        trackFlag |> uint // flags
        0us // compliance flags

        24uy // highest fret
        6uy // strings
        inst.MetaData.Tuning.Strings |> Array.map byte

        // Notes
        notes

        // Number of sections
        sectionCount

        // Section type 10 = Handshapes
        if handShapes.Length > 0 then
            10us
            handShapes.Length
            for hs in handShapes do yield! writeSection hs

        // Section type 14 = Tremolo
        if tremoloSections.Length > 0 then
            14us
            tremoloSections.Length
            for ts in tremoloSections do yield! writeSection ts

        // Section type 16 = FHP
        if anchors.Length > 0 then
            16us
            anchors.Length
            for a in anchors do yield! writeSection a

        // Section type 18 = Tone changes
        if tones.Length > 0 then
            18us
            tones.Length
            for t in tones do yield! writeSection t

        // Number of custom data blocks
        customDataBlockCount

        // ID 2 = Pro guitar finger arrays
        if fingeringData.Length > 0 then
            yield! customDataBlock 2u fingeringData

        // ID 3 = Arrangement type
        yield! customDataBlock 3u (Array.singleton (getArrangementType inst))

        // ID 4 = Pro guitar track tuning not honored
        yield! customDataBlock 4u (Array.singleton 1uy)

        // ID 6 = Capo position
        if inst.MetaData.Capo > 0y then
            yield! customDataBlock 6u (Array.singleton (byte inst.MetaData.Capo))

        // ID 7 = Tech notes
        if techNotesData.Length > 0 then
            yield! customDataBlock 7u techNotesData
    }
