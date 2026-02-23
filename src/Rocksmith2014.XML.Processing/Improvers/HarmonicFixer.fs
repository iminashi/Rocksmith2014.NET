module Rocksmith2014.XML.Processing.HarmonicFixer

open Rocksmith2014.XML

/// Converts notes set as both harmonic and pinch harmonic to pinch harmonics.
let improve (arrangement: InstrumentalArrangement) =
    for level in arrangement.Levels do
        for note in level.Notes do
            if note.IsHarmonic && note.IsPinchHarmonic then
                note.IsHarmonic <- false
