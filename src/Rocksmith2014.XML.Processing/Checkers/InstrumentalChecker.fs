module Rocksmith2014.XML.Processing.InstrumentalChecker

open Rocksmith2014.XML
open Rocksmith2014.XML.Extensions
open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text.RegularExpressions
open Utils

[<IsReadOnly; Struct>]
type private NgSection = { StartTime: int; EndTime: int }

/// Checks for unexpected crowd events between the intro applause start and end events.
let checkCrowdEventPlacement (arrangement: InstrumentalArrangement) =
    let introApplauseStart =
        arrangement.Events
        |> ResizeArray.tryFind (fun e -> e.Code = "E3")

    let applauseEnd =
        arrangement.Events
        |> ResizeArray.tryFind (fun e -> e.Code = "E13")

    let crowdEventRegex = Regex("e[0-2]|E3|D3$")

    match introApplauseStart, applauseEnd with
    | None, _ ->
        List.empty
    | Some start, None ->
        [ issue ApplauseEventWithoutEnd start.Time ]
    | Some start, Some end' ->
        arrangement.Events
        |> Seq.filter (fun ev -> ev.Time > start.Time && ev.Time < end'.Time && crowdEventRegex.IsMatch ev.Code)
        |> Seq.map (fun ev -> issue (EventBetweenIntroApplause ev.Code) ev.Time)
        |> Seq.toList

let private getNoguitarSections (arrangement: InstrumentalArrangement) =
    [| let sections = arrangement.Sections

       for i in 1 .. sections.Count do
           let section = sections[i - 1]

           let endTime =
               if i = sections.Count then
                   arrangement.MetaData.SongLength
               else
                   sections[i].Time

           if String.startsWith "noguitar" section.Name then
               { StartTime = section.Time
                 EndTime = endTime } |]

let private isInsideNoguitarSection noGuitarSections (time: int) =
    noGuitarSections
    |> Array.exists (fun x -> time >= x.StartTime && time < x.EndTime)

let private isLinkedToChord (level: Level) (note: Note) =
    level.Chords.Exists(fun c ->
        c.Time = note.Time + note.Sustain
        && c.HasChordNotes
        && c.ChordNotes.Exists(fun cn -> cn.String = note.String))

let private checkLinkNext (level: Level) (currentIndex: int) (note: Note) =
    if isLinkedToChord level note then
        Some(issue NoteLinkedToChord note.Time)
    else
        match Utils.tryFindNextNoteOnSameString level.Notes currentIndex note with
        | None ->
            Some(issue LinkNextMissingTargetNote note.Time)

        // Check if the next note is at the end of the sustain for this note
        | Some nextNote when nextNote.Time - (note.Time + note.Sustain) > 1 ->
            Some(issue IncorrectLinkNext note.Time)

        // Check if the frets match
        | Some nextNote when note.Fret <> nextNote.Fret ->
            let slideTo =
                if note.SlideTo = -1y then
                    note.SlideUnpitchTo
                else
                    note.SlideTo

            if slideTo = nextNote.Fret then
                None
            elif slideTo <> -1y then
                Some(issue LinkNextSlideMismatch note.Time)
            else
                Some(issue LinkNextFretMismatch nextNote.Time)

        // Check if the bend values match
        | Some nextNote when note.IsBend ->
            let thisNoteLastBendValue =
                note.BendValues[note.BendValues.Count - 1].Step

            // If the next note has bend values and the first one is at the same timecode as the note, compare to that bend value
            let nextNoteFirstBendValue =
                if nextNote.IsBend && nextNote.Time = nextNote.BendValues[0].Time then
                    nextNote.BendValues[0].Step
                else
                    0f

            if thisNoteLastBendValue <> nextNoteFirstBendValue then
                Some(issue LinkNextBendMismatch nextNote.Time)
            else
                None

        | _ ->
            None

let private isOnToneChange (arr: InstrumentalArrangement) time =
    notNull arr.Tones.Changes
    && arr.Tones.Changes.Exists(fun t -> t.Time = time)

let private isPhraseChangeOnSustain (arr: InstrumentalArrangement) (note: Note) =
    arr.PhraseIterations.Exists(fun pi ->
        pi.Time > note.Time
        && pi.Time <= note.Time + note.Sustain
        && (not <| String.startsWith "mover" arr.Phrases[pi.PhraseId].Name))

let private tryFindOverlappingBendValue (bendValues: ResizeArray<BendValue>) =
    let rec finder index =
        match bendValues |> ResizeArray.tryItem (index + 1) with
        | Some nextBv ->
            if bendValues[index].Time = nextBv.Time then
                Some (issue OverlappingBendValues nextBv.Time)
            else
                finder (index + 1)
        | None ->
            None
    finder 0

/// Checks the notes in the level for issues.
let checkNotes (arrangement: InstrumentalArrangement) (level: Level) =
    let ngSections = getNoguitarSections arrangement

    [
        for i = 0 to level.Notes.Count - 1 do
            let note = level.Notes[i]
            let time = note.Time
            let prevNoteOnSameStringOpt, differentStringNotesBetweenPrevNoteOnSameString =
                findPreviousNoteOnSameString level.Notes i
            // TODO: find previous chord that uses same string
            let anchorAtNoteOpt = level.Anchors.FindByTime(time) |> Option.ofObj

            // Check for notes with LinkNext and unpitched slide
            if note.IsLinkNext && note.IsUnpitchedSlide then
                issue UnpitchedSlideWithLinkNext time

            // Check for phrases placed on a LinkNext note's sustain
            if note.IsLinkNext && isPhraseChangeOnSustain arrangement note then
                issue PhraseChangeOnLinkNextNote time

            // Check for notes with both harmonic and pinch harmonic attributes
            if note.IsHarmonic && note.IsPinchHarmonic then
                issue DoubleHarmonic time

            // Check 7th fret harmonic notes with sustain (and without ignore)
            if not note.IsIgnore && note.Fret = 7y && note.IsHarmonic && note.Sustain > 0 then
                issue SeventhFretHarmonicWithSustain time

            // Check bend values
            if note.IsBend then
                // Check for natural harmonic with bend
                if note.IsHarmonic then
                    issue NaturalHarmonicWithBend time

                // Check for missing bend values
                if note.BendValues.FindIndex(fun bv -> bv.Step <> 0.0f) = -1 then
                    issue MissingBendValue time

                yield!
                    tryFindOverlappingBendValue note.BendValues
                    |> Option.toList

            // Check tone change placement
            if isOnToneChange arrangement time then
                issue ToneChangeOnNote time

            // Check LinkNext issues
            if note.IsLinkNext then
                yield! checkLinkNext level i note |> Option.toList

            // Check for notes inside noguitar sections
            if isInsideNoguitarSection ngSections time then
                issue NoteInsideNoguitarSection time

            // Check for HOPO on same fret as previous note
            // Don't create an issue if there are notes on a different string between this note and the previous note on the same string
            // Should prevent false positives for "hammer-ons from nowhere"
            if not differentStringNotesBetweenPrevNoteOnSameString
               && prevNoteOnSameStringOpt |> Option.exists (fun pn -> pn.Fret = note.Fret && note.IsHopo)
            then
                issue HopoIntoSameNote time

            // Check for finger change during slide
            match anchorAtNoteOpt, prevNoteOnSameStringOpt with
            | Some thisAnchor, Some prevNote when prevNote.IsLinkNext && prevNote.IsSlide ->
                let previousActiveAnchor = findActiveAnchor level prevNote.Time
                let prevFinger = prevNote.Fret - previousActiveAnchor.Fret
                let thisFinger = note.Fret - thisAnchor.Fret
                if prevFinger <> thisFinger then
                    issue FingerChangeDuringSlide time
            | _ ->
                ()

            // Check for position shift into pull-off
            match anchorAtNoteOpt with
            | Some activeAnchor ->
                let previousAnchor = findActiveAnchor level (time - 10)
                if previousAnchor <> activeAnchor && note.IsPullOff && note.Fret > 0y then
                    issue PositionShiftIntoPullOff time
            | None ->
                ()

            // Check for invalid strings on bass arrangement
            if arrangement.MetaData.ArrangementProperties.PathBass && note.String >= 4y then
                issue InvalidBassArrangementString time
    ]

let private chordHasStrangeFingering (chordTemplates: ResizeArray<ChordTemplate>) (chord: Chord) =
    option {
        let! chordTemplate = chordTemplates |> ResizeArray.tryItem (int chord.ChordId)
        let fingersSorted = chordTemplate.Fingers |> Array.sort
        // Ignore thumb
        let! lowestFinger = fingersSorted |> Array.tryFind (fun f -> f > 0y)
        let fingerIndex = Array.IndexOf(chordTemplate.Fingers, lowestFinger)
        let lowestFingerFret = chordTemplate.Frets[fingerIndex]
        return
            chordTemplate.Frets
            |> Array.exists (fun fret -> fret > 0y && fret < lowestFingerFret)
    }
    |> Option.contains true

let private chordHasBarreOverOpenStrings (chordTemplates: ResizeArray<ChordTemplate>) (chord: Chord) =
    chordTemplates
    |> ResizeArray.tryItem (int chord.ChordId)
    |> Option.exists (fun ct ->
        [ 0y..4y ]
        |> List.exists (fun finger ->
            let low = Array.IndexOf(ct.Fingers, finger)
            let high = Array.LastIndexOf(ct.Fingers, finger)

            low <> -1 && high > low &&
            ct.Frets.AsSpan(low, high - low).Contains(0y)))

let private chordHasMutedString (chord: Chord) =
    not chord.IsFretHandMute
    && chord.ChordNotes.Exists(fun n -> n.IsFretHandMute)
    && not (chord.ChordNotes.TrueForAll(fun n -> n.IsFretHandMute))

/// Checks the chords in the level for issues.
let checkChords (arrangement: InstrumentalArrangement) (level: Level) =
    let ngSections = getNoguitarSections arrangement

    [
        for chord in level.Chords do
            let time = chord.Time

            if chord.HasChordNotes then
                let chordNotes = chord.ChordNotes
                let anchorAtChordOpt = level.Anchors.FindByTime(time) |> Option.ofObj

                // Check 7th fret harmonic notes with sustain (and without ignore)
                if not chord.IsIgnore && chordNotes.Exists(fun cn -> cn.Sustain > 0 && cn.Fret = 7y && cn.IsHarmonic) then
                    issue SeventhFretHarmonicWithSustain time

                // Check for notes with LinkNext and unpitched slide
                if chordNotes.Exists(fun cn -> cn.IsLinkNext && cn.IsUnpitchedSlide) then
                    issue UnpitchedSlideWithLinkNext time

                // Check for notes with both harmonic and pinch harmonic attributes
                if chordNotes.Exists(fun cn -> cn.IsHarmonic && cn.IsPinchHarmonic) then
                    issue DoubleHarmonic time

                // Check for missing bend values
                if chordNotes.Exists(fun cn -> cn.IsBend && cn.BendValues.FindIndex(fun bv -> bv.Step <> 0.0f) = -1) then
                    issue MissingBendValue time

                yield!
                    chordNotes
                    |> ResizeArray.tryPick (fun cn -> if cn.IsBend then tryFindOverlappingBendValue cn.BendValues else None)
                    |> Option.toList

                // EOF does not set LinkNext on chords correctly, so check all chords regardless of LinkNext status
                yield!
                    chordNotes
                    |> Seq.filter (fun cn -> cn.IsLinkNext)
                    |> Seq.map (fun cn -> checkLinkNext level -1 cn |> Option.toList)
                    |> List.concat

                // Check for position shift into pull-off
                match anchorAtChordOpt with
                | Some activeAnchor ->
                    let previousAnchor = findActiveAnchor level (time - 10)
                    if previousAnchor <> activeAnchor && chordNotes.Exists(fun note -> note.IsPullOff && note.Fret > 0y) then
                        issue PositionShiftIntoPullOff time
                | None ->
                    ()

                // Check for invalid strings on bass arrangement
                if arrangement.MetaData.ArrangementProperties.PathBass && chordNotes.Exists(fun note -> note.String >= 4y) then
                    issue InvalidBassArrangementString time

            // Check for chords that have LinkNext, but no LinkNext chord notes
            if chord.IsLinkNext && (not chord.HasChordNotes || chord.ChordNotes.TrueForAll(fun cn -> not cn.IsLinkNext)) then
                issue MissingLinkNextChordNotes time

            // Check tone change placement
            if isOnToneChange arrangement time then
                issue ToneChangeOnNote time

            // Check chords at the end of handshape (no "handshape sustain")
            let handShape =
                level.HandShapes.Find(fun hs ->
                    hs.ChordId = chord.ChordId && time >= hs.StartTime && time <= hs.EndTime)

            if notNull handShape && handShape.EndTime - time <= 5 then
                issue ChordAtEndOfHandShape time

            // Check the fingering of the chord and invalid muted strings
            if chord.HasChordNotes && not chord.IsHighDensity then
                if chordHasStrangeFingering arrangement.ChordTemplates chord then
                    issue PossiblyWrongChordFingering chord.Time
                if chordHasBarreOverOpenStrings arrangement.ChordTemplates chord then
                    issue BarreOverOpenStrings chord.Time
                if chordHasMutedString chord then
                    issue MutedStringInNonMutedChord chord.Time

            // Check for chords inside noguitar sections
            if isInsideNoguitarSection ngSections time then
                issue NoteInsideNoguitarSection time
    ]

/// Checks the handshapes in the level for issues.
let checkHandshapes (arrangement: InstrumentalArrangement) (level: Level) =
    let handShapes = level.HandShapes
    let chordTemplates = arrangement.ChordTemplates
    let anchors = level.Anchors

    // Logic to weed out some false positives
    let isSameAnchorWith1stFinger (neighbour: HandShape option) (activeAnchor: Anchor) =
        match neighbour with
        | None ->
            false
        | Some neighbour ->
            let neighbourAnchor = anchors.FindLast(fun a -> a.Time <= neighbour.StartTime)
            let neighbourTemplate = chordTemplates[int neighbour.ChordId]

            neighbourTemplate.Fingers |> Array.contains 1y && neighbourAnchor = activeAnchor

    [
        for i = 0 to handShapes.Count - 1 do
            let handShape = handShapes[i]
            let previous = handShapes |> ResizeArray.tryItem (i - 1)
            let next = handShapes |> ResizeArray.tryItem (i + 1)

            let activeAnchor = anchors.FindLast(fun a -> a.Time <= handShape.StartTime)
            let chordTemplate = chordTemplates[int handShape.ChordId]

            // Check only handshapes that do not use the 1st finger or the thumb
            if not (chordTemplate.Fingers |> Array.exists (fun f -> f = 0y || f = 1y)) && notNull activeAnchor then
                let chordNotOk =
                    (chordTemplate.Frets, chordTemplate.Fingers)
                    ||> Array.exists2 (fun fret finger -> fret = activeAnchor.Fret && finger <> -1y)

                if chordNotOk && not (isSameAnchorWith1stFinger previous activeAnchor ||
                                      isSameAnchorWith1stFinger next activeAnchor) then
                    issue FingeringAnchorMismatch handShape.StartTime
    ]

/// Looks for anchors that are very close to a note but not exactly on a note.
let private findCloseAnchors (level: Level) =
    let pickTimeAndDistance (anchor: Anchor) (noteTime: int) =
        let distance = anchor.Time - noteTime
        if distance <> 0 && abs distance <= 5 then
            Some(anchor.Time, distance)
        else
            None

    let noteAndChordTimes =
        level.Notes
        |> Seq.map (fun n -> n.Time)
        |> Seq.append (level.Chords |> Seq.map (fun c -> c.Time))
        |> Seq.sort
        |> Seq.distinct
        |> Array.ofSeq

    let anchorsNearNoteOrChord =
        level.Anchors
        |> Seq.choose (fun anchor ->
            match Array.BinarySearch(noteAndChordTimes, anchor.Time) with
            | index when index < 0 ->
                noteAndChordTimes
                |> Array.tryPick (pickTimeAndDistance anchor)
            | _ ->
                None)

    let slideEndTimes =
        let chordSlides =
            level.Chords
            |> Seq.choose (fun chord ->
                if chord.HasChordNotes then
                    chord.ChordNotes
                    |> ResizeArray.tryFind (fun n -> n.IsSlide)
                    |> Option.map (fun n -> n.Time + n.Sustain)
                else
                    None)

        level.Notes
        |> Seq.choose (fun note -> if note.IsSlide then Some (note.Time + note.Sustain) else None)
        |> Seq.append chordSlides
        |> Set.ofSeq

    anchorsNearNoteOrChord
    |> Seq.filter (fun (time, _) -> not <| slideEndTimes.Contains time)
    |> Seq.map (fun (anchorTime, distance) ->
        issue (AnchorNotOnNote distance) anchorTime)

/// Looks for anchors that will break a handshape.
let private findAnchorsInsideHandShapes isMoverPhraseTime phraseTimes (level: Level) =
    level.Anchors
    |> Seq.filter (fun anchor ->
        level.HandShapes.Exists(fun hs -> anchor.Time > hs.StartTime && anchor.Time < hs.EndTime)
        && not (isMoverPhraseTime anchor.Time))
    |> Seq.map (fun anchor ->
        if phraseTimes |> Set.contains anchor.Time then
            issue AnchorInsideHandShapeAtPhraseBoundary anchor.Time
        else
            issue AnchorInsideHandShape anchor.Time)

/// Looks for anchors very close to the end of unpitched slide notes.
let private findUnpitchedSlideAnchors isMoverPhraseTime (level: Level) =
    let slideEnds =
        level.Notes
        |> Seq.filter (fun n -> n.IsUnpitchedSlide)
        |> Seq.map (fun n -> n.Time + n.Sustain)
        |> Seq.toArray

    level.Anchors
    |> Seq.filter (fun anchor ->
        Array.exists (fun time -> abs (anchor.Time - time) < 4) slideEnds
        && not (isMoverPhraseTime anchor.Time))
    |> Seq.map (fun anchor -> issue AnchorCloseToUnpitchedSlide anchor.Time)

/// Checks the anchors in the level for issues.
let checkAnchors (arrangement: InstrumentalArrangement) (level: Level) =
    let isMoverPhraseTime =
        let moverPhraseTimes =
            arrangement.Phrases
            |> Seq.indexed
            |> Seq.filter (fun (_, phrase) -> String.startsWith "mover" phrase.Name)
            |> Seq.collect (fun (index, _) ->
                arrangement.PhraseIterations.FindAll(fun pi -> pi.PhraseId = index))
            |> Seq.map (fun x -> x.Time)
            |> Array.ofSeq

        fun time ->
            // Allow 1ms deviation
            moverPhraseTimes
            |> Array.exists (fun t -> abs (t - time) <= 1)

    let phraseTimes =
        let phraseTimes =
            arrangement.PhraseIterations
            |> Seq.map (fun pi -> pi.Time)

        let sectionTimes =
            arrangement.Sections
            |> Seq.map (fun s -> s.Time)

        phraseTimes
        |> Seq.append sectionTimes
        |> Set.ofSeq

    findCloseAnchors level
    |> Seq.append (findAnchorsInsideHandShapes isMoverPhraseTime phraseTimes level)
    |> Seq.append (findUnpitchedSlideAnchors isMoverPhraseTime level)
    |> Seq.toList

let private getComparer<'a when 'a :> IHasTimeCode> () =
    { new Comparer<'a>() with member _.Compare(x, y) = compare x.Time y.Time }

let private noteOrChordExistsAtTime (level: Level) (time: int) =
    level.Notes.BinarySearch(Note(Time = time), getComparer()) >= 0
    || level.Chords.BinarySearch(Chord(Time = time), getComparer()) >= 0

let private isMover1Phrase (phrase: Phrase) =
    phrase.Name.Equals("mover1", StringComparison.OrdinalIgnoreCase)

/// Checks the phrases in the arrangement for issues.
let checkPhrases (arr: InstrumentalArrangement) =
    if arr.PhraseIterations.Count >= 2 then
        let firstNoteTime = Utils.getFirstNoteTime arr

        let incorrectMover1Phrases =
            arr.PhraseIterations
            |> Seq.choose (fun pi ->
                let level = arr.Levels[0]
                if isMover1Phrase arr.Phrases[pi.PhraseId] && noteOrChordExistsAtTime level pi.Time then
                    Some (issue IncorrectMover1Phrase pi.Time)
                else
                    None)

        [
            // Check for notes inside the first phrase
            match firstNoteTime with
            | Some firstNoteTime when firstNoteTime < arr.PhraseIterations[1].Time ->
                issue FirstPhraseNotEmpty firstNoteTime
            | _ ->
                ()

            // Check for missing END phrase
            if not <| arr.Phrases.Exists(fun p -> String.equalsIgnoreCase "END" p.Name) then
                issue NoEndPhrase 0

            // Check for more than 100 phrases
            if arr.PhraseIterations.Count > 100 then
                issue MoreThan100Phrases 0

            // Check phrases that are moved
            yield! incorrectMover1Phrases
        ]
    else
        List.empty

let private getInstrumentalChecks arr =
    [| checkNotes arr
       checkChords arr
       checkHandshapes arr
       checkAnchors arr |]

let private parallelizeInstrumentalCheck arr =
    let checks = getInstrumentalChecks arr
    if arr.Levels.Count = 1 then
        let level = arr.Levels[0]
        checks
        |> Array.Parallel.map (fun check -> check level)
    else
        arr.Levels.ToArray()
        |> Array.Parallel.collect (fun level -> Array.map (fun check -> check level) checks)
    |> List.concat

let private allChecks =
    [ checkCrowdEventPlacement
      checkPhrases
      parallelizeInstrumentalCheck ]

/// Runs all the checks on the given arrangement.
let runAllChecks (arr: InstrumentalArrangement) =
    allChecks
    |> List.collect (fun check -> check arr)
    |> List.distinct
    |> List.sortBy (fun issue -> issue.TimeCode)
