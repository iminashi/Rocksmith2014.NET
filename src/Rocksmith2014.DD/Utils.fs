﻿[<AutoOpen>]
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

let getSustain = function
    | XmlNote xn ->
        xn.Sustain
    | XmlChord xc ->
        if isNull xc.ChordNotes then
            0
        else
            xc.ChordNotes.[0].Sustain
