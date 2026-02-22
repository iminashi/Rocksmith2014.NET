module Rocksmith2014.XML.Processing.UnncessaryNoteRemover

open Rocksmith2014.XML

/// Removes notes that come after LinkNext, but have no sustain.
let improve (arrangement: InstrumentalArrangement) =
    for level in arrangement.Levels do
        let notesToRemove = ResizeArray<Note>()
        let notes = level.Notes

        for i = 0 to notes.Count - 1 do
            let note = notes[i]
            if note.IsLinkNext then
                // Find the next note on the same string
                match Utils.tryFindNextNoteOnSameString notes i note with
                | Some nextNote when nextNote.Sustain = 0 ->
                    notesToRemove.Add(nextNote)
                    note.IsLinkNext <- false
                | _ ->
                    ()

        for chord in level.Chords do
            if chord.IsLinkNext && notNull chord.ChordNotes then
                // Find the first note after the chord
                match notes.FindIndex(fun n -> n.Time > chord.Time) with
                | -1 ->
                     ()
                | noteIndex ->
                    // Check all chord notes that have LinkNext
                    chord.ChordNotes
                    |> Seq.filter _.IsLinkNext
                    |> Seq.iter (fun chordNote ->
                        match Utils.tryFindNextNoteOnSameString notes (noteIndex - 1) chordNote with
                        | Some note when note.Sustain = 0 ->
                            chordNote.IsLinkNext <- false
                            notesToRemove.Add(note)
                        | _ ->
                            ()
                    )

                    chord.IsLinkNext <- chord.ChordNotes.Exists(_.IsLinkNext)

        for note in notesToRemove do
            notes.Remove(note) |> ignore
