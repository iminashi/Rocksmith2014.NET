[<RequireQualifiedAccess>]
module Array

open System

/// Returns the average of the given array, zero for an empty array.
let tryAverage = function
    | [||] -> 0.f
    | arr -> Array.average arr

/// Returns true if all the elements in the array are the same or the array is empty.
let allSame (array: 'a array) =
    array.Length <= 1
    ||
    array.AsSpan(1).TrimStart(array.[0]).IsEmpty

/// Array.choose with the element index passed to the function.
let choosei f array =
    let mutable i = -1
    Array.choose (fun x -> i <- i + 1; f i x) array
