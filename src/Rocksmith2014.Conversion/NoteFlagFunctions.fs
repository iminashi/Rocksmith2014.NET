module Rocksmith2014.Conversion.NoteFlagFunctions

open Rocksmith2014.SNG

// Functions for flagging notes (0 = None, 1 = The note is numbered).

let always (_: ValueOption<Note>) (currentNote: Note) =
    if currentNote.FretId <> 0y then 1u else 0u

let never (_: ValueOption<Note>) (_: Note) = 0u

let onAnchorChange (previousNote: ValueOption<Note>) (currentNote: Note) =
    match previousNote with
    | ValueNone ->
        if currentNote.FretId <> 0y then 1u else 0u
    | ValueSome previous ->
        if previous.AnchorFretId <> currentNote.AnchorFretId && currentNote.FretId <> 0y then 1u else 0u