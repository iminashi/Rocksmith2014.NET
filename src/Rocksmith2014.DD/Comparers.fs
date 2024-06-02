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

let private findNextMatch equal skipped1 skipped2 list1 list2 =
    let rec search skipped1 skipped2 remaining1 remaining2 =
        match remaining1, remaining2 with
        | [], _
        | _, [] ->
            ValueNone
        | head1 :: tail1, head2 :: tail2 when equal head1 head2 ->
            ValueSome (tail1, tail2)
        | head1 :: tail1, head2 :: tail2 ->
            match List.tryFindIndex (equal head2) skipped1 with
            | Some i ->
                ValueSome (List.skip i list1, tail2)
            | None ->
                match List.tryFindIndex (equal head1) skipped2 with
                | Some j ->
                    ValueSome (tail1, List.skip j list2)
                | None ->
                    search (head1 :: skipped1) (head2 :: skipped2) tail1 tail2

    search skipped1 skipped2 list1 list2

/// Calculates the number of same items in two lists, when the order of the items matters.
let getSameItemCount (equal: 'a -> 'a -> bool) (input1: 'a list) (input2: 'a list) =
    let rec getCount count len1 len2 list1 list2 =
        match list1, list2 with
        | head1 :: tail1, head2 :: tail2 when equal head1 head2 ->
            // First items match, continue count
            getCount (count + 1) (len1 - 1) (len2 - 1) tail1 tail2
        | head1 :: tail1, head2 :: tail2 ->
            // Skip items until the list lengths are the same
            if len1 > len2 then
                getCount count (len1 - 1) len2 tail1 list2
            elif len1 < len2 then
                getCount count len1 (len2 - 1) list1 tail2
            else
                // Same lengths, find the next matching items, taking into account skipped items
                match findNextMatch equal [head1] [head2] tail1 tail2 with
                | ValueNone ->
                    count
                | ValueSome (newTail1, newTail2) ->
                    let newLen1 = List.length newTail1
                    let newLen2 = List.length newTail2

                    getCount (count + 1) newLen1 newLen2 newTail1 newTail2
        | _ ->
            count

    // Precalculate since getting the list length is O(N)
    let l1 = List.length input1
    let l2 = List.length input2

    getCount 0 l1 l2 input1 input2

/// Calculates the similarity in percents between the two lists.
let getSimilarityPercent eq l1 l2 =
    match l1, l2 with
    | [], [] ->
        100.
    | [], _ | _, [] ->
        0.
    | _ ->
        let sameCount = getSameItemCount eq l1 l2 |> float
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
