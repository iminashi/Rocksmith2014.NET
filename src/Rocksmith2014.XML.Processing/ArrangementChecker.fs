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

let private isInsideNoguitarSection noGuitarSections (note: Note) =
    noGuitarSections
    |> List.exists (fun (_, startTime, endTime) -> note.Time >= startTime && note.Time < endTime)
    
/// Checks the notes in the arrangement for issues.
let checkNotes (arrangement: InstrumentalArrangement) (level: Level) =
    let ngSections =
        arrangement.Sections
        |> Seq.pairwise
        |> Seq.map (fun (first, second) -> first.Name, first.Time, second.Time)
        |> Seq.filter (fun (name, _, _) -> name = "noguitar")
        |> Seq.toList

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

        if isInsideNoguitarSection ngSections note then
            $"Note inside noguitar section at {timeToString note.Time}."
        ]
