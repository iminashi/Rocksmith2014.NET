namespace DLCBuilder

open System

module Option =
    let ofString s = if String.IsNullOrWhiteSpace s then None else Some s

module List =
    let removeAt index (list : 'a list) =
        let rec remove current list =
            match list with
            | [] -> []
            | _::tail when current = index -> tail
            | h::tail -> h::(remove (current + 1) tail)
    
        remove 0 list

    let rec remove item (list : 'a list) =
        match list with
        | [] -> []
        | h::tail when Object.ReferenceEquals(h, item) -> tail
        | h::tail -> h::(remove item tail)

[<AutoOpen>]
module General =
    let dispose<'a when 'a :> IDisposable> (x: 'a) = x.Dispose() 
