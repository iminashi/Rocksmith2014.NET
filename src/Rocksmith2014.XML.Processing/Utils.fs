module Rocksmith2014.XML.Processing.Utils

open Rocksmith2014.XML

let internal issue type' time = IssueWithTimeCode(type', time)

[<RequireQualifiedAccess>]
module Option =
    let minOfMany (options: 'a option list) =
        options
        |> List.collect Option.toList
        |> List.tryMin

/// Converts a time in milliseconds into a string.
let timeToString (time: int) =
    let minutes = time / 1000 / 60
    let seconds = (time / 1000) - (minutes * 60)

    let milliSeconds =
        time - (minutes * 60 * 1000) - (seconds * 1000)

    $"{minutes:D2}:{seconds:D2}.{milliSeconds:D3}"

let private getTimes<'a when 'a :> IHasTimeCode> (startTime: int) (count: int) (items: 'a seq) =
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

let findPreviousNoteOnSameString (notes: ResizeArray<Note>) (startIndex: int) =
    let startNote = notes[startIndex]
    let rec search index diffStringNotesInBetween =
        match ResizeArray.tryItem index notes with
        | None ->
            None, diffStringNotesInBetween
        | Some n when n.Time = startNote.Time ->
            search (index - 1) diffStringNotesInBetween
        | Some n when n.String <> startNote.String ->
            search (index - 1) true
        | Some _ as pn ->
            pn, diffStringNotesInBetween

    search (startIndex - 1) false

let getFretOrSlideEndFret (n: Note) =
    if n.IsSlide then
        n.SlideTo
    elif n.IsUnpitchedSlide then
        n.SlideUnpitchTo
    else
        n.Fret

// Returns the possible chord and the fret used for the given string.
let findPreviousChordUsingSameString
        (templates: ResizeArray<ChordTemplate>) (chords: ResizeArray<Chord>) (stringNum: sbyte) (time: int) =
    chords
    |> Seq.tryFindBack (fun chord ->
        if chord.Time >= time then
            false
        else
            templates
            |> ResizeArray.tryItem (int chord.ChordId)
            |> Option.exists (fun template -> template.Frets[int stringNum] > -1y))
    |> Option.map (fun chord ->
        let fret =
            chord.ChordNotes
            |> Option.ofObj
            |> Option.bind (ResizeArray.tryPick (fun cn ->
                if cn.String = stringNum then
                    Some (getFretOrSlideEndFret cn)
                else
                    None))
            |> Option.defaultWith (fun () -> templates[int chord.ChordId].Frets[int stringNum])

        chord, fret)

let tryFindActiveAnchor (level: Level) (time: int) =
    let anchors = level.Anchors
    if anchors.Count = 0 then
        None
    else
        let rec search index =
            if index >= anchors.Count || anchors[index].Time > time then
                Some (anchors[max 0 (index - 1)])
            else
                search (index + 1)

        search 0

let tryFindNextNoteOnSameString (notes: ResizeArray<Note>) (currentIndex: int) (note: Note) =
    let nextIndex =
        if currentIndex = -1 then
            notes.FindIndex(fun n -> n.Time > note.Time && n.String = note.String)
        else
            notes.FindIndex(currentIndex + 1, fun n -> n.String = note.String)

    if nextIndex = -1 then None else Some notes[nextIndex]

/// Returns the time of the next note, chord or handshape that is closest to the given time.
let tryFindNextContentTime (level: Level) (time: int) : int option =
    let tryFindNext (ra: ResizeArray<#IHasTimeCode>) =
        ra
        |> ResizeArray.tryFind (fun x -> x.Time >= time)
        |> Option.map (fun x -> x.Time)

    let noteTime = tryFindNext level.Notes
    let chordTime = tryFindNext level.Chords
    let handShapeTime = tryFindNext level.HandShapes

    Option.minOfMany [ noteTime; chordTime; handShapeTime ]
