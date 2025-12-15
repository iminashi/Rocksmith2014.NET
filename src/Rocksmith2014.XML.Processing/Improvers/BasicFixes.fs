module Rocksmith2014.XML.Processing.BasicFixes

open System.Collections.Generic
open System.Text.RegularExpressions
open Rocksmith2014.XML

/// Filters the characters in the arrangement's phrase names.
///
/// Allow only characters that are used in official files.
/// Double quotes in a phrase name can corrupt the save file.
let validatePhraseNames (arrangement: InstrumentalArrangement) =
    arrangement.Phrases
    |> ResizeArray.iter (fun phrase ->
        phrase.Name <- Regex.Replace(phrase.Name, "[^a-zA-Z0-9 _#]", ""))

/// Adds ignore to 23rd and 24th fret notes and chords that contain such frets.
/// Also adds ignore to 7th fret harmonics with sustains.
let addIgnores (arrangement: InstrumentalArrangement) =
    let ignoreHarmonic (n: Note) =
        n.Fret = 7y && n.Sustain > 0 && n.IsHarmonic

    arrangement.Levels
    |> ResizeArray.iter (fun level ->
        level.Notes
        |> ResizeArray.iter (fun n ->
            if n.Fret >= 23y || ignoreHarmonic n then n.IsIgnore <- true)

        level.Chords
        |> ResizeArray.iter (fun c ->
            if c.HasChordNotes && c.ChordNotes.Exists(ignoreHarmonic) then
                c.IsIgnore <- true

            arrangement.ChordTemplates
            |> ResizeArray.tryItem (int c.ChordId)
            |> Option.iter (fun template ->
                if template.Frets |> Array.exists (fun f -> f >= 23y) then
                    c.IsIgnore <- true)
        )
    )

/// Fixes various link next errors for notes.
let fixLinkNexts (arrangement: InstrumentalArrangement) =
    arrangement.Levels
    |> ResizeArray.iter (fun level ->
        let notes = level.Notes
        for i = 0 to notes.Count - 1 do
            let note = notes[i]
            if note.IsLinkNext then
                match Utils.tryFindNextNoteOnSameString notes i note with
                | None ->
                    note.IsLinkNext <- false
                | Some nextNote when nextNote.Time - (note.Time + note.Sustain) > 50 ->
                    note.IsLinkNext <- false
                | Some nextNote ->
                    let correctFret =
                        if note.SlideTo > 0y then
                            note.SlideTo
                        elif note.SlideUnpitchTo > 0y then
                            note.SlideUnpitchTo
                        else
                            note.Fret

                    nextNote.Fret <- correctFret

                    if note.IsBend then
                        let thisNoteLastBendValue =
                            note.BendValues[note.BendValues.Count - 1].Step

                        if thisNoteLastBendValue > 0.0f then
                            if not nextNote.IsBend then
                                nextNote.MaxBend <- thisNoteLastBendValue
                                nextNote.BendValues <- ResizeArray.singleton (BendValue(nextNote.Time, thisNoteLastBendValue))
                            elif nextNote.BendValues[0].Time <> nextNote.Time then
                                if nextNote.MaxBend < thisNoteLastBendValue then nextNote.MaxBend <- thisNoteLastBendValue
                                nextNote.BendValues.Insert(0, BendValue(nextNote.Time, thisNoteLastBendValue)))

/// Removes overlapping bend values from notes and chord notes.
let removeOverlappingBendValues (arrangement: InstrumentalArrangement) =
    let filterBendValues (note: Note) =
        if note.IsBend then
            note.BendValues <-
                note.BendValues
                |> Seq.distinctBy (fun bv -> bv.Time)
                |> ResizeArray

    arrangement.Levels
    |> ResizeArray.iter (fun level ->
        level.Notes
        |> ResizeArray.iter filterBendValues

        level.Chords
        |> ResizeArray.iter (fun c ->
            if c.HasChordNotes then
                c.ChordNotes
                |> ResizeArray.iter filterBendValues)
    )

// Removes fret-hand-muted notes from chords that also contain normal notes.
let removeMutedNotesFromChords (arrangement: InstrumentalArrangement) =
    let fixedChordTemplates = HashSet<int16>()

    arrangement.Levels
    |> ResizeArray.iter (fun level ->
        level.Chords
        |> ResizeArray.iter (fun chord ->
            if fixedChordTemplates.Contains(chord.ChordId) |> not && chord.HasChordNotes && not chord.IsFretHandMute then
                let mutedNotes =
                    chord.ChordNotes.FindAll(fun n -> n.IsFretHandMute)

                // Remove the mutes unless all notes are muted
                if mutedNotes.Count > 0 && mutedNotes.Count <> chord.ChordNotes.Count then
                    chord.ChordNotes.RemoveAll(fun n -> n.IsFretHandMute) |> ignore

                    // Fix chord template
                    arrangement.ChordTemplates
                    |> ResizeArray.tryItem (int chord.ChordId)
                    |> Option.iter (fun template ->
                        mutedNotes.ForEach(fun n ->
                            let i = int n.String
                            template.Frets[i] <- -1y
                            template.Fingers[i] <- -1y)
                    )

                    fixedChordTemplates.Add(chord.ChordId) |> ignore
        )
    )

/// Removes anchors that are identical to the previous one.
let removeRedundantAnchors (arrangement: InstrumentalArrangement) =
    let phraseTimes =
        arrangement.PhraseIterations
        |> Seq.map (fun pi -> pi.Time)
        |> Set.ofSeq

    arrangement.Levels
    |> ResizeArray.iter (fun level ->
        let anchors = level.Anchors
        if anchors.Count > 0 then
            let result = ResizeArray<Anchor>(anchors.Count)
            result.Add(anchors[0])

            for i = 1 to anchors.Count - 1 do
                let previousAnchor = anchors[i - 1]
                let currentAnchor = anchors[i]
                let isIdenticalAnchor =
                    previousAnchor.Fret = currentAnchor.Fret && previousAnchor.Width = currentAnchor.Width
                if not isIdenticalAnchor || phraseTimes.Contains currentAnchor.Time then
                    result.Add(currentAnchor)

            level.Anchors <- result)
