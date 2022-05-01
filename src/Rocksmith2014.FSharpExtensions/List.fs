[<RequireQualifiedAccess>]
module List

/// Removes the item from the list.
let rec remove (item: 'a) (list: 'a list) =
    match list with
    | [] ->
        []
    | h :: tail when h = item ->
        tail
    | h :: tail ->
        h :: (remove item tail)

/// Switches the old item into the new one in the list.
let rec update (oldItem: 'a) (newItem: 'a) (list: 'a list) =
    match list with
    | [] ->
        []
    | h :: tail when h = oldItem ->
        newItem :: tail
    | h :: tail ->
        h :: (update oldItem newItem tail)

/// Tries to find the lowest element in the list, returns None for an empty list.
let tryMin list =
    match list with
    | [] -> None
    | _ -> Some(List.min list)

/// Adds a new item to the start of the list.
let add newHead list = newHead :: list
