module TechNotes

open Rocksmith2014.EOF.EOFTypes
open System.IO
open BinaryWriterBuilder

[<return: Struct>]
let private (|Combinable|_|) (a: EOFNote) (b: EOFNote) =
    if a.Position = b.Position
        && a.NoteType = b.NoteType
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

let combineTechNotes (techNotes: EOFNote array) =
    let combiner current acc =
        match acc with
        | (Combinable current prev) :: tail ->
            let combined =
                { current with
                    BitFlag = current.BitFlag ||| prev.BitFlag
                    Frets = current.Frets |> Array.append prev.Frets
                    ExtendedNoteFlags = current.ExtendedNoteFlags ||| prev.ExtendedNoteFlags }
            combined :: tail
        | [] ->
            [ current ]
        | _ ->
            current :: acc

    // TODO: Improve
    let separator a acc =
        match acc with
        | b :: tail when a.Position = b.Position ->
            if canMove b then
                let b2 = { b with Position = b.Position + 50u }
                a :: b2 :: tail
            elif canMove a then
                let a2 = { a with Position = a.Position + 50u }
                b :: a2 :: tail
            else
                acc
        | [] ->
            [ a ]
        | _ ->
            a :: acc

    techNotes
    |> Seq.sortBy (fun x -> x.Position, x.NoteType)
    |> fun s -> Seq.foldBack combiner s []
    |> fun s -> Seq.foldBack separator s []
    |> List.toArray

let getTechNoteData (techNotes: EOFNote array) =
    if techNotes.Length > 0 then
        use m = new MemoryStream()
        binaryWriter { techNotes } |> toStream(m)
        m.ToArray()
    else
        Array.empty
