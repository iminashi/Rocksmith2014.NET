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
    | None, _ | _, None -> Note8th
    | Some b1, Some b2 ->
        if time = b1.Time then
            if b1.Measure >= 0s then OnStrongBeat else OnWeakBeat
        else
            let dist = b2.Time - b1.Time
            let notePos = time - b1.Time
            if Math.Abs(notePos - (dist / 2)) <= 3 then Note8th
            elif Math.Abs(notePos - (dist / 3)) <= 3 then Note8thTriplet
            elif Math.Abs(notePos - (dist / 4)) <= 3 then Note16th
            elif Math.Abs(notePos - (dist / 6)) <= 3 then Note16thTriplet
            else Other
    
let createDivisionMap (divisions: (int * BeatDivision) array) totalNotes =
    divisions
    |> Array.groupBy snd
    |> Seq.map (fun (group, elems) -> group, elems.Length)
    |> Seq.sortBy fst
    |> Seq.fold (fun acc elem ->
        let division, notes = elem
        let low =
            match List.tryHead acc with
            | None -> 0uy
            | Some x -> (snd x).High
        let high = byte <| Math.Round(float low + (100. * float notes / float totalNotes))

        (division, { Low = low; High = high })::acc
    ) []
    |> Map.ofList
