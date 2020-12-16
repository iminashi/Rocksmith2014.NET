module Rocksmith2014.XML.Processing.EOFFixes

open Rocksmith2014.XML
open System.Text.RegularExpressions

/// Adds linknext on chords without it but which have linknext chord notes.
let fixChordLinkNext (arrangement: InstrumentalArrangement) =
    arrangement.Levels
    |> Seq.collect (fun l -> l.Chords)
    |> Seq.filter (fun chord -> (not <| isNull chord.ChordNotes) && not chord.IsLinkNext && chord.ChordNotes.Exists(fun cn -> cn.IsLinkNext))
    |> Seq.iter (fun chord -> chord.IsLinkNext <- true)

/// Fixes incorrect crowd events: E0, E1, E2.
let fixCrowdEvents (arrangement: InstrumentalArrangement) =
    if not <| isNull arrangement.Events && arrangement.Events.Count > 0 then
        for event in arrangement.Events do
            if Regex.IsMatch(event.Code, "E[0-2]") then
                event.Code <- event.Code.ToLowerInvariant()

/// Shortens handshapes of chords that include the slide-to notes.
let fixChordSlideHandshapes (arrangement: InstrumentalArrangement) =
    for level in arrangement.Levels do
        level.Chords
        |> Seq.filter (fun chord -> chord.IsLinkNext && chord.ChordNotes.Exists(fun cn -> cn.IsSlide))
        |> Seq.iter (fun chord ->
            let handshape = level.HandShapes.Find(fun hs -> hs.StartTime = chord.Time)
            if not <| isNull handshape && handshape.EndTime > handshape.StartTime + chord.ChordNotes.[0].Sustain then
                handshape.EndTime <- handshape.StartTime + chord.ChordNotes.[0].Sustain)
    
/// Applies all the fixes.
let fixAll arrangement =
    fixCrowdEvents arrangement
    fixChordLinkNext arrangement
    fixChordSlideHandshapes arrangement
