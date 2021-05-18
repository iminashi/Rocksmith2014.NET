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
    //&& sameBendValues n1.BendValues n2.BendValues

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

let [<Literal>] private MaxSkips = 5

let private skipWhileNot eq elem list =
    let rec doSkip depth remaining =
        match remaining with
        | [] ->
            []
        | _ when depth = MaxSkips ->
            []
        | head::tail when not <| eq head elem ->
            doSkip (depth + 1) tail
        | _ ->
            remaining

    doSkip 0 list

/// Calculates the number of same elements in the lists, when the order of the elements matters.
let getSameElementCount eq elems1 elems2 =
    let rec getCount count list1 list2 =
        match list1, list2 with
        | head1::tail1, head2::tail2 when eq head1 head2 ->
            getCount (count + 1) tail1 tail2
        | _::l::tail1, _::r::tail2 when eq l r ->
            getCount (count + 1) tail1 tail2
        | head1::tail1, head2::tail2 ->
            let tail1' = skipWhileNot eq head2 tail1
            let count1 = getCount count tail1' list2

            let tail2' = skipWhileNot eq head1 tail2
            let count2 = getCount count list1 tail2'

            max count1 count2
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

/// Calculates the maximum possible similarity in percents between two lists based on their lengths.
let getMaxSimilarityFastest l1 l2 =
    match l1, l2 with
    | [], [] -> 100.
    | [], _ | _, [] -> 0.
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
    | [], [] -> 100.
    | [], _ | _, [] -> 0.
    | _ ->
        let map1 = createMap l1
        let map2 = createMap l2

        let sameCount =
            (0, map1)
            ||> Map.fold (fun state key count1 ->
                match map2 |> Map.tryFind key with
                | Some count2 ->
                    state + count1 + count2 - max count1 count2
                | None ->
                    state)

        let maxLength = max l1.Length l1.Length
        100. * float sameCount / float maxLength
