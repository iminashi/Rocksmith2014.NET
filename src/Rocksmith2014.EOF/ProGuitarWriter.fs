module Rocksmith2014.EOF.ProGuitarWriter

open Rocksmith2014.XML
open System
open EOFTypes
open BinaryWriterBuilder
open FlagBuilder
open NoteConverter
open Tremolo
open TechNotes
open HandShapes

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

let convertAnchors (inst: InstrumentalArrangement) =
    inst.Levels
    |> Seq.mapi (fun diff level ->
        level.Anchors.ToArray()
        |> Array.map (fun a -> EOFSection.Create(byte diff, uint a.Time, uint a.Fret, 0u))
    )
    |> Array.concat

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

let prepareNotes (handShapeResult: HsResult array) (inst: InstrumentalArrangement) (notes: EOFNote array) =
    let updates =
        handShapeResult
        |> Array.collect (function AdjustSustains s -> s | _ -> Array.empty)
        |> Array.map (fun x -> (x.Difficulty, x.Time), x.NewSustain)
        |> readOnlyDict

    // Update note sustains if necessary
    let notes =
        if updates.Count = 0 then
            notes
        else
            notes
            |> Array.map (fun n ->
                match updates.TryGetValue((n.NoteType, n.Position)) with
                | true, length -> { n with Length = length }
                | false, _ -> n)

    // Fix fret numbers if capo is used
    if inst.MetaData.Capo > 0y then
        notes
        |> Array.map (fun n ->
            let capo = byte inst.MetaData.Capo
            let newFrets =
                n.Frets
                |> Array.map (fun f -> if f = 0uy then 0uy else f - capo)
            let newSlide = n.SlideEndFret |> ValueOption.map (fun f -> f - capo)
            let newUpSlide = n.UnpitchedSlideEndFret |> ValueOption.map (fun f -> f - capo)
            { n with Frets = newFrets; SlideEndFret = newSlide; UnpitchedSlideEndFret = newUpSlide })
    else
        notes

let writeProTrack (name: string) (imported: ImportedArrangement) =
    let inst = imported.Data
    let notes, fingeringData, techNotes = convertNotes inst
    let tones = convertTones inst
    let anchors = convertAnchors inst
    let handShapeResult = convertHandShapes inst notes
    let notes = prepareNotes handShapeResult inst notes
    let handShapes = handShapeResult |> Array.choose (function SectionCreated s -> Some s | _ -> None)

    let fingeringData = fingeringData |> Array.concat
    let techNotes =
        techNotes
        |> Array.concat
        |> combineTechNotes

    let techNotesData = getTechNoteData techNotes
    let tremoloSections = createTremoloSections notes

    let sectionCount =
        [ anchors; handShapes; tones; tremoloSections ]
        |> List.sumBy (fun x -> Convert.ToUInt16(x.Length > 0))

    let customDataBlockCount =
        let fingering = if fingeringData.Length > 0 then 1u else 0u
        let capo = if inst.MetaData.Capo > 0y then 1u else 0u
        let tech = if techNotesData.Length > 0 then 1u else 0u
        let diff = if inst.Levels.Count > 5 then 1u else 0u
        2u + fingering + capo + tech + diff

    let trackFlag =
        let ap = inst.MetaData.ArrangementProperties
        flags {
            EOFTrackFlag.UNLIMITED_DIFFS
            EOFTrackFlag.ALT_NAME

            if ap.BassPick then
                EOFTrackFlag.RS_PICKED_BASS

            if ap.BonusArrangement then
                EOFTrackFlag.RS_BONUS_ARR
            elif not ap.Represent then
                EOFTrackFlag.RS_ALT_ARR
        }

    let stringCount =
        if inst.MetaData.ArrangementProperties.PathBass then 4uy else 6uy
    let tuning =
        inst.MetaData.Tuning.Strings
        |> Array.take (int stringCount)
        |> Array.map byte

    let trackType =
        match name with
        | Contains "BONUS" -> 14uy
        | Contains "BASS" -> 8uy
        | _ -> 9uy

    binaryWriter {
        // Name (PART REAL...)
        name
        // Format (4 = Pro Guitar/Bass)
        4uy 
        // Behaviour (5 = Pro Guitar/Bass)
        5uy
        // Type
        trackType
        // Difficulty level
        -1y
        //Flags
        trackFlag |> uint
        // Compliance flags
        0us

        // Alternative name
        imported.CustomName

        24uy // highest fret
        stringCount // strings
        tuning

        // Notes
        notes

        // Number of sections
        sectionCount

        // Section type 10 = Handshapes
        if handShapes.Length > 0 then
            10us
            handShapes

        // Section type 14 = Tremolo
        if tremoloSections.Length > 0 then
            14us
            tremoloSections

        // Section type 16 = FHP
        if anchors.Length > 0 then
            16us
            anchors

        // Section type 18 = Tone changes
        if tones.Length > 0 then
            18us
            tones

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

        // ID 9 = Difficulty level count
        if inst.Levels.Count > 5 then
            yield! customDataBlock 9u (Array.singleton (byte inst.Levels.Count))
    }
