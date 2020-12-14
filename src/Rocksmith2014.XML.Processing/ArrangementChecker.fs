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
