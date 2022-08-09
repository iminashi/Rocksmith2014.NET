module Rocksmith2014.EOF.EOFProjectWriter

open Rocksmith2014.XML
open BinaryFileWriter
open EOFTypes
open Helpers
open IniWriters
open BeatWriter
open VocalsWriter
open System

let writeOggProfiles (delay: int) =
    binaryWriter {
        // Number of OGG profiles
        1us

        // File name
        "guitar.ogg"
        // orig. filename length
        0us
        // ogg profile length
        0us
        // MIDI delay
        delay
        // profile flags
        0
    }

let writeEvents (events: (int * EOFEvent) array) =
    binaryWriter {
        // Number of events
        events.Length 

        for _, e in events do
            // Text
            e.Text
            // associated_beat_number_or_position
            e.BeatNumber
            // associated_track_number
            0us
            // Flags
            e.Flag
    }


let customData =
    binaryWriter {
        0u // number int
    }

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

let writeProTrack (_inst: InstrumentalArrangement) =
    binaryWriter {
        "arr_lead"
        4uy // format
        5uy // behaviour
        9uy // type
        -1y // difficulty level
        4u // flags
        0us // compliance flags

        24uy // highest fret
        6uy // strings
        Array.replicate 6 0uy // tuning

        0u // notes
        //writeString writer "" // chord name
        //writer.Write(0y)
        //writer.Write(0b00000001y)
        //writer.Write(0y)
        //writer.Write(5y) // frets
        //writer.Write(0y)
        //writer.Write(4500)
        //writer.Write(497)
        //writer.Write(0) // flags

        0us // number of sections

        0u // custom data blocks

        //writer.Write(5) // size
        //writer.Write(2) // ID
        //writer.Write(0y) // data
    }

let writeDummyLegacyTack (name: string, behavior: byte, type': byte, lanes: byte) =
    binaryWriter {
        name
        1uy // format
        behavior // behaviour
        type' // type
        -1y // difficulty level
        let flags = if name = "PART DRUMS" then 4278190080u else 0u
        flags // flags
        0us // compliance flags

        lanes // number of lanes
        0u // number of notes

        0us // number of section types

        1 // number of custom data blocks
        4 // custom data block size
        0xFFFFFFFFu // custom data
    }


let writeTracks (tracks: EOFTrack list) =
    binaryWriter {
        tracks.Length + 1 // number int

        // Track 0, which stores non track-specific sections like bookmarks
        ""
        0uy // format
        0uy // behaviour
        0uy // type
        0y // difficulty level
        0 // flags
        65535us // compliance flags

        0us // number of sections
        1 // custom data blocks
        4 // custom data block size
        0xFFFFFFFFu // custom data

        for t in tracks do
            match t with
            | Legacy (n, b, t, l) ->
                yield! writeDummyLegacyTack (n, b, t, l)
            | Vocals (name, vocals) ->
                yield! writeVocalsTrack name vocals
            | ProGuitar (ExistingTrack arr) ->
                yield! writeProTrack arr
            | ProGuitar (EmptyTrack name) ->
                yield! writeEmptyProGuitarTrack name
    }

let tracks vocals =
    [
        Legacy ("PART GUITAR", 1uy, 1uy, 5uy)
        Legacy ("PART BASS", 1uy, 2uy, 5uy)
        Legacy ("PART GUITAR COOP", 1uy, 3uy, 5uy)
        Legacy ("PART RHYTHM", 1uy, 4uy, 5uy)
        Legacy ("PART DRUMS", 2uy, 5uy, 5uy)
        Vocals ("PART VOCALS", vocals)
        Legacy ("PART KEYS", 4uy, 7uy, 5uy)
        ProGuitar (EmptyTrack "PART REAL_BASS")
        ProGuitar (EmptyTrack "PART REAL_GUITAR")
        Legacy ("PART DANCE", 7uy, 10uy, 4uy)
        ProGuitar (EmptyTrack "PART REAL_BASS_22")
        ProGuitar (EmptyTrack "PART REAL_GUITAR_22")
        Legacy ("PART REAL_DRUMS_PS", 2uy, 13uy, 5uy)
    ]

let writeHeader =
    binaryWriter {
        // Magic
        "EOFSONH\x00"B
        // Padding (8 bytes)
        0L

        // Revision
        1
        // Timing format (0 = ms)
        0uy
        // Time division (not used)
        480
    }

/// Write project.
let writeEofProject (path: string) (inst: InstrumentalArrangement) (vocals: Vocal seq) =
    let events, tsEvents = createEOFEvents inst

    let tsEvents =
        if not tsEvents.IsEmpty then
            getTimeSignatures tsEvents
        else
            inferTimesignatures inst.Ebeats

    let iniStrings =
        [|
            if not <| String.IsNullOrWhiteSpace inst.MetaData.ArtistName then
                { StringType = IniStringType.Artist
                  Value = inst.MetaData.ArtistName }

            if not <| String.IsNullOrWhiteSpace inst.MetaData.Title then
                { StringType = IniStringType.Title
                  Value = inst.MetaData.Title }

            if not <| String.IsNullOrWhiteSpace inst.MetaData.AlbumName then
                { StringType = IniStringType.Album
                  Value = inst.MetaData.AlbumName }

            if inst.MetaData.AlbumYear > 0 then
                { StringType = IniStringType.Year
                  Value = string inst.MetaData.AlbumYear }
        |]

    binaryWriter {
        yield! writeHeader
        yield! writeIniStrings iniStrings
        yield! writeIniBooleans
        yield! writeIniNumbers
        yield! writeOggProfiles inst.StartBeat
        yield! writeBeats inst events tsEvents
        yield! writeEvents events
        yield! customData
        yield! writeTracks (tracks vocals)
    }
    |> toFile path
