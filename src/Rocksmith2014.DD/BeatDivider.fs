module Rocksmith2014.DD.BeatDivider

open Rocksmith2014.DD.DataExtractor
open Rocksmith2014.XML
open System

let round (value: float) = Math.Round(value, MidpointRounding.AwayFromZero)

let private getSubdivision startTime endTime time =
    let dist = float <| endTime - startTime
    let pos = float <| time - startTime
    let mid = dist / 2.
    let div =
        let pos = if pos - mid > 10. then pos - mid else pos
        dist / pos
    
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

let private phraseDivisions = [| 0; 3; 2; 3; 1; 3; 2; 3; 4 |]

let private getDivisionInPhrase startTime endTime time =
    let dist = float <| endTime - startTime
    let divisionLength = dist / 9.
    let pos = float <| time - startTime

    let rec findDiv curr =
        if pos >= divisionLength * float curr && pos < divisionLength * float (curr + 1) then
            curr
        else
            findDiv (curr + 1)

    let div = findDiv 0
    phraseDivisions.[div]

let getDivision (phraseData: PhraseData) (time: int) (entity: XmlEntity) : BeatDivision =
    let { StartTime=phraseStartTime; EndTime=phraseEndTime; Beats=beats } = phraseData

    let beat1 =
        beats
        |> List.tryFindBack (fun b -> b.Time <= time)

    let beat2 =
        beats
        |> List.tryFind (fun b -> b.Time >= time)

    let divisionInPhrase = getDivisionInPhrase phraseStartTime phraseEndTime time
    let isFretHandMute =
        match entity with
        | XmlNote n -> n.IsFretHandMute
        | XmlChord c -> c.IsFretHandMute

    // De-emphasize fret hand mutes
    if isFretHandMute then 20 else 0
    + divisionInPhrase +
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
            | None -> 0.
            | Some (_, range) -> range.High
        let high = low + (float notes / float totalNotes)

        (division, { Low = low; High = high })::acc
    ) []
    |> Map.ofList
