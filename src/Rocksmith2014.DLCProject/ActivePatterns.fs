[<AutoOpen>]
module Rocksmith2014.DLCProject.ActivePatterns

let (|Contains|_|) (substr: string) (str: string) =
    if str.Contains substr then
        Some ()
    else
        None

let (|EndsWith|_|) (substr: string) (str: string) =
    if str.EndsWith substr then
        Some ()
    else
        None