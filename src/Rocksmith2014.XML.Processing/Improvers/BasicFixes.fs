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
