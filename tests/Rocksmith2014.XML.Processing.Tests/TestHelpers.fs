[<AutoOpen>]
module TestHelpers

/// Shorthand for creating a ResizeArray from a list.
let (!) (list: 'a list) = ResizeArray list
