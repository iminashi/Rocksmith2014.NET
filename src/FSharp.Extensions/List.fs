[<RequireQualifiedAccess>]
module List

/// Removes the item at the given index form the list.
let removeAt index (list: 'a list) =
    let rec remove current list =
        match list with
        | [] ->
            []
        | _ :: tail when current = index ->
            tail
        | h :: tail ->
            h :: (remove (current + 1) tail)

    remove 0 list

let insertAt index (item: 'a) (list: 'a list) =
    let rec insert current list =
        match list with
        | [] ->
            [ item ]
        | l when current = index ->
            item :: l
        | h :: tail ->
            h :: (insert (current + 1) tail)

    insert 0 list

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

/// Replaces the item at the given index with the new one.
let updateAt index newItem =
    List.mapi (fun i x -> if i = index then newItem else x)

/// Tries to find the lowest element in the list, returns None for an empty list.
let tryMin list =
    match list with
    | [] -> None
    | _ -> Some(List.min list)
