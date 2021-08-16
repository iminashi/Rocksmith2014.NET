[<RequireQualifiedAccess>]
module Array

open System

/// Returns a new array with the item at the given index replaced with the new one.
let updateAt (index: int) newItem array =
    let arr = Array.copy array
    arr.[index] <- newItem
    arr

/// Returns the average of the given array, zero for an empty array.
let tryAverage = function
    | [||] -> 0.f
    | arr -> Array.average arr

/// Returns true if all the elements in the array are the same or the array is empty.
let allSame (array: 'a array) =
    array.Length <= 1
    ||
    array.AsSpan(1).TrimStart(array.[0]).IsEmpty
