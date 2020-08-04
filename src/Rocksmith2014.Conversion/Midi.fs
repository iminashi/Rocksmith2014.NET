module Rocksmith2014.Conversion.Midi

open Rocksmith2014

/// The MIDI notes for each string in standard tuning.
let private standardTuningMidiNotes = [| 40; 45; 50; 55; 59; 64 |]

/// Maps the array of frets into an array of MIDI notes.
let mapToMidiNotes (metaData: XML.MetaData) (frets: sbyte array) =
    Array.init 6 (fun str ->
        if frets.[str] = -1y then
            -1
        else
            let tuning = metaData.Tuning.Strings
            let fret =
                if metaData.Capo > 0y && frets.[str] = 0y then
                    int metaData.Capo
                else
                    int frets.[str]
            let offset = if metaData.ArrangementProperties.PathBass then -12 else 0
            standardTuningMidiNotes.[str] + int tuning.[str] + fret + offset
    )
