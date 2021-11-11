module Rocksmith2014.XML.Processing.EOFFixes

open Rocksmith2014.XML
open Rocksmith2014.XML.Extensions
open System.Text.RegularExpressions

/// Adds linknext to chords that have linknext chord notes, but are missing the attribute.
let addMissingChordLinkNext (arrangement: InstrumentalArrangement) =
    arrangement.Levels
    |> Seq.collect (fun l -> l.Chords)
    |> Seq.filter (fun chord ->
        chord.HasChordNotes
        && not chord.IsLinkNext
        && chord.ChordNotes.Exists(fun cn -> cn.IsLinkNext))
    |> Seq.iter (fun chord -> chord.IsLinkNext <- true)

/// Removes linknext from chord notes that are not immediately followed by a note on the same string.
let removeInvalidChordNoteLinkNexts (arrangement: InstrumentalArrangement) =
    let fixChordNote (level: Level) (cn: Note) =
        match level.Notes.Find(fun n -> n.Time > cn.Time && n.String = cn.String) with
        | null ->
            cn.IsLinkNext <- false
        | note when note.Time - cn.Time - cn.Sustain > 2 ->
            cn.IsLinkNext <- false
        | _ ->
            ()

    arrangement.Levels
    |> ResizeArray.iter (fun level ->
        level.Chords
        |> Seq.filter (fun chord -> chord.IsLinkNext && chord.HasChordNotes)
        |> Seq.iter (fun chord ->
            chord.ChordNotes
            |> ResizeArray.iter (fun cn -> if cn.IsLinkNext then fixChordNote level cn)))

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
            let handshape =
                level.HandShapes.Find(fun hs -> hs.StartTime = chord.Time)

            if notNull handshape && handshape.EndTime > handshape.StartTime + chord.ChordNotes[0].Sustain then
                handshape.EndTime <- handshape.StartTime + chord.ChordNotes[0].Sustain)

/// Ensures that there is an anchor at the start of each phrase.
let fixPhraseStartAnchors (arrangement: InstrumentalArrangement) =
    // If there are DD levels, assume that the anchors are correct
    if arrangement.Levels.Count = 1 then
        let anchors = arrangement.Levels[0].Anchors

        // Skip the COUNT and END phrases
        for i = 1 to arrangement.PhraseIterations.Count - 2 do
            let piTime = arrangement.PhraseIterations[i].Time
            let activeAnchor = anchors.FindLast(fun a -> a.Time <= piTime)

            if notNull activeAnchor && activeAnchor.Time <> piTime then
                // If an active anchor exists, copy it to the start of the phrase
                anchors.InsertByTime(Anchor(activeAnchor.Fret, piTime, activeAnchor.Width))
            elif isNull activeAnchor then
                // Otherwise try to find the next anchor
                let nextAnchor = anchors.Find(fun a -> a.Time > piTime)

                if notNull nextAnchor then
                    nextAnchor.Time <- piTime

/// Applies all the fixes.
let fixAll arrangement =
    fixCrowdEvents arrangement
    addMissingChordLinkNext arrangement
    removeInvalidChordNoteLinkNexts arrangement
    fixChordSlideHandshapes arrangement
    fixPhraseStartAnchors arrangement
