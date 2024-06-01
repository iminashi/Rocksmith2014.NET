module Rocksmith2014.DD.Comparers

open Rocksmith2014.XML

let sameBendValues (bends1: ResizeArray<BendValue>) (bends2: ResizeArray<BendValue>) =
    match bends1, bends2 with
    | null, null ->
        true
    | null, _ | _, null  ->
        false
    | bends1, bends2 when bends1.Count <> bends2.Count ->
        false
    | both ->
        both ||> Seq.forall2 (fun b1 b2 -> b1.Step = b2.Step)

let sameNote (n1: Note) (n2: Note) =
    n1.Fret = n2.Fret
    && n1.String = n2.String
    && n1.Mask = n2.Mask
    && n1.SlideTo = n2.SlideTo
    && n1.SlideUnpitchTo = n2.SlideUnpitchTo
    && n1.Vibrato = n2.Vibrato
    && n1.Tap = n2.Tap
    && n1.MaxBend = n2.MaxBend

let sameNotes (notes1: Note list) (notes2: Note list) =
    if notes1.Length <> notes2.Length then
        false
    else
        (notes1, notes2) ||> List.forall2 sameNote

let sameChordNotes (c1: Chord) (c2: Chord) =
    match c1.ChordNotes, c2.ChordNotes with
    | null, null ->
        true
    | null, _ | _, null  ->
        false
    | cns1, cns2 when cns1.Count <> cns2.Count ->
        false
    | both ->
        both ||> Seq.forall2 sameNote

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
    let rec getCount count len1 len2 list1 list2 =
        match list1, list2 with
        | head1 :: tail1, head2 :: tail2 when eq head1 head2 ->
            getCount (count + 1) (len1 - 1) (len2 - 1) tail1 tail2
        | _ :: tail1, _ :: tail2 ->
            if len1 > len2 then
                getCount count (len1 - 1) len2 tail1 list2
            elif len1 < len2 then
                getCount count len1 (len2 - 1) list1 tail2
            else
                getCount count (len1 - 1) (len2 - 1) tail1 tail2
        | _ ->
            count

    // Precalculate since getting the list length is O(N)
    let l1 = List.length elems1
    let l2 = List.length elems2

    getCount 0 l1 l2 elems1 elems2

/// Calculates the similarity in percents between the two lists.
let getSimilarityPercent eq l1 l2 =
    match l1, l2 with
    | [], [] ->
        100.
    | [], _ | _, [] ->
        0.
    | _ ->
        let sameCount = getSameElementCount eq l1 l2 |> float
        let maxCount = max l1.Length l2.Length |> float
        100. * sameCount / maxCount

/// Calculates the maximum possible similarity in percents between two lists based on their lengths.
let getMaxSimilarityFastest l1 l2 =
    match l1, l2 with
    | [], [] ->
        100.
    | [], _ | _, [] ->
        0.
    | _ ->
        let maxLength = max l1.Length l2.Length
        let minLength = min l1.Length l2.Length
        100. * float minLength / float maxLength

let chordProjection (chord: Chord) = chord.ChordId
let noteProjection (note: Note) = int16 note.String ||| (int16 note.Fret <<< 8)

/// Calculates the maximum possible similarity in percents between two lists by
/// finding the number of same elements based on a projection.
let getMaxSimilarityFast projection l1 l2 =
    let createMap = (List.countBy projection) >> Map.ofList

    match l1, l2 with
    | [], [] ->
        100.
    | [], _ | _, [] ->
        0.
    | _ ->
        let map1 = createMap l1
        let map2 = createMap l2

        let sameCount =
            (0, map1)
            ||> Map.fold (fun state key count1 ->
                match map2.TryFind key with
                | Some count2 ->
                    state + min count1 count2
                | None ->
                    state)

        let maxLength = max l1.Length l1.Length
        100. * float sameCount / float maxLength
