module Rocksmith2014.Conversion.Utils

open System
open Rocksmith2014.SNG
open Rocksmith2014

/// Converts a floating point number (seconds) into an integer (milliseconds).
let secToMs (sec: float32) = Math.Round(float sec * 1000.0) |> int

/// Converts an integer (milliseconds) into a floating point number (seconds).
let msToSec (time: int) = float32 time / 1000.0f

/// Tests if an SNG note mask has the given flag.
let inline (?=) (mask:NoteMask) (flag:NoteMask) = (mask &&& flag) <> NoteMask.None

// Returns true if the SNG note is not a chord.
let isNote (n: Note) = n.ChordId = -1

/// Maps an array into a ResizeArray using the given map function.
let mapToResizeArray map (array : 'a array) =
    let ra = ResizeArray(array.Length)
    for i = 0 to array.Length - 1 do
        ra.Add(map array.[i])
    ra

/// Maps a ResizeArray into an array using the given map function.
let mapToArray map (resizeArray: ResizeArray<'a>) =
    Array.init resizeArray.Count (fun i -> map resizeArray.[i])

/// Maps a ResizeArray into an array using the given map function, with index.
let mapiToArray map (resizeArray: ResizeArray<'a>) =
    Array.init resizeArray.Count (fun i -> map i resizeArray.[i])

/// Returns the average of the given array, zero for an empty array.
let tryAverage = function
    | [||] -> 0.f
    | arr -> arr |> Array.average

/// Converts a boolean value into a signed byte.
let inline boolToByte b = if b then 1y else 0y

/// Finds the index of the phrase iteration that contains the given time code.
let findPiId inclusive (time: int) (iterations: ResizeArray<XML.PhraseIteration>) =
    let mutable id = iterations.Count - 1
    while id > 0 && not ((inclusive && iterations.[id].Time = time) || iterations.[id].Time < time) do
        id <- id - 1
    id

// Beats on the same time code as a phrase iteration belong to the previous phrase iteration
let findBeatPhraseIterationId time iterations = findPiId false time iterations

let findPhraseIterationId time iterations = findPiId true time iterations

let findSectionId (time: int) (sections: ResizeArray<XML.Section>) =
    let mutable id = sections.Count - 1
    while id > 0 && not (sections.[id].Time <= time) do
        id <- id - 1
    id

let findAnchor (time: int) (anchors: ResizeArray<XML.Anchor>) =
    let rec find index =
        if index < 0 then
            failwith "No anchor found for note."
        elif anchors.[index].Time <= time then
            anchors.[index]
        else
            find (index - 1)
    find (anchors.Count - 1)

let findFingerPrintId time (fingerPrints: FingerPrint array) =
    let mutable id = 0
    while id <> fingerPrints.Length && not (time >= fingerPrints.[id].StartTime && time < fingerPrints.[id].EndTime) do
        id <- id + 1
    if id = fingerPrints.Length then -1 else id

let findIndex startIndex time (noteTimes: int array) =
    let mutable index = startIndex
    while index <> noteTimes.Length && not (noteTimes.[index] >= time) do
        index <- index + 1
    if index = noteTimes.Length then -1 else index

/// Finds the indexes of the first and last notes in the given time range.
let findFirstAndLastTime (noteTimes: int array) startTime endTime =
    let firstIndex = findIndex 0 startTime noteTimes
    match firstIndex with
    | -1 -> None
    | index when noteTimes.[index] >= endTime -> None
    | firstIndex ->
        let lastIndex =
            let i = findIndex firstIndex endTime noteTimes
            if i = -1 then noteTimes.Length - 1 else i - 1
        Some (firstIndex, lastIndex)