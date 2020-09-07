namespace Rocksmith2014.Common

type Platform = PC | Mac

[<RequireQualifiedAccess>]
module Platform =
    type Path =
    | Audio = 0
    | SNG = 1
    | PackageSuffix = 2

    let private pathsPC = [| "windows"; "generic"; "_p" |]
    let private pathsMac = [| "mac"; "macos"; "_m" |]

    /// Returns a platform-specific string for the given path.
    let getPath platform (path: Path) =
        let index = LanguagePrimitives.EnumToValue path
        match platform with
        | PC -> pathsPC.[index]
        | Mac -> pathsMac.[index]
