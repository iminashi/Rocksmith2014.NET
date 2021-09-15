[<AutoOpen>]
module ActivePatterns

open System

let (|Contains|_|) (substr: string) (str: string) =
    if String.containsIgnoreCase substr str then
        Some ()
    else
        None

let (|StartsWith|_|) (prefix: string) (str: string) =
    if String.startsWith prefix str then
        Some ()
    else
        None

let (|EndsWith|_|) (suffix: string) (str: string) =
    if String.endsWith suffix str then
        Some ()
    else
        None
