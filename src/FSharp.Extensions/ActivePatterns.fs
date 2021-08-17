[<AutoOpen>]
module ActivePatterns

open System

let (|Contains|_|) (substr: string) (str: string) =
    if str.Contains(substr, StringComparison.InvariantCultureIgnoreCase) then
        Some ()
    else
        None

let (|EndsWith|_|) (suffix: string) (str: string) =
    if String.endsWith suffix str then
        Some ()
    else
        None
