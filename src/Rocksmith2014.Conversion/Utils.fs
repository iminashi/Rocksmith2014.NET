module Rocksmith2014.Conversion.Utils

open System
open Rocksmith2014.SNG
open Nessos.Streams
open Rocksmith2014

/// Converts a floating point number (seconds) into an integer (milliseconds).
let secToMs (sec: float32) = Math.Round(float sec * 1000.0) |> int

/// Converts an integer (milliseconds) into a floating point number (seconds).
let msToSec (time: int) = float32 time / 1000.0f

/// Tests if an SNG note mask has the given flag.
let inline (?=) (mask:NoteMask) (flag:NoteMask) = (mask &&& flag) <> NoteMask.None

/// Maps an array into a ResizeArray using the given map function.
let mapToResizeArray map array =
    array
    |> Stream.ofArray
    |> Stream.map map
    |> Stream.toResizeArray

/// Maps a ResizeArray into an array using the given map function.
let mapToArray map resizeArray =
    resizeArray
    |> Stream.ofResizeArray
    |> Stream.map map
    |> Stream.toArray

/// Maps a ResizeArray into an array using the given map function, with index.
let mapiToArray map resizeArray =
    resizeArray
    |> Stream.ofResizeArray
    |> Stream.mapi map
    |> Stream.toArray

/// Returns the average of the given array, zero for an empty array.
let tryAverage = function
    | [||] -> 0.f
    | arr -> arr |> Array.average

/// Converts a boolean value into a signed byte.
let inline boolToByte b = if b then 1y else 0y

/// Finds the index of the phrase iteration that contains the given time code.
let findPiId inclusive (time: int) (iterations: ResizeArray<XML.PhraseIteration>) =
    let rec find index =
        if index <= 0 then
            0
        elif (inclusive && iterations.[index].Time = time) || iterations.[index].Time < time then
            index
        else
            find (index - 1)
    find (iterations.Count - 1)

// Beats on the same time code as a phrase iteration belong to the previous phrase iteration
let findBeatPhraseIterationId time iterations = findPiId false time iterations

let findPhraseIterationId time iterations = findPiId true time iterations

let findSectionId (time: int) (sections: ResizeArray<XML.Section>) =
    let rec find index =
        if index <= 0 then
            0
        elif sections.[index].Time <= time then
            index
        else
            find (index - 1)
    find (sections.Count - 1)

let findAnchor (time: int) (anchors: ResizeArray<XML.Anchor>) =
    let rec find index =
        if index < 0 then
            failwith "No anchor found for note."
        elif anchors.[index].Time <= time then
            anchors.[index]
        else
            find (index - 1)
    find (anchors.Count - 1)
