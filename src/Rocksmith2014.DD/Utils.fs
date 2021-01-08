[<AutoOpen>]
module Rocksmith2014.DD.Utils

open Rocksmith2014.XML

let getRange<'T when 'T :> IHasTimeCode> startTime endTime (s: ResizeArray<'T>) =
    s.FindAll(fun e -> e.Time >= startTime && e.Time < endTime)
    |> List.ofSeq

let getNoteCount (template: ChordTemplate) =
    template.Frets
    |> Array.fold (fun acc elem -> if elem >= 0y then acc + 1 else acc) 0

let getTimeCode = function
    | XmlNote xn -> xn.Time
    | XmlChord xc -> xc.Time
