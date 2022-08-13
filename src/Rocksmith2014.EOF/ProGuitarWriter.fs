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

let writeNote (note: EOFNote) =
    binaryWriter {
        note.ChordName
        note.ChordNumber
        note.NoteType
        note.BitFlag
        note.GhostBitFlag
        note.Frets
        note.LegacyBitFlags
        note.Position
        note.Length
        note.Flags |> uint
        note.SlideEndFret
        note.BendStrength
        note.UnpitchedSlideEndFret
        if note.ExtendedNoteFlags <> EOFExtendedNoteFlag.ZERO then note.ExtendedNoteFlags |> uint
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
    let folder current acc =
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

    let folder2 a acc =
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
    |> fun s -> Seq.foldBack folder s []
    |> fun s -> Seq.foldBack folder2 s []
    |> List.toArray

let convertAnchors (level: Level) =
    level.Anchors.ToArray()
    |> Array.map (fun a -> EOFSection.Create(0uy, a.Time, int a.Fret, 0u))

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
            SectionCreated <| EOFSection.Create(0uy, hs.StartTime, hs.EndTime, if isArpeggio then 0u else 2u)
    )

let convertTones (inst: InstrumentalArrangement) =
    inst.Tones.Changes.ToArray()
    |> Array.map (fun t ->
        let endTime = if t.Name = inst.Tones.BaseToneName then 1 else 0
        { EOFSection.Create(255uy, t.Time, endTime, 0u) with Name = t.Name })

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
        use m = new MemoryStream()
        binaryWriter {
            if techNotes.Length > 0 then techNotes.Length
            for tn in techNotes do yield! writeNote tn
        } |> toStream(m)
        m.ToArray()

    let writeTechNotes = techNotesData.Length > 0

    let sectionCount =
        [ anchors; handShapes; tones ]
        |> List.sumBy (fun x -> Convert.ToUInt16(x.Length > 0))

    binaryWriter {
        "PART REAL_GUITAR"
        4uy // format
        5uy // behaviour
        9uy // type
        -1y // difficulty level
        4u // flags
        0us // compliance flags

        24uy // highest fret
        6uy // strings
        inst.MetaData.Tuning.Strings |> Array.map byte

        // Notes
        notes.Length
        for n in notes do yield! writeNote n

        // Number of sections
        sectionCount

        // Section type 10 = Handshapes
        if handShapes.Length > 0 then
            10us
            handShapes.Length
            for hs in handShapes do yield! writeSection hs

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
        if writeTechNotes then 3u else 2u

        // ID 2 = Pro guitar finger arrays
        yield! customDataBlock 2u fingeringData

        // ID 4 = Pro guitar track tuning not honored
        yield! customDataBlock 4u (Array.singleton 1uy)

        // ID 7 = Tech notes
        if writeTechNotes then
            yield! customDataBlock 7u techNotesData
    }
