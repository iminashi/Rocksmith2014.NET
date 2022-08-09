module VocalsWriter

open Rocksmith2014.XML
open BinaryFileWriter
open SectionWriter

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

let writeVocalsTrack (name: string) (vocalSeq: Vocal seq) =
    let vocals = vocalSeq |> List.ofSeq
    let sectionTimes = getSectionTimes vocals

    binaryWriter {
        name
        2uy // format
        3uy // behaviour
        6uy // type
        -1y // difficulty level
        4278190080u // flags
        0us // compliance flags

        // MIDI tone
        5y

        vocals.Length
        for v in vocals do yield! writeVocal v

        // Number of section types
        1us 
        // Lyrics section
        5us

        // Number of sections
        sectionTimes.Length
        for startTime, endTime in sectionTimes do
            yield! writeSection "" 0uy startTime endTime 0u

        // Number of custom data blocks
        1
        // Custom data block size
        4
        // Custom data
        0xFFFFFFFFu
    }
