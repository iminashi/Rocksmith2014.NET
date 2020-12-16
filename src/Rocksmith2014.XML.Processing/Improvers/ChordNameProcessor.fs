module Rocksmith2014.XML.Processing.ChordNameProcessor

open Rocksmith2014.XML
open System

let private emptyOrElse name orElse =
    if String.IsNullOrWhiteSpace name then
        String.Empty
    else
        orElse name

/// Processes the chord names in the arrangement.
let improve (arrangement: InstrumentalArrangement) =
    for chordTemplate in arrangement.ChordTemplates do
        // Not implemented: OF (one fret) chord names

        chordTemplate.Name <-
            emptyOrElse chordTemplate.Name (fun name ->
                name.Replace("min", "m")
                    .Replace("CONV", "")
                    .Replace("-nop", "")
                    .Replace("-arp", ""))

        chordTemplate.DisplayName <-
            emptyOrElse chordTemplate.DisplayName (fun dName ->
                dName.Replace("min", "m")
                     .Replace("CONV", "-arp"))
