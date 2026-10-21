[<AutoOpen>]
module internal Rocksmith2014.DD.Utils

open System
open Rocksmith2014.XML

let getRange<'T when 'T :> IHasTimeCode> (startTime: int) (endTime: int) (s: ResizeArray<'T>) =
    s.FindAll(fun e -> e.Time >= startTime && e.Time < endTime)
    |> List.ofSeq

let getNoteCount (template: ChordTemplate) =
    template.Frets
    |> Array.sumBy (fun fret -> Convert.ToInt32(fret >= 0y))

/// Orders strings low to high; string 6 precedes string 0.
let pitchRank (stringIndex: sbyte) =
    StringLayout.MusicalRank(int stringIndex, StringLayout.MaxStringCount)

let getAllowedChordNotes (diffPercent: float) (maxChordNotesInPhrase: int) =
    if maxChordNotesInPhrase = 0 then
        0
    else
        diffPercent * float maxChordNotesInPhrase
        |> (ceil >> int)

/// For double and triple stops on the highest strings, start adding notes from the highest string.
let shouldStartFromHighestNote (totalNotes: int) (template: ChordTemplate) =
    let fr = StringLayout.ToLowToHigh(template.Frets)
    let length = fr.Length
    (totalNotes = 3 && length >= 3 && fr[length - 1] > -1y && fr[length - 2] > -1y && fr[length - 3] > -1y)
    || (totalNotes = 2 && length >= 2 && fr[length - 1] > -1y && fr[length - 2] > -1y)

let createTemplateRequest (originalId: int16) (noteCount: int) (totalNotes: int) (template: ChordTemplate) (target: RequestTarget) =
    let startFromHighestNote = shouldStartFromHighestNote totalNotes template

    { OriginalId = originalId
      NoteCount = byte noteCount
      FromHighestNote = startFromHighestNote
      Target = target }
