module ProGuitarWriter

open Rocksmith2014.XML
open BinaryFileWriter
open NoteConverter
open EOFTypes
open System.IO

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

    techNotes
    |> Seq.sortBy (fun x -> x.Position)
    |> fun s -> Seq.foldBack folder s []
    |> List.toArray

let writeProTrack (inst: InstrumentalArrangement) =
    let notes, fingeringData, techNotes =
        convertNotes inst inst.Levels[0]
        |> Array.unzip3

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
        0us

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
