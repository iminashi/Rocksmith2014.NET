[<AutoOpen>]
module GeneralHelpers

/// Determines whether the given value is not null.
let inline notNull obj =
    obj |> isNull |> not

/// Calls the impure function with the target value and returns it.
let inline apply f target =
    f target
    target
