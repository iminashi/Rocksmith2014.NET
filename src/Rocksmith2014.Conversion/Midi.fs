module Rocksmith2014.Conversion.Midi

open Rocksmith2014

/// The MIDI notes for each string in standard tuning.
let private standardTuningMidiNotes = [| 40; 45; 50; 55; 59; 64 |]

/// Maps the array of frets into an array of MIDI notes.
let mapToMidiNotes (xml: XML.InstrumentalArrangement) (frets: sbyte array) =
    Array.init 6 (fun str ->
        if frets.[str] = -1y then
            -1
        else
            let tuning = xml.Tuning.Strings
            let fret =
                if xml.Capo > 0y && frets.[str] = 0y then
                    int xml.Capo
                else
                    int frets.[str]
            let offset = if xml.ArrangementProperties.PathBass then -12 else 0
            standardTuningMidiNotes.[str] + int tuning.[str] + fret + offset
    )
