namespace Rocksmith2014.Common

type Platform = PC | Mac

[<RequireQualifiedAccess>]
module Platform =
    type Path =
    | Audio = 0
    | SNG = 1
    | PackageSuffix = 2

    let private PathsPC = [| "windows"; "generic"; "_p" |]
    let private PathsMac = [| "mac"; "macos"; "_m" |]

    let getPath platform (path: Path) =
        let index = LanguagePrimitives.EnumToValue path
        match platform with
        | PC -> PathsPC.[index]
        | Mac -> PathsMac.[index]
