module Rocksmith2014.XML.Processing.BasicFixes

open Rocksmith2014.XML
open System.Text.RegularExpressions

/// Filters the characters in the arrangement's phrase names.
///
/// Allow only characters that are used in official files.
/// Double quotes in a phrase name can corrupt the save file.
let validatePhraseNames (arrangement: InstrumentalArrangement) =
    arrangement.Phrases
    |> ResizeArray.iter (fun phrase ->
        phrase.Name <- Regex.Replace(phrase.Name, "[^a-zA-Z0-9 _#]", ""))

/// Adds ignore to 23rd and 24th fret notes and chords that contain such frets.
let addIgnoreToHighFretNotes (arrangement: InstrumentalArrangement) =
    arrangement.Levels
    |> ResizeArray.iter (fun level ->
        level.Notes
        |> ResizeArray.iter (fun n -> if n.Fret >= 23y then n.IsIgnore <- true)

        level.Chords
        |> ResizeArray.iter (fun c ->
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
                                nextNote.BendValues <- ResizeArray.singleton (BendValue(nextNote.Time, thisNoteLastBendValue))
                            elif nextNote.BendValues[0].Time <> nextNote.Time then
                                nextNote.BendValues.Insert(0, BendValue(nextNote.Time, thisNoteLastBendValue)))
