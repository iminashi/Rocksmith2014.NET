module Rocksmith2014.Conversion.Utils

open System
open Rocksmith2014.SNG.Types
open Nessos.Streams

/// Converts a floating point number (seconds) into an integer (milliseconds).
let secToMs (sec:float32) = Math.Round(float sec * 1000.0) |> int

/// Converts an integer (milliseconds) into a floating point number (seconds).
let msToSec (time:int) = float32 time / 1000.0f

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

let tryAverage = function
    | [||] -> 0.f
    | arr -> arr |> Array.average