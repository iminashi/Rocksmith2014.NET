module Rocksmith2014.DLCProject.Manifest.Techniques

open Rocksmith2014.SNG

let inline hasFlag (n: Note) flag = (n.Mask &&& flag) = flag

let isPowerChord sng note =
    hasFlag note NoteMask.DoubleStop
    &&
    let s1 = Array.findIndex (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    let s2 = Array.findIndexBack (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    let f1 = Array.find (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    let f2 = Array.findBack (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    // Root on D string or lower
    s1 <= 2 && s1 + 1 = s2 && f1 + 2y = f2

let private isDropTuning (tuning: int16 array) =
    tuning.[0] = tuning.[1] - 2s && tuning.[0] = tuning.[2] - 2s

let isDropDPower sng note =
    hasFlag note NoteMask.Chord
    && isDropTuning sng.MetaData.Tuning
    &&
    let frets = sng.Chords.[note.ChordId].Frets
    frets.[0] <> -1y && frets.[0] = frets.[1] && frets.[0] = frets.[2]

let isChord note =
    hasFlag note NoteMask.Chord
    && not (hasFlag note NoteMask.Sustain)
    && not (hasFlag note NoteMask.DoubleStop)

let isBarre sng note =
    hasFlag note NoteMask.Chord && not (hasFlag note NoteMask.DoubleStop)
    &&
    sng.Chords.[note.ChordId].Fingers
    |> Array.countBy id
    |> Array.exists (fun (finger, count) -> finger <> -1y && count >= 3)

let isObliqueBend sng note =
    hasFlag note NoteMask.ChordNotes
    &&
    sng.ChordNotes.[note.ChordNotesId].BendData
    |> Array.sumBy (fun b -> if b.UsedCount > 0 then 1 else 0) = 1

let isCompoundBend note =
    hasFlag note NoteMask.Bend && note.BendData.Length >= 3
    && note.BendData.[0].Step <> note.BendData.[1].Step
    && note.BendData.[1].Step <> note.BendData.[2].Step

let isPreBend note =
    hasFlag note NoteMask.Bend
    &&
    let firstBend = note.BendData.[0]
    firstBend.Time = note.Time && firstBend.Step > 0.f

let isChordHammerOn sng note =
    hasFlag note NoteMask.ChordNotes
    &&
    sng.ChordNotes.[note.ChordNotesId].Mask
    |> Array.exists (fun x -> (x &&& NoteMask.HammerOn) <> NoteMask.None)

let isDoubleStopAdjacentStrings sng note =
    hasFlag note NoteMask.DoubleStop
    &&
    let s1 = Array.findIndex (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    let s2 = Array.findIndexBack (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    s1 + 1 = s2

let isDoubleStopNonAdjacentStrings sng note =
    hasFlag note NoteMask.DoubleStop && not <| isDoubleStopAdjacentStrings sng note

let isChordSlide sng note =
    hasFlag note NoteMask.ChordNotes && not (hasFlag note NoteMask.DoubleStop)
    &&
    sng.ChordNotes.[note.ChordNotesId].SlideTo |> Array.exists (fun x -> x <> -1y)

let isChordTremolo sng note =
    hasFlag note NoteMask.ChordNotes && not (hasFlag note NoteMask.DoubleStop)
    &&
    sng.ChordNotes.[note.ChordNotesId].Mask
    |> Array.exists (fun x -> (x &&& NoteMask.Tremolo) <> NoteMask.None)

let isDoubleStopSlide sng note =
    hasFlag note (NoteMask.DoubleStop ||| NoteMask.ChordNotes)
    &&
    sng.ChordNotes.[note.ChordNotesId].SlideTo |> Array.exists (fun x -> x <> -1y)

let isDoubleStopTremolo sng note =
    hasFlag note (NoteMask.DoubleStop ||| NoteMask.ChordNotes)
    &&
    sng.ChordNotes.[note.ChordNotesId].Mask
    |> Array.exists (fun x -> (x &&& NoteMask.Tremolo) <> NoteMask.None)

let isDoubleStopBend sng note =
    hasFlag note (NoteMask.DoubleStop ||| NoteMask.ChordNotes)
    &&
    sng.ChordNotes.[note.ChordNotesId].BendData |> Array.exists (fun x -> x.UsedCount > 0)
