module Rocksmith2014.DD.BeatDivider

open Rocksmith2014.XML
open System

let getDivision (beats: Ebeat list) (time: int) =
    let beat1 =
        beats
        |> List.tryFindBack (fun b -> b.Time <= time)

    let beat2 =
        beats
        |> List.tryFind (fun b -> b.Time >= time)

    match beat1, beat2 with
    | None, _ | _, None ->
        2
    | Some b1, Some b2 ->
        if time = b1.Time then
            if b1.Measure >= 0s then 0 else 1
        else
            let dist = b2.Time - b1.Time
            let notePos = time - b1.Time
            let div = int <| Math.Round(float dist / float notePos)
            // TODO: Improve
            if div = 1 then 8 else div
    
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
