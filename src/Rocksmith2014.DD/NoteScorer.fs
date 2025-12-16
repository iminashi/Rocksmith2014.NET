module internal Rocksmith2014.DD.NoteScorer

open System
open Rocksmith2014.DD.DataExtractor
open Rocksmith2014.XML
open Rocksmith2014.XML.Extension

let private round (value: float) =
    Math.Round(value, MidpointRounding.AwayFromZero)

let private getSubdivision startTime endTime time =
    let dist = float <| endTime - startTime
    let pos = float <| time - startTime
    let mid = dist / 2.
    let div =
        let pos = if pos - mid > 10. then pos - mid else pos
        dist / pos

    int <| round div

let private getSubdivisionInsideMeasure (phraseEndTime: int) (beats: Ebeat list) (time: int) =
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
    let distance = float <| endTime - startTime
    let divisionLength = distance / float phraseDivisions.Length
    let position = float <| time - startTime

    let rec findDivIndex curr =
        let low = divisionLength * float curr
        let high = divisionLength * float (curr + 1)

        if position >= low && position < high then
            curr
        else
            findDivIndex (curr + 1)

    let divsionIndex = findDivIndex 0
    phraseDivisions[divsionIndex]

let private getTechniquePenalties (entity: XmlEntity) =
    let isFretHandMute, isBend =
        match entity with
        | XmlNote n ->
            n.IsFretHandMute, n.MaxBend > 0.0f
        | XmlChord c ->
            c.IsFretHandMute, false

    if isFretHandMute then
        20
    elif isBend then
        10
    else
        0

let getScore (phraseData: PhraseData) (time: NoteTime) (entity: XmlEntity) : NoteScore =
    let { StartTime = phraseStartTime; EndTime = phraseEndTime; Beats = beats } = phraseData

    let beat1 =
        beats
        |> List.tryFindBack (fun b -> b.Time <= time)

    let beat2 =
        beats
        |> List.tryFind (fun b -> b.Time >= time)

    let divisionInPhrase = getDivisionInPhrase phraseStartTime phraseEndTime time
    let penalties = getTechniquePenalties entity

    penalties
    + divisionInPhrase
    + match beat1, beat2 with
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

let createScoreMap (scores: (NoteTime * NoteScore) array) (totalNotes: int) : ScoreMap =
    scores
    |> Array.groupBy snd
    |> Seq.map (fun (group, elems) -> group, elems.Length)
    |> Seq.sortBy fst
    |> Seq.fold (fun acc (score, notes) ->
        let low =
            match List.tryHead acc with
            | None ->
                0.
            | Some (_, range) ->
                range.High

        let high = low + (float notes / float totalNotes)

        (score, { Low = low; High = high }) :: acc
    ) []
    |> readOnlyDict
