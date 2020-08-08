module Rocksmith2014.DLCProject.Manifest.Techniques

open Rocksmith2014.SNG

let hasFlag (n: Note) f = (n.Mask &&& f) <> NoteMask.None

let isPowerChord sng note =
    if hasFlag note NoteMask.DoubleStop then
        let s1 = Array.findIndex (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
        let s2 = Array.findIndexBack (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
        let f1 = Array.find (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
        let f2 = Array.findBack (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
        // Root on D string or lower
        s1 <= 2 && s1 + 1 = s2 && f1 + 2y = f2
    else
        false

let isChord sng note =
    hasFlag note NoteMask.Chord
    && not (hasFlag note NoteMask.Sustain)
    && sng.Chords.[note.ChordId].Frets |> Array.sumBy (fun f -> if f >= 0y then 1 else 0) >= 3

let isObliqueBend sng note =
    hasFlag note NoteMask.ChordNotes
    &&
    sng.ChordNotes.[note.ChordNotesId].BendData
    |> Array.sumBy (fun b -> if b.UsedCount > 0 then 1 else 0) = 1

let isChordBend sng note =
    hasFlag note NoteMask.ChordNotes
    &&
    sng.ChordNotes.[note.ChordNotesId].BendData |> Array.exists (fun x -> x.UsedCount > 0)

let isComplexBend note =
    hasFlag note NoteMask.Bend
    &&
    (not (note.BendData |> Array.forall (fun bv -> bv.Step = note.BendData.[0].Step)))

let isChordHammerOn sng note =
    hasFlag note NoteMask.ChordNotes
    &&
    sng.ChordNotes.[note.ChordNotesId].Mask
    |> Array.exists (fun x -> (x &&& NoteMask.HammerOn) <> NoteMask.None)
