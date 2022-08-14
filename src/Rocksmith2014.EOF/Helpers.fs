module Rocksmith2014.EOF.Helpers

open Rocksmith2014.XML
open System.Text.RegularExpressions
open EOFTypes

let getClosestBeat (beats: Ebeat array) (time: int) =
    let index =
        beats
        |> Array.tryFindIndexBack (fun b -> b.Time <= time)

    let next =
        index
        |> Option.bind (fun i -> beats |> Array.tryItem (i + 1))

    match index, next with
    | Some index, Some nextBeat ->
        if abs (time - beats[index].Time) < abs (time - nextBeat.Time) then
            index
        else
            index + 1
    | Some index, None ->
        index
    | _ ->
        0

let createEOFEvents (getTrackNumber: InstrumentalArrangement -> int) (inst: InstrumentalArrangement) =
    let beats = inst.Ebeats.ToArray()

    // Do not import time signature events
    let otherEvents =
        inst.Events
        |> Seq.filter (fun e -> e.Code.StartsWith("TS") |> not)

    let create text time flags =
        { Text = text
          BeatNumber = getClosestBeat beats time
          TrackNumber = getTrackNumber inst |> uint16
          Flag = flags }

    let phraseEvents =
        inst.PhraseIterations
        |> Seq.map (fun p ->
            let phrase = inst.Phrases[p.PhraseId]
            create phrase.Name p.Time 1us)
        |> Seq.cache

    let sectionEvents =
        inst.Sections
        |> Seq.map (fun s -> create s.Name s.Time 2us)
        |> Seq.cache

    let otherEvents =
        otherEvents
        |> Seq.map (fun e -> create e.Code e.Time 4us)

    let unifiedPhrasesAndSections =
        phraseEvents
        |> Seq.choose (fun p ->
            if sectionEvents |> Seq.exists (fun s -> s.BeatNumber = p.BeatNumber && s.Text = p.Text) then
                Some { p with Flag = 3us }
            else
                None)
        |> Seq.cache

    let phraseEvents =
        phraseEvents
        |> Seq.filter (fun p ->
            unifiedPhrasesAndSections
            |> Seq.exists (fun u -> p.BeatNumber = u.BeatNumber) |> not)

    let sectionEvents =
        sectionEvents
        |> Seq.filter (fun s ->
            unifiedPhrasesAndSections
            |> Seq.exists (fun u -> s.BeatNumber = u.BeatNumber) |> not)

    unifiedPhrasesAndSections
    |> Seq.append phraseEvents
    |> Seq.append sectionEvents
    |> Seq.append otherEvents

let tryParseTimeSignature (text: string) =
    let m = Regex.Match(text, "TS:(\d+)/(\d+)")
    if m.Success then
        let n = m.Groups[1].Value |> uint
        let d = m.Groups[2].Value |> uint
        if n <> 0u && d <> 0u then
            Some (n, d)
        else
            None
    else
        None

let getTimeSignatures (tsEvents: Event list) =
    tsEvents
    |> List.choose (fun e ->
        tryParseTimeSignature e.Code
        |> Option.map (fun (n, d) ->
            let ts =
                match n, d with
                | 2u, 4u -> ``TS 2 | 4``
                | 3u, 4u -> ``TS 3 | 4``
                | 4u, 4u -> ``TS 4 | 4``
                | 5u, 4u -> ``TS 5 | 4``
                | 6u, 4u -> ``TS 6 | 4``
                | a, b -> CustomTS(a, b)
            e.Time, ts))

let getBeatCountChanges (beats: Ebeat seq) =
    let initState = {| Counter = 1; BeatCounts = List.empty<int * int> |}

    (beats, initState)
    ||> Seq.foldBack (fun beat state ->
        if beat.Measure < 0s then
            {| state with Counter = state.Counter + 1 |}
        else
            // If the beat count has not changed, replace the old time with an earlier one
            let rest =
                match state.BeatCounts with
                | (_, prevCount) :: tail ->
                     if prevCount = state.Counter then tail else state.BeatCounts
                | [] ->
                    []

            {| state with
                Counter = 1
                BeatCounts = (beat.Time, state.Counter) :: rest |})

let inferTimeSignatures (beats: Ebeat seq) =
    let result = getBeatCountChanges beats

    result.BeatCounts
    |> List.map (fun (time, beatCount) ->
        let ts =
            match beatCount with
            | 2 -> ``TS 2 | 4``
            | 3 -> ``TS 3 | 4``
            | 4 -> ``TS 4 | 4``
            | 5 -> ``TS 5 | 4``
            | 6 -> ``TS 6 | 4``
            | 9 -> CustomTS (9u, 8u)
            | 12 -> CustomTS (12u, 8u)
            | v -> CustomTS (uint v, 4u)
        time, ts)
