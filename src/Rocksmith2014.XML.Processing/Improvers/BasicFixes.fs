module Rocksmith2014.XML.Processing.BasicFixes

open Rocksmith2014.XML
open System.Text.RegularExpressions

/// Filters the characters in the arrangement's phrase names.
///
/// Allow only characters that are used in official files.
/// Double quotes in a phrase name can corrupt the save file.
let validatePhraseNames (arrangement: InstrumentalArrangement) =
    arrangement.Phrases
    |> Seq.iter (fun phrase ->
        phrase.Name <- Regex.Replace(phrase.Name, "[^a-zA-Z0-9 _#]", ""))
