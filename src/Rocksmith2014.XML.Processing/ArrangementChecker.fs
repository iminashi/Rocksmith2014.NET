module Rocksmith2014.XML.Processing.ArrangementChecker

open Rocksmith2014.XML
open System.Text.RegularExpressions

let private timeToString time =
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
        [ $"There is an intro applause event (E3) at {timeToString s.Time} without an end event (E13)." ]
    | s, e ->
        arrangement.Events
        |> Seq.choose (fun ev ->
            if ev.Time > s.Time && ev.Time < e.Time && crowdEventRegex.IsMatch ev.Code then
                Some ev
            else
                None)
        |> Seq.map (fun ev -> $"Unexpected event ({ev.Code}) between intro applause events at {timeToString ev.Time}.")
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
    
/// Checks the notes in the arrangement for issues.
let checkNotes (arrangement: InstrumentalArrangement) (level: Level) =
    let ngSections = getNoguitarSections arrangement

    [ for note in level.Notes do
        if note.IsLinkNext && note.IsUnpitchedSlide then
            $"Unpitched slide note with LinkNext at {timeToString note.Time}."
        
        if note.IsHarmonic && note.IsPinchHarmonic then
            $"Note set as both harmonic and pinch harmonic at {timeToString note.Time}."
        
        if note.Fret >= 23y && not note.IsIgnore then
            let o = if note.Fret = 23y then "rd" else "th"
            $"Note on {note.Fret}{o} fret without ignore status at {timeToString note.Time}."
        
        if note.Fret = 7y && note.IsHarmonic && note.Sustain > 0 then 
            $"7th fret harmonic note with sustain at {timeToString note.Time}."
            
        if note.IsBend && note.BendValues.FindIndex(fun bv -> bv.Step <> 0.0f) = -1 then
            $"Note missing a bend value at {timeToString note.Time}."

        if not <| isNull arrangement.Tones.Changes && arrangement.Tones.Changes.Exists(fun t -> t.Time = note.Time) then
            $"Tone change occurs on a note at {timeToString note.Time}."

        // TODO: Check linknext

        if isInsideNoguitarSection ngSections note.Time then
            $"Note inside noguitar section at {timeToString note.Time}."
        ]

/// Checks the chords in the arrangement for issues.
let checkChords (arrangement: InstrumentalArrangement) (level: Level) =
    let ngSections = getNoguitarSections arrangement

    [ for chord in level.Chords do
        let chordNotes = chord.ChordNotes

        if not <| isNull chordNotes then
            // Check for inconsistent chord note sustains
            if not <| chordNotes.TrueForAll(fun cn -> cn.Sustain = chordNotes.[0].Sustain) then
                $"Chord with varying chord note sustains at {timeToString chord.Time}."

            // Check 7th fret harmonic notes with sustain (and without ignore)
            if not chord.IsIgnore && chordNotes.Exists(fun cn -> cn.Sustain > 0 && cn.Fret = 7y && cn.IsHarmonic) then
                $"7th fret harmonic note with sustain at {timeToString chord.Time}."

            // Check for notes with LinkNext and unpitched slide
            if chordNotes.Exists(fun cn -> cn.IsLinkNext && cn.IsUnpitchedSlide) then
                $"Chord note set as unpitched slide note with LinkNext at {timeToString chord.Time}."

            // Check for notes with both harmonic and pinch harmonic
            if chordNotes.Exists(fun cn -> cn.IsHarmonic && cn.IsPinchHarmonic) then
                $"Chord note set as both harmonic and pinch harmonic at {timeToString chord.Time}."

            // Check 23rd and 24th fret chords without ignore
            if chordNotes.TrueForAll(fun cn -> cn.Fret >= 23y) && not chord.IsIgnore then
                $"Chord on 23rd/24th fret without ignore status at {timeToString chord.Time}."

            // TODO: Check linknext

        // Check tone change placement
        if not <| isNull arrangement.Tones.Changes && arrangement.Tones.Changes.Exists(fun t -> t.Time = chord.Time) then
            $"Tone change occurs on a chord at {timeToString chord.Time}."

        // Check chords at the end of handshape (no handshape sustain)
        let handShape = level.HandShapes.Find(fun hs -> hs.ChordId = chord.ChordId && chord.Time >= hs.StartTime && chord.Time <= hs.EndTime)
        if not <| isNull handShape && handShape.EndTime - chord.Time <= 5 then
            $"Chord without handshape sustain at {timeToString chord.Time}."

        // Check for chords inside noguitar sections
        if isInsideNoguitarSection ngSections chord.Time then
            $"Chord inside noguitar section at {timeToString chord.Time}."
   ]
