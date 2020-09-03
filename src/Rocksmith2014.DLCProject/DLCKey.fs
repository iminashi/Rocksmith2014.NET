namespace Rocksmith2014.DLCProject

open System
open Rocksmith2014.Common

module DLCKey =
    let create (charterName: string) (artist: string) (title: string) =
        let prefix =
            let name = StringValidator.dlcKey charterName
            if String.IsNullOrWhiteSpace name || name.Length < 2 then
                String([| RandomGenerator.nextAlphabet(); RandomGenerator.nextAlphabet() |])
            else
                name.Substring(0, 2)
        let validArtist = StringValidator.dlcKey artist
        let validTitle = StringValidator.dlcKey title

        prefix
        + validArtist.Substring(0, Math.Min(5, validArtist.Length))
        + validTitle.Substring(0, Math.Min(5, validTitle.Length))
