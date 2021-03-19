module Rocksmith2014.XML.Processing.EOFFixes

open Rocksmith2014.XML
open System.Text.RegularExpressions

/// Adds linknext to chords that have linknext chord notes, but are missing the attribute.
let fixChordLinkNext (arrangement: InstrumentalArrangement) =
    arrangement.Levels
    |> Seq.collect (fun l -> l.Chords)
    |> Seq.filter (fun chord ->
        chord.HasChordNotes
        && not chord.IsLinkNext
        && chord.ChordNotes.Exists(fun cn -> cn.IsLinkNext))
    |> Seq.iter (fun chord -> chord.IsLinkNext <- true)

/// Fixes incorrect crowd events: E0, E1, E2.
let fixCrowdEvents (arrangement: InstrumentalArrangement) =
    for event in arrangement.Events do
        if Regex.IsMatch(event.Code, "^E[0-2]$") then
            event.Code <- event.Code.ToLowerInvariant()

/// Shortens handshapes of chords that include the slide-to notes.
let fixChordSlideHandshapes (arrangement: InstrumentalArrangement) =
    for level in arrangement.Levels do
        level.Chords
        |> Seq.filter (fun chord ->
            chord.IsLinkNext
            && chord.HasChordNotes
            && chord.ChordNotes.Exists(fun cn -> cn.IsSlide))
        |> Seq.iter (fun chord ->
            let handshape = level.HandShapes.Find(fun hs -> hs.StartTime = chord.Time)
            if not <| isNull handshape && handshape.EndTime > handshape.StartTime + chord.ChordNotes.[0].Sustain then
                handshape.EndTime <- handshape.StartTime + chord.ChordNotes.[0].Sustain)
    
/// Moves the first anchor of a phrase to the start time of the phrase if needed.
let fixPhraseStartAnchors (arrangement: InstrumentalArrangement) =
    // If there are DD levels, assume that the anchors are correct
    if arrangement.Levels.Count = 1 then
        arrangement.PhraseIterations
        |> Seq.pairwise
        |> Seq.iter (fun (first, second) ->
            let firstAnchor =
                arrangement.Levels.[0].Anchors.Find(fun a -> a.Time >= first.Time && a.Time < second.Time)
            if not <| isNull firstAnchor && firstAnchor.Time <> first.Time then
               firstAnchor.Time <- first.Time)
    
/// Applies all the fixes.
let fixAll arrangement =
    fixCrowdEvents arrangement
    fixChordLinkNext arrangement
    fixChordSlideHandshapes arrangement
    fixPhraseStartAnchors arrangement
