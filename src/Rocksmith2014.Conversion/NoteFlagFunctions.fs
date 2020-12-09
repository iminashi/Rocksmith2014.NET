module Rocksmith2014.Conversion.NoteFlagFunctions

open Rocksmith2014.SNG

// Functions for flagging notes (0 = None, 1 = The note is numbered).

/// Numbers all notes.
let always (_: Note option) (currentNote: Note) =
    if currentNote.FretId <> 0y then 1u else 0u

/// Never numbers notes.
let never (_: Note option) (_: Note) = 0u

/// Numbers a note when the anchor changes.
let onAnchorChange (previousNote: Note option) (currentNote: Note) =
    match previousNote with
    // Never number open notes
    | _ when currentNote.FretId = 0y -> 0u
    | Some previous when previous.AnchorFretId <> currentNote.AnchorFretId -> 1u
    | Some _ -> 0u
    | None -> 1u
