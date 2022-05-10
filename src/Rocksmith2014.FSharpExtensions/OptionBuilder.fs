[<AutoOpen>]
module OptionBuilder

type OptionBuilder() =
    member _.Bind(x, f) = Option.bind f x
    member _.Return(x) = Some x
    member _.ReturnFrom(x: 'a option) = x
    member _.Zero() = None

let option = OptionBuilder()
