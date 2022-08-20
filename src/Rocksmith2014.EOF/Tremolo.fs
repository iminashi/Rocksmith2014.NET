module Tremolo

open Rocksmith2014.EOF.EOFTypes

type private TempTremoloSection =
    { Difficulty: byte
      PrevIndex: int
      StartTime: uint
      EndTime: uint }

let createTremoloSections (notes: EOFNote array) =
    notes
    |> Array.indexed
    |> Array.fold (fun acc (i, note) ->
        if note.Flags &&& EOFNoteFlag.TREMOLO = EOFNoteFlag.ZERO then
            acc
        else
            match acc with
            | h :: t when h.PrevIndex = i - 1 ->
                // Extend previous tremolo section
                { h with PrevIndex = i; EndTime = note.Position + note.Length } :: t
            | _ ->
                // Create new tremolo section
                let newTremolo =
                    { Difficulty = note.Difficulty
                      PrevIndex = i
                      StartTime = note.Position
                      EndTime = note.Position + note.Length }

                newTremolo :: acc
    ) []
    |> List.rev
    |> List.map (fun x -> EOFSection.Create(x.Difficulty, x.StartTime, x.EndTime, 0u))
    |> List.toArray
