module Rocksmith2014.DD.Comparers

open Rocksmith2014.XML

let sameBendValues (bends1: ResizeArray<BendValue>) (bends2: ResizeArray<BendValue>) =
    match bends1, bends2 with
    | null, null -> true
    | null, _ | _, null  -> false
    | bends1, bends2 when bends1.Count <> bends2.Count -> false
    | both -> both ||> Seq.forall2 (fun b1 b2 -> b1.Step = b2.Step)

let sameNote (n1: Note) (n2: Note) = 
    n1.Fret = n2.Fret
    && n1.String = n2.String
    && n1.Sustain = n2.Sustain
    && n1.Mask = n2.Mask
    && n1.SlideTo = n2.SlideTo
    && n1.SlideUnpitchTo = n2.SlideUnpitchTo
    && n1.Vibrato = n2.Vibrato
    && sameBendValues n1.BendValues n2.BendValues

let sameNotes (notes1: Note list) (notes2: Note list) =
    if notes1.Length <> notes2.Length then
        false
    else
        (notes1, notes2) ||> List.forall2 sameNote

let sameChordNotes (c1: Chord) (c2: Chord) =
    match c1.ChordNotes, c2.ChordNotes with
    | null, null -> true
    | null, _ | _, null  -> false
    | cns1, cns2 when cns1.Count <> cns2.Count -> false
    | both -> both ||> Seq.forall2 sameNote

let sameChords (chords1: Chord list) (chords2: Chord list) =
    if chords1.Length <> chords2.Length then
        false
    else
        (chords1, chords2)
        ||> List.forall2 (fun c1 c2 ->
            c1.ChordId = c2.ChordId
            && c1.Mask = c2.Mask
            && sameChordNotes c1 c2)
