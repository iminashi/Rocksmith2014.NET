[<AutoOpen>]
module internal Rocksmith2014.DD.Utils

open Rocksmith2014.XML
open System

let getRange<'T when 'T :> IHasTimeCode> startTime endTime (s: ResizeArray<'T>) =
    s.FindAll(fun e -> e.Time >= startTime && e.Time < endTime)
    |> List.ofSeq

let getNoteCount (template: ChordTemplate) =
    template.Frets
    |> Array.sumBy (fun fret -> Convert.ToInt32(fret >= 0y))

let getAllowedChordNotes (diffPercent: float) maxChordNotesInPhrase =
    if maxChordNotesInPhrase = 0 then
        0
    else
        diffPercent * float maxChordNotesInPhrase
        |> (ceil >> int)

/// For double and triple stops on the highest strings, start adding notes from the highest string.
let shouldStartFromHighestNote (totalNotes: int) (template: ChordTemplate) =
    let fr = template.Frets
    (totalNotes = 3 && fr[5] > -1y && fr[4] > -1y && fr[3] > -1y)
    || (totalNotes = 2 && fr[5] > -1y && fr[4] > -1y)

let createTemplateRequest (originalId: int16) (noteCount: int) (totalNotes: int) (template: ChordTemplate) target =
    let startFromHighestNote = shouldStartFromHighestNote totalNotes template

    { OriginalId = originalId
      NoteCount = byte noteCount
      FromHighestNote = startFromHighestNote
      Target = target }
