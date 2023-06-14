[<RequireQualifiedAccess>]
module ResizeArray

open System

/// Initializes a new ResizeArray with the given initial capacity.
let init (size: int) f =
    let a = ResizeArray(size)
    for i = 0 to size - 1 do a.Add(f i)
    a

/// Returns the element if the index is within the ResizeArray.
let inline tryItem (index: int) (a: ResizeArray<_>) =
    if index >= 0 && index < a.Count then
        Some a[index]
    else
        None

/// Returns the last element in the ResizeArray or None if it is empty.
let inline tryLast (a: ResizeArray<_>) =
    if a.Count = 0 then
        None
    else
        Some a[a.Count - 1]

/// Returns the first element in the ResizeArray or None if it is empty.
let inline tryHead (a: ResizeArray<_>) =
    if a.Count = 0 then None else Some a[0]

/// Returns the first element that matches the predicate, or None if no match is found.
let tryFind (predicate: 'a -> bool) (a: ResizeArray<'a>) =
    let rec seek index =
        if index = a.Count then
            None
        else
            if predicate a[index] then
                Some a[index]
            else
                seek (index + 1)
    seek 0

/// Returns the first element for which the function returns Some(x).
let tryPick (chooser: 'a -> 'b option) (a: ResizeArray<'a>) =
    let rec seek index =
        if index = a.Count then
            None
        else
            match chooser a[index] with
            | None -> seek (index + 1)
            | x -> x
    seek 0

/// Returns a new ResizeArray containing only the elements for which the predicate returns true.
let inline filter (predicate: 'a -> bool) (a: ResizeArray<'a>) : ResizeArray<'a> =
    a.FindAll(Predicate<_>(predicate))

/// Executes the action for each of the elements in the ResizeArray.
let inline iter ([<InlineIfLambda>] action: 'a -> unit) (a: ResizeArray<'a>) =
    for i = 0 to a.Count - 1 do
        action a[i]

/// Returns a ResizeArray that contains the single item.
let singleton (value: 'a) =
    let a = ResizeArray<'a>(1)
    a.Add(value)
    a

/// Returns the largest value found using the projection.
let findMaxBy (projection: 'a -> 'b) (a: ResizeArray<'a>) =
    let mutable max = projection a[0]
    for i = 1 to a.Count - 1 do
        let res = projection a[i]
        if res > max then max <- res
    max
