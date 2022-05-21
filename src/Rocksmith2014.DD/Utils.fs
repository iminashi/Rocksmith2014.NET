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
