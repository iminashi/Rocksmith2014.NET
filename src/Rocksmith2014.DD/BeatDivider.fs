module Rocksmith2014.DD.BeatDivider

open Rocksmith2014.XML
open System

let private getSubdivision startTime endTime time =
    let dist = float <| endTime - startTime
    let pos = float <| time - startTime
    let div =
        let d = dist / pos
        // TODO: Improve
        if round d < 2. then d * 2. else d
    
    int <| round div
    
let private getSubdivisionInsideMeasure phraseEndTime (beats: Ebeat list) (time: int) =
    let measure =
        beats
        |> List.tryFindBack (fun b -> b.Time < time && b.Measure <> -1s)

    match measure with
    | None ->
        2
    | Some first ->
        let followingMeasure =
            beats
            |> List.tryFind (fun b -> b.Time > time && b.Measure <> -1s)

        let endTime = 
            match followingMeasure with
            | None -> phraseEndTime
            | Some second -> second.Time

        getSubdivision first.Time endTime time

let getDivision phraseEndTime (beats: Ebeat list) (time: int) =
    let beat1 =
        beats
        |> List.tryFindBack (fun b -> b.Time <= time)

    let beat2 =
        beats
        |> List.tryFind (fun b -> b.Time >= time)

    match beat1, beat2 with
    | None, _ ->
        // The note comes before any beat
        20
    | Some b1, Some b2 ->
        if time = b1.Time then
            if b1.Measure >= 0s then
                // On the first beat of the measure
                0
            else
                getSubdivisionInsideMeasure phraseEndTime beats time
        else
            10 * getSubdivision b1.Time b2.Time time
    | Some b1, None ->
        // The note comes after the last beat in the phrase
        10 * getSubdivision b1.Time phraseEndTime time
    
let createDivisionMap (divisions: (int * BeatDivision) array) totalNotes =
    divisions
    |> Array.groupBy snd
    |> Seq.map (fun (group, elems) -> group, elems.Length)
    |> Seq.sortBy fst
    |> Seq.fold (fun acc (division, notes) ->
        let low =
            match List.tryHead acc with
            | None -> 0uy
            | Some x -> (snd x).High
        let high = Math.Round(float low + (100. * float notes / float totalNotes))

        (division, { Low = low; High = byte high })::acc
    ) []
    |> Map.ofList
