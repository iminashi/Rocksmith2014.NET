module VocalsWriter

open System
open Rocksmith2014.EOF.EOFTypes
open Rocksmith2014.EOF.ImportTypes
open Rocksmith2014.XML
open BinaryWriterBuilder

let writeVocal (vocal: Vocal) =
    binaryWriter {
        if vocal.Lyric.EndsWith("+") then
            vocal.Lyric.Substring(0, vocal.Lyric.Length - 1)
        else
            vocal.Lyric

        // Lyric set number (0=PART VOCALS, 1=HARM1, 2=HARM2...)
        0uy

        // Note
        if vocal.Note = 254uy then 0uy else vocal.Note
        // Time
        vocal.Time
        // Length
        vocal.Length
        // Flags
        0u
    }

let getSectionTimes vocals =
    let rec getTimes startTime result (vocs: Vocal list) =
        match vocs with
        | h :: left when h.Lyric.EndsWith("+") ->
            let start = startTime |> Option.defaultValue h.Time
            let result = (start, h.Time + h.Length) :: result

            match left with
            | next :: _ ->
                getTimes (Some next.Time) result left
            | [] ->
                result
        | h :: left ->
            let startTime = startTime |> Option.orElse (Some h.Time)
            getTimes startTime result left
        | [] ->
            result

    vocals
    |> getTimes None []
    |> List.rev

let writeVocalsTrack (vocalsData: ImportedVocals option) =
    let vocals =
        vocalsData
        |> Option.map (fun x -> List.ofSeq x.Vocals)
        |> Option.defaultValue List.empty

    let trackFlags, customName =
        match vocalsData |> Option.bind (fun x -> x.CustomName |> Option.ofString) with
        | Some customName ->
            4278190082u, customName
        | None ->
            4278190080u, ""

    let sections =
        getSectionTimes vocals
        |> List.map (fun (startTime, endTime) -> EOFSection.Create(0uy, uint startTime, uint endTime, 0u))
        |> List.toArray

    binaryWriter {
        "PART VOCALS"
        2uy // format
        3uy // behaviour
        6uy // type
        -1y // difficulty level
        trackFlags // flags
        0us // compliance flags

        if not <| String.IsNullOrEmpty(customName) then customName

        // MIDI tone
        5y

        // Vocals
        vocals.Length
        for v in vocals do yield! writeVocal v

        // Number of section types
        1us
        // Lyrics section
        5us

        // Sections
        sections

        // Number of custom data blocks
        1
        // Custom data block size
        4
        // Custom data
        0xFFFFFFFFu
    }
