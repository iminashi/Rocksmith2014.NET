module Rocksmith2014.DLCProject.DLCKey

open System
open Rocksmith2014.Common

let [<Literal>] MinimumLength = 5

let private createPart str =
    let p = StringValidator.dlcKey str
    p.Substring(0, min 5 p.Length)

let private randomChars count =
    Array.init count (fun _ -> RandomGenerator.nextAlphabet())
    |> String

let private createPrefix charterName =
    let name = StringValidator.dlcKey charterName
    if name.Length < 2 then
        randomChars 2
    else
        name.Substring(0, 2)

/// Creates a DLC key from the charter name, artist name and title.
let create (charterName: string) (artist: string) (title: string) =
    let key = $"{createPrefix charterName}{createPart artist}{createPart title}"
    if key.Length < MinimumLength then
        key + randomChars (MinimumLength - key.Length)
    else
        key
