[<AutoOpen>]
module FSharpExtensions

open System
open System.Collections.Generic
open System.IO

[<RequireQualifiedAccess>]
module Async =
    /// Maps the result of an asynchronous computation.
    let map f (task: Async<_>) =
        async {
            let! x = task
            return f x
        }

[<RequireQualifiedAccess>]
module File =
    /// Calls the map function if the file with the given path exists.
    let tryMap f path =
        if File.Exists(path) then
            Some(f path)
        else
            None

[<RequireQualifiedAccess>]
module Dictionary =
    /// Maps the result of IReadOnlyDictionary.TryGetValue into an option.
    let tryGetValue key (dict: IReadOnlyDictionary<_, _>) =
        match dict.TryGetValue(key) with
        | true, value ->
            Some value
        | false, _ ->
            None

[<RequireQualifiedAccess>]
module Option =
    /// Creates an option from a string, where a null or whitespace string equals None.
    let inline ofString s =
        if String.IsNullOrWhiteSpace(s) then
            None
        else
            Some s

    /// Creates an option from an array, where null or an empty array equals None.
    let ofArray array =
        match array with
        | null | [||] ->
            None
        | array ->
            Some array
