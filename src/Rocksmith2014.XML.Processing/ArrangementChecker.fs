module Rocksmith2014.XML.Processing.ArrangementChecker

open Rocksmith2014.XML
open System
open System.Text.RegularExpressions

type Issue = { Message : string; TimeCode: int }

let private issue msg time = { Message = msg; TimeCode = time }

let private stringNames =
    [| "Low E"
       "A"
       "D"
       "G"
       "B"
       "High E" |]

let timeToString time =
    let minutes = time / 1000 / 60
    let seconds = (time / 1000) - (minutes * 60)
    let milliSeconds = time - (minutes * 60 * 1000) - (seconds * 1000)
    $"{minutes:D2}:{seconds:D2}.{milliSeconds:D3}"

/// Checks for unexpected crowd events between the intro applause events.
let checkCrowdEventPlacement (arrangement: InstrumentalArrangement) =
    let introApplauseStart = arrangement.Events.Find(fun e -> e.Code = "E3")
    let applauseEnd = arrangement.Events.Find(fun e -> e.Code = "E13")
    let crowdEventRegex = Regex("e[0-2]|E3|D3$")

    match introApplauseStart, applauseEnd with
    | null, _ -> []
    | s, null ->
        [ issue $"An intro applause event (E3) without an end event (E13)" s.Time ]
    | s, e ->
        arrangement.Events
        |> Seq.choose (fun ev ->
            if ev.Time > s.Time && ev.Time < e.Time && crowdEventRegex.IsMatch ev.Code then
                Some ev
            else
                None)
        |> Seq.map (fun ev -> issue $"Unexpected event ({ev.Code}) between intro applause events" ev.Time)
        |> Seq.toList

let private getNoguitarSections (arrangement: InstrumentalArrangement) =
    arrangement.Sections
    |> Seq.pairwise
    |> Seq.map (fun (first, second) -> first.Name, first.Time, second.Time)
    |> Seq.filter (fun (name, _, _) -> name = "noguitar")
    |> Seq.toList

let private isInsideNoguitarSection noGuitarSections (time: int) =
    noGuitarSections
    |> List.exists (fun (_, startTime, endTime) -> time >= startTime && time < endTime)

let private isLinkedToChord (level: Level) (note: Note) =
    level.Chords.Exists(fun c -> 
        c.Time = note.Time + note.Sustain
        && not <| isNull c.ChordNotes
        && c.ChordNotes.Exists(fun cn -> cn.String = note.String))

let private findNextNote (notes: ResizeArray<Note>) currentIndex (note: Note) =
    let nextIndex =
        if currentIndex = -1 then
            notes.FindIndex(fun n -> n.Time > note.Time && n.String = note.String)
        else
            notes.FindIndex(currentIndex + 1, fun n -> n.String = note.String)

    if nextIndex = -1 then None else Some notes.[nextIndex]

let private checkLinkNext (level: Level) (currentIndex: int) (note: Note) =
    if isLinkedToChord level note then
        Some (issue $"Note incorrectly linked to a chord" note.Time)
    else
        match findNextNote level.Notes currentIndex note with
        | None -> Some (issue $"Unable to find next note for LinkNext note" note.Time)

        // Check if the frets match
        | Some nextNote when note.Fret <> nextNote.Fret ->
            let slideTo =
                if note.SlideTo = -1y then
                    note.SlideUnpitchTo
                else
                    note.SlideTo
            
            if slideTo <> -1y && slideTo <> nextNote.Fret then
                Some (issue $"LinkNext fret mismatch for slide" note.Time)
            elif slideTo = -1y then
                // Check if the next note is at the end of the sustain for this note
                if nextNote.Time - (note.Time + note.Sustain) > 1 then
                    Some (issue $"Incorrect LinkNext status on note, {stringNames.[int note.String]} string" note.Time)
                else
                    Some (issue $"LinkNext fret mismatch" nextNote.Time)
            else
                None

        // Check if bendValues match
        | Some nextNote when note.IsBend ->
            let thisNoteLastBendValue =
                let last = Seq.last note.BendValues
                last.Step

            // If the next note has bend values and the first one is at the same timecode as the note, compare to that bend value
            let nextNoteFirstBendValue =
                if nextNote.IsBend && nextNote.Time = nextNote.BendValues.[0].Time then
                    nextNote.BendValues.[0].Step
                else 0f

            if thisNoteLastBendValue <> nextNoteFirstBendValue then
                Some (issue $"LinkNext bend mismatch" nextNote.Time)
            else
                None

        | _ -> None
    
/// Checks the notes in the arrangement for issues.
let checkNotes (arrangement: InstrumentalArrangement) (level: Level) =
    let ngSections = getNoguitarSections arrangement

    [ for i = 0 to level.Notes.Count - 1 do
        let note = level.Notes.[i]
        let time = note.Time

        if note.IsLinkNext && note.IsUnpitchedSlide then
            issue $"Unpitched slide note with LinkNext" time
        
        if note.IsHarmonic && note.IsPinchHarmonic then
            issue $"Note set as both harmonic and pinch harmonic" time
        
        if note.Fret >= 23y && not note.IsIgnore then
            let o = if note.Fret = 23y then "rd" else "th"
            issue $"Note on {note.Fret}{o} fret without ignore status" time
        
        if not note.IsIgnore && note.Fret = 7y && note.IsHarmonic && note.Sustain > 0 then 
            issue $"7th fret harmonic note with sustain" time
            
        if note.IsBend && note.BendValues.FindIndex(fun bv -> bv.Step <> 0.0f) = -1 then
            issue $"Note missing a bend value" time

        if not <| isNull arrangement.Tones.Changes && arrangement.Tones.Changes.Exists(fun t -> t.Time = time) then
            issue $"Tone change occurs on a note" time

        if note.IsLinkNext then
            yield! checkLinkNext level i note |> Option.toList

        if isInsideNoguitarSection ngSections time then
            issue $"Note inside noguitar section" time
        ]

/// Checks the chords in the arrangement for issues.
let checkChords (arrangement: InstrumentalArrangement) (level: Level) =
    let ngSections = getNoguitarSections arrangement

    [ for chord in level.Chords do
        let chordNotes = chord.ChordNotes
        let time = chord.Time

        if not <| isNull chordNotes then
            // Check for inconsistent chord note sustains
            if not <| chordNotes.TrueForAll(fun cn -> cn.Sustain = chordNotes.[0].Sustain) then
                issue $"Chord with varying chord note sustains" time

            // Check 7th fret harmonic notes with sustain (and without ignore)
            if not chord.IsIgnore && chordNotes.Exists(fun cn -> cn.Sustain > 0 && cn.Fret = 7y && cn.IsHarmonic) then
                issue $"7th fret harmonic note with sustain" time

            // Check for notes with LinkNext and unpitched slide
            if chordNotes.Exists(fun cn -> cn.IsLinkNext && cn.IsUnpitchedSlide) then
                issue $"Chord note set as unpitched slide note with LinkNext" time

            // Check for notes with both harmonic and pinch harmonic
            if chordNotes.Exists(fun cn -> cn.IsHarmonic && cn.IsPinchHarmonic) then
                issue $"Chord note set as both harmonic and pinch harmonic" time

            // Check 23rd and 24th fret chords without ignore
            if chordNotes.TrueForAll(fun cn -> cn.Fret >= 23y) && not chord.IsIgnore then
                issue $"Chord on 23rd/24th fret without ignore status" time

            if chord.IsLinkNext then
                yield! [ for cn in chordNotes do
                            yield! checkLinkNext level -1 cn |> Option.toList ]

        // Check tone change placement
        if not <| isNull arrangement.Tones.Changes && arrangement.Tones.Changes.Exists(fun t -> t.Time = time) then
            issue $"Tone change occurs on a chord" time

        // Check chords at the end of handshape (no handshape sustain)
        let handShape = level.HandShapes.Find(fun hs -> hs.ChordId = chord.ChordId && time >= hs.StartTime && time <= hs.EndTime)
        if not <| isNull handShape && handShape.EndTime - time <= 5 then
            issue $"Chord at the end of a handshape" time

        // Check for chords inside noguitar sections
        if isInsideNoguitarSection ngSections time then
            issue $"Chord inside noguitar section" time
   ]

/// Checks the handshapes in the arrangement for issues.
let checkHandshapes (arrangement: InstrumentalArrangement) (level: Level) =
    let handShapes = level.HandShapes
    let chordTemplates = arrangement.ChordTemplates
    let anchors = level.Anchors

    // Logic to weed out some false positives
    let isSameAnchorWith1stFinger (neighbour: HandShape option) (activeAnchor: Anchor) =
         match neighbour with
         | None -> false
         | Some neighbour ->
            let neighbourAnchor = anchors.FindLast(fun a -> a.Time <= neighbour.StartTime)
            let neighbourTemplate = chordTemplates.[int neighbour.ChordId]
    
            neighbourTemplate.Fingers |> Array.exists((=) 1y) && neighbourAnchor = activeAnchor

    [ for i = 0 to handShapes.Count - 1 do
        let handShape = handShapes.[i]
        let previous = if i = 0 then None else Some handShapes.[i - 1]
        let next = if i = handShapes.Count - 1 then None else Some handShapes.[i + 1]

        let activeAnchor = anchors.FindLast(fun a -> a.Time <= handShape.StartTime)
        let chordTemplate = chordTemplates.[int handShape.ChordId]
        
        // Check only handshapes that do not use the 1st finger
        if not (chordTemplate.Fingers |> Array.exists ((=) 1y)) then
            let chordNotOk =
                (chordTemplate.Frets, chordTemplate.Fingers)
                ||> Array.exists2 (fun fret finger -> fret = activeAnchor.Fret && finger <> -1y)
            
            if chordNotOk then
                if not (isSameAnchorWith1stFinger previous activeAnchor ||
                        isSameAnchorWith1stFinger next activeAnchor) then
                    issue $"Handshape fingering does not match anchor position" handShape.StartTime
    ]

/// Looks for anchors that are very close to a note but not exactly on a note.
let checkAnchors (level: Level) =
    let pickTimeAndDistance noteTime (anchor: Anchor) =
        let distance = anchor.Time - noteTime 
        if distance <> 0 && Math.Abs(distance) <= 5 then
            Some (anchor.Time, distance)
        else
            None

    let anchorsNearNotes =
        level.Notes
        |> Seq.choose (fun note ->
            Seq.tryPick (pickTimeAndDistance note.Time) level.Anchors)

    let anchorsNearChords =
        level.Chords
        |> Seq.choose (fun chord ->
            Seq.tryPick (pickTimeAndDistance chord.Time) level.Anchors)

    anchorsNearNotes
    |> Seq.append anchorsNearChords
    |> Seq.map (fun (anchorTime, distance) ->
        issue $"Anchor not on a note. Distance to closest note: {distance} ms" anchorTime)
    |> Seq.toList

/// Runs all the checks on the given arrangement.
let runAllChecks (arr: InstrumentalArrangement) =
    let results =
        arr.Levels.ToArray()
        |> Array.Parallel.map (fun level ->
            [ yield! checkNotes arr level
              yield! checkChords arr level
              yield! checkHandshapes arr level
              yield! checkAnchors level ])
        |> List.concat

    [ yield! checkCrowdEventPlacement arr
      yield! results ]
    |> List.distinct
    |> List.sortBy (fun issue -> issue.TimeCode)
