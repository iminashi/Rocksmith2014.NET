module Rocksmith2014.XML.Processing.Utils

open Rocksmith2014.XML

let internal issue type' time = { Type = type'; TimeCode = time }

[<RequireQualifiedAccess>]
module Option =
    let minOfMany options =
        options
        |> List.collect Option.toList
        |> List.tryMin

/// Converts a time in milliseconds into a string.
let timeToString time =
    let minutes = time / 1000 / 60
    let seconds = (time / 1000) - (minutes * 60)

    let milliSeconds =
        time - (minutes * 60 * 1000) - (seconds * 1000)

    $"{minutes:D2}:{seconds:D2}.{milliSeconds:D3}"

let private getTimes<'a when 'a :> IHasTimeCode> startTime count (items: 'a seq) =
    items
    |> Seq.filter (fun x -> x.Time >= startTime)
    |> Seq.map (fun x -> x.Time)
    // Notes on the same timecode (i.e. split chords) count as one
    |> Seq.distinct
    |> Seq.truncate count

let internal findTimeOfNthNoteFrom (level: Level) (startTime: int) (nthNote: int) =
    let noteTimes = level.Notes |> getTimes startTime nthNote
    let chordTimes = level.Chords |> getTimes startTime nthNote

    noteTimes
    |> Seq.append chordTimes
    |> Seq.sort
    |> Seq.skip (nthNote - 1)
    |> Seq.head

let private getMinTime (first: int option) (second: int option) =
    Option.minOfMany [ first; second ]

let findFirstLevelWithContent (arrangement: InstrumentalArrangement) =
    if arrangement.Levels.Count = 1 then
        Some arrangement.Levels[0]
    else
        // Find the first phrase that has difficulty levels
        let firstPhraseIteration =
            arrangement.PhraseIterations
            |> ResizeArray.tryFind (fun pi ->
                arrangement.Phrases[pi.PhraseId].MaxDifficulty > 0uy)

        match firstPhraseIteration with
        | Some firstPhraseIteration ->
            let firstPhrase = arrangement.Phrases[firstPhraseIteration.PhraseId]
            Some arrangement.Levels[int firstPhrase.MaxDifficulty]
        | None ->
            // There are DD levels, but no phrases where MaxDifficulty > 0
            // Find the first level that has notes or chords
            arrangement.Levels
            |> Seq.tryFind (fun level -> level.Notes.Count > 0 || level.Chords.Count > 0)

let getFirstNoteTime (arrangement: InstrumentalArrangement) =
    findFirstLevelWithContent arrangement
    |> Option.bind (fun firstPhraseLevel ->
        let firstNote =
            firstPhraseLevel.Notes
            |> ResizeArray.tryHead
            |> Option.map (fun n -> n.Time)

        let firstChord =
            firstPhraseLevel.Chords
            |> ResizeArray.tryHead
            |> Option.map (fun c -> c.Time)

        getMinTime firstNote firstChord)
