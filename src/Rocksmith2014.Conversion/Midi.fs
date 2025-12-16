module Rocksmith2014.Conversion.Midi

open Rocksmith2014.XML

/// The MIDI notes for each string in standard tuning.
let private standardTuningMidiNotes = [| 40; 45; 50; 55; 59; 64 |]

/// Creates a MIDI note from the given properties.
let toMidiNote (string: int) (fret: sbyte) (tuning: int16 array) (capo: sbyte) (isBass: bool) =
    let fret =
        if capo > 0y && fret = 0y then
            int capo
        else
            int fret

    let offset = if isBass then -12 else 0

    standardTuningMidiNotes[string]
    + int tuning[string]
    + fret
    + offset

/// Maps the array of frets into an array of MIDI notes.
let mapToMidiNotes (metaData: MetaData) (frets: sbyte array) =
    Array.init 6 (fun str ->
        if frets[str] = -1y then
            -1
        else
            toMidiNote str frets[str] metaData.Tuning.Strings metaData.Capo metaData.ArrangementProperties.PathBass)
