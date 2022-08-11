module ProGuitarWriter

open Rocksmith2014.XML
open BinaryFileWriter
open NoteConverter
open EOFTypes

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

let customDataBlock (blockId: int) (data: byte array) =
    binaryWriter {
        // Custom data block size (+ 4 bytes for block ID)
        data.Length + 4
        blockId
        data
    }

let writeProTrack (inst: InstrumentalArrangement) =
    let notes, fingeringData =
        convertNotes inst inst.Levels[0]
        |> Array.unzip

    let fingeringData =
        fingeringData
        |> Array.collect id

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
        2u

        // ID 2 = Pro guitar finger arrays
        yield! customDataBlock 2 fingeringData

        // ID 4 = Pro guitar track tuning not honored
        yield! customDataBlock 4 (Array.singleton 1uy)
    }
