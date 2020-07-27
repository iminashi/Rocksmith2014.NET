module Rocksmith2014.Conversion.NoteFlagFunctions

open Rocksmith2014.SNG

// Functions for flagging notes (0 = None, 1 = The note is numbered).

let always (_: Note option) (currentNote: Note) =
    if currentNote.FretId <> 0y then 1u else 0u

let never (_: Note option) (_: Note) = 0u

let onAnchorChange (previousNote: Note option) (currentNote: Note) =
    match previousNote with
    | None ->
        if currentNote.FretId <> 0y then 1u else 0u
    | Some previous ->
        if previous.AnchorFretId <> currentNote.AnchorFretId && currentNote.FretId <> 0y then 1u else 0u