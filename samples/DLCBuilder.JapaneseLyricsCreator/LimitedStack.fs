namespace JapaneseLyricsCreator

/// Stack with a fixed size.
/// When a new item is pushed when the stack is full, the oldest item gets replaced.
type LimitedStack<'a>(size: int) =
    let mutable start = 0
    let items: 'a voption array = Array.replicate size ValueNone

    let wrap i =
        if i < 0 then
            items.Length - 1
        elif i >= items.Length then
            0
        else
            i

    member _.HasItems with get() = items[start].IsSome

    member _.Push(item) =
        if items[start].IsSome then
            start <- wrap (start + 1)

        items[start] <- ValueSome item

    member _.Pop() =
        let item =
            items[start]
            |> ValueOption.defaultWith (fun () -> failwith "Stack has no items.")

        items[start] <- ValueNone
        start <- wrap (start - 1)

        item

    override _.ToString() = sprintf "%A" items
