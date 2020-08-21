namespace DLCBuilder

open System

module Option =
    let ofString s = if String.IsNullOrWhiteSpace s then None else Some s

module String =
    let notEmpty = (String.IsNullOrWhiteSpace >> not)

module List =
    let removeAt index (list: 'a list) =
        let rec remove current list =
            match list with
            | [] -> []
            | _::tail when current = index -> tail
            | h::tail -> h::(remove (current + 1) tail)
    
        remove 0 list

    let rec remove (item: 'a) (list: 'a list) =
        match list with
        | [] -> []
        | h::tail when h = item -> tail
        | h::tail -> h::(remove item tail)

    let rec update (oldItem: 'a) (newItem: 'a) (list: 'a list) =
        match list with
        | [] -> []
        | h::tail when h = oldItem -> newItem::tail
        | h::tail -> h::(update oldItem newItem tail)

[<AutoOpen>]
module General =
    let dispose<'a when 'a :> IDisposable> (x: 'a) = x.Dispose() 
