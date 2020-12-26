[<AutoOpen>]
module Rocksmith2014.DLCProject.ActivePatterns

open System

let (|Contains|_|) (substr: string) (str: string) =
    if str.Contains(substr, StringComparison.InvariantCulture) then
        Some ()
    else
        None

let (|EndsWith|_|) (substr: string) (str: string) =
    if str.EndsWith(substr, StringComparison.InvariantCulture) then
        Some ()
    else
        None
