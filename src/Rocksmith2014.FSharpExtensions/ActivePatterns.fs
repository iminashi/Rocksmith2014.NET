[<AutoOpen>]
module ActivePatterns

open System

let (|Contains|_|) (substr: string) (str: string) =
    if String.containsIgnoreCase substr str then
        Some()
    else
        None

let (|StartsWith|_|) (prefix: string) (str: string) =
    if String.startsWith prefix str then
        Some()
    else
        None

let (|EndsWith|_|) (suffix: string) (str: string) =
    if String.endsWith suffix str then
        Some()
    else
        None

let (|HasExtension|) (path: string) =
    IO.Path.GetExtension(path).ToLowerInvariant()

[<return: Struct>]
let (|UInt64|_|) (str: string) =
    match UInt64.TryParse str with
    | true, v -> ValueSome v
    | false, _ -> ValueNone
