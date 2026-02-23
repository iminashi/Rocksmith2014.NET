module Rocksmith2014.XML.Processing.DoubleStopNameRemover

open System
open Rocksmith2014.XML

let private isPowerChord (tuning: int16 array) (stringIndexes: int array) (frets: sbyte array) =
    let s1, s2 = stringIndexes[0], stringIndexes[1]
    let f1, f2 = frets[s1], frets[s2]

    // Adjacent strings
    s1 + 1 = s2
    // Interval is a fifth
    && (tuning[s1] + int16 f1) + 2s = (tuning[s2] + int16 f2)

/// Removes names from double stops (excluding the common power chord shape).
let improve (tuning: int16 array) (arrangement: InstrumentalArrangement) =
    for chordTemplate in arrangement.ChordTemplates do
        let stringIndexes =
            chordTemplate.Frets
            |> Array.choosei (fun i fret -> if fret >= 0y then Some i else None)

        if stringIndexes.Length = 2 && not <| isPowerChord tuning stringIndexes chordTemplate.Frets then
            chordTemplate.Name <- String.Empty
            chordTemplate.DisplayName <- String.Empty
