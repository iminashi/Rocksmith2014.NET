namespace Rocksmith2014.Common

type Platform = PC | Mac

module Platform =
    let private PathsPC = [| "windows"; "generic"; "_p" |]
    let private PathsMac = [| "mac"; "macos"; "_m" |]

    let getPath platform index =
        match platform with
        | PC -> PathsPC.[index]
        | Mac -> PathsMac.[index]