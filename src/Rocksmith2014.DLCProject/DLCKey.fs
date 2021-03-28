namespace Rocksmith2014.DLCProject

open System
open Rocksmith2014.Common

module DLCKey =
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
        if key.Length < 5 then
            key + randomChars (5 - key.Length)
        else
            key
