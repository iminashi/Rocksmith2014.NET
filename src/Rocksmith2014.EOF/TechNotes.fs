module TechNotes

open Rocksmith2014.EOF.EOFTypes
open System.IO
open BinaryWriterBuilder

[<return: Struct>]
let private (|Combinable|_|) (a: EOFNote) (b: EOFNote) =
    if a.Position = b.Position
        // For linknext notes, the last bendvalue may be at the same time as the note linked to
        && a.BitFlag <> b.BitFlag
        && a.Difficulty = b.Difficulty
        && a.BendStrength = b.BendStrength
        && a.SlideEndFret = b.SlideEndFret
        && a.UnpitchedSlideEndFret = b.UnpitchedSlideEndFret
        && a.Flags = b.Flags
        && a.ExtendedNoteFlags = b.ExtendedNoteFlags
    then
        ValueSome b
    else
        ValueNone

let private canMove (tn: EOFNote) =
    tn.Flags &&& EOFNoteFlag.BEND = EOFNoteFlag.ZERO
    && tn.ExtendedNoteFlags &&& EOFExtendedNoteFlag.STOP = EOFExtendedNoteFlag.ZERO

let private combine current prev =
    { current with
        BitFlag = current.BitFlag ||| prev.BitFlag
        Frets = current.Frets |> Array.append prev.Frets
        ExtendedNoteFlags = current.ExtendedNoteFlags ||| prev.ExtendedNoteFlags }

let private movePosition (techNote: EOFNote) =
    { techNote with Position = min (techNote.Position + 50u) techNote.EndPosition }

let private convertToPreBend (techNote: EOFNote) =
    { techNote with
        Flags = techNote.Flags ||| EOFNoteFlag.EXTENDED_FLAGS
        ExtendedNoteFlags = techNote.ExtendedNoteFlags ||| EOFExtendedNoteFlag.PRE_BEND }

let combineTechNotes (techNotes: EOFNote array) =
    let combiner current acc =
        match acc with
        | (Combinable current prev) :: tail ->
            assert (current.BitFlag <> prev.BitFlag)
            (combine current prev) :: tail
        | [] ->
            [ current ]
        | _ ->
            current :: acc

    let separator a acc =
        match acc with
        | b :: tail when a.Position = b.Position && a.Difficulty = b.Difficulty ->
            if canMove b then
                a :: movePosition b :: tail
            elif canMove a then
                b :: movePosition a :: tail
            else
                // Neither tech note can be moved
                // Check if bends can be converted to pre-bends
                if a.Position = a.ActualNotePosition && b.Position = b.ActualNotePosition then
                    if a.Flags &&& EOFNoteFlag.BEND <> EOFNoteFlag.ZERO then
                        let a2 = movePosition a |> convertToPreBend
                        b :: a2 :: tail
                    elif b.Flags &&& EOFNoteFlag.BEND <> EOFNoteFlag.ZERO then
                        // Convert b to pre-bend
                        let b2 = movePosition b |> convertToPreBend
                        a :: b2 :: tail
                    else
                        // Two stop tech notes at note time? Should not end up here
                        a :: acc
                else
                    a :: acc
        | [] ->
            [ a ]
        | _ ->
            a :: acc

    techNotes
    |> Seq.sortBy (fun x -> x.Position, x.Difficulty)
    |> fun s -> Seq.foldBack combiner s []
    |> fun s -> Seq.foldBack separator s []
    |> List.toArray

let getTechNoteData (techNotes: EOFNote array) =
    if techNotes.Length > 0 then
        use m = new MemoryStream()
        binaryWriter { techNotes } |> toStream m
        m.ToArray()
    else
        Array.empty
