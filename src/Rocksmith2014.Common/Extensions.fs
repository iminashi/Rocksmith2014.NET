namespace Rocksmith2014.Common

open System
open System.Collections.Generic
open System.IO

[<AutoOpen>]
module GeneralHelpers =
    /// Determines whether the given value is not null.
    let inline notNull obj = obj |> isNull |> not

    /// Calls the impure function with the target value and returns it.
    let inline apply f target = f target; target

[<RequireQualifiedAccess>]
module File =
    /// Calls the map function if the file with the given path exists.
    let tryMap f path =
        if File.Exists path
        then Some (f path)
        else None

[<RequireQualifiedAccess>]
module Async =
    /// Maps the result of an asynchronous computation.
    let map f (task: Async<_>) = async {
        let! x = task
        return f x }

[<RequireQualifiedAccess>]
module ResizeArray =
    let init (size: int) f =
        let a = ResizeArray(size)
        for i = 0 to size - 1 do a.Add(f i)
        a

[<RequireQualifiedAccess>]
module Array =
    /// Returns a new array with the item at the given index replaced with the new one.
    let updateAt (index: int) newItem array =
        let arr = Array.copy array
        arr.[index] <- newItem
        arr

[<RequireQualifiedAccess>]
module Dictionary =
    /// Maps the result of IReadOnlyDictionary.TryGetValue into an option.
    let tryGetValue key (dict: IReadOnlyDictionary<_,_>) =
        match dict.TryGetValue key with
        | true, value -> Some value
        | false, _ -> None

[<RequireQualifiedAccess>]
module Option =
    /// Creates an option from a string, where a null or whitespace string equals None.
    let ofString s = if String.IsNullOrWhiteSpace s then None else Some s

    /// Creates an option from an array, where null or an empty array equals None.
    let ofArray a =
        match a with
        | null | [||] -> None
        | array -> Some array

[<RequireQualifiedAccess>]
module String =
    /// Returns true if the string is not null or whitespace.
    let notEmpty = (String.IsNullOrWhiteSpace >> not)

    /// Returns true if the string starts with the given value (case insensitive).
    let startsWith prefix (str: string) = notNull str && str.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)

    /// Returns true if the string ends with the given value (case insensitive).
    let endsWith suffix (str: string) = notNull str && str.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)

    /// Returns true if the string contains the given value (case sensitive).
    let contains (substr: string) (str: string) = str.Contains(substr, StringComparison.Ordinal)

    /// Returns the string if it is shorter than the max length, otherwise a substring of it.
    let truncate (maxLength: int) (str: string) =
        if str.Length > maxLength then
            str.Substring(0, maxLength)
        else
            str

[<RequireQualifiedAccess>]
module List =
    /// Removes the item at the given index form the list.
    let removeAt index (list: 'a list) =
        let rec remove current list =
            match list with
            | [] -> []
            | _::tail when current = index -> tail
            | h::tail -> h::(remove (current + 1) tail)

        remove 0 list

    let insertAt index (item: 'a) (list: 'a list) =
        let rec insert current list =
            match list with
            | [] -> [ item ]
            | l when current = index -> item::l
            | h::tail -> h::(insert (current + 1) tail)

        insert 0 list

    /// Removes the item from the list.
    let rec remove (item: 'a) (list: 'a list) =
        match list with
        | [] -> []
        | h::tail when h = item -> tail
        | h::tail -> h::(remove item tail)

    /// Switches the old item into the new one in the list.
    let rec update (oldItem: 'a) (newItem: 'a) (list: 'a list) =
        match list with
        | [] -> []
        | h::tail when h = oldItem -> newItem::tail
        | h::tail -> h::(update oldItem newItem tail)

    /// Replaces the item at the given index with the new one.
    let rec updateAt index newItem = List.mapi (fun i x -> if i = index then newItem else x)
