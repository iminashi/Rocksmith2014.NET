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
    && n1.Mask = n2.Mask
    && n1.SlideTo = n2.SlideTo
    && n1.SlideUnpitchTo = n2.SlideUnpitchTo
    && n1.Vibrato = n2.Vibrato
    && n1.Tap = n2.Tap
    && n1.MaxBend = n2.MaxBend
    //&& sameBendValues n1.BendValues n2.BendValues

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

let sameChord (c1: Chord) (c2: Chord) =
    c1.ChordId = c2.ChordId
    && c1.Mask = c2.Mask
    && sameChordNotes c1 c2

let sameChords (chords1: Chord list) (chords2: Chord list) =
    if chords1.Length <> chords2.Length then
        false
    else
        (chords1, chords2)
        ||> List.forall2 sameChord

/// Calculates the number of same elements in the lists, when the order of the elements matters.
let getSameElementCount eq elems1 elems2 =
    let rec getCount count l1 l2 =
        match l1, l2 with
        | h1::t1, h2::t2 ->
            if eq h1 h2 then
                 getCount (count + 1) t1 t2
            else
                let t1' = List.skipWhile ((eq h2) >> not) t1
                let count1 = getCount count t1' l2

                if t1.Length = t1'.Length then
                    count1
                else
                    let t2' = List.skipWhile ((eq h1) >> not) t2
                    let count2 = getCount count l1 t2'

                    let count3 =
                        if t2.Length = t2'.Length then
                            0
                        else
                            getCount count t1 t2

                    max count1 count2
                    |> max count3
        | _ ->
            count

    getCount 0 elems1 elems2

/// Calculates the similarity in percents between the two lists.
let getSimilarityPercent eq l1 l2 =
    match l1, l2 with
    | [], [] -> 100.
    | [], _ | _, [] -> 0.
    | _ ->
        let sameCount = getSameElementCount eq l1 l2 |> float
        let maxCount = max l1.Length l2.Length |> float
        100. * sameCount / maxCount

/// Calculates the maximum possible similarity in percents between the two lists based on their lengths.
let getMaxSimilarityPercent l1 l2 =
    match l1, l2 with
    | [], [] -> 100.
    | [], _ | _, [] -> 0.
    | _ ->
        let maxLength = max l1.Length l2.Length
        let minLength = min l1.Length l2.Length
        100. * float minLength / float maxLength
