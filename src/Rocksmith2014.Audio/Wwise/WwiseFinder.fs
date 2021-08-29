module Rocksmith2014.Audio.WwiseFinder

open System
open System.IO
open System.Text.RegularExpressions

let private tryFindWwiseInstallation rootDir =
    let audiokineticDirectory = Path.Combine(rootDir, "Audiokinetic")

    if Directory.Exists audiokineticDirectory then
        audiokineticDirectory
        |> Directory.EnumerateDirectories
        |> Seq.tryFind (fun fn -> Regex.IsMatch(fn, "20(19|21)"))
    else
        None

let findWindows () =
    let wwiseRoot =
        match Environment.GetEnvironmentVariable "WWISEROOT" |> Option.ofString with
        | Some (Contains "2019" | Contains "2021" as path) when Directory.Exists path ->
            path
        | _ ->
            // Try the default installation directory in program files
            tryFindWwiseInstallation (Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86))
            |> Option.orElseWith (fun () -> tryFindWwiseInstallation (Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)))
            |> Option.defaultWith (fun () -> failwith "Could not locate Wwise 2019 or 2021 installation from WWISEROOT environment variable or path Program Files\Audiokinetic.")

    Path.Combine(wwiseRoot, "Authoring", "x64", "Release", "bin", "WwiseConsole.exe")

let findMac () =
    let wwiseAppPath =
        tryFindWwiseInstallation "/Applications"
        |> Option.defaultWith (fun () -> failwith "Could not find Wwise 2019 or 2021 installation in /Applications/Audiokinetic/")

    Path.Combine(wwiseAppPath, "Wwise.app", "Contents", "Tools", "WwiseConsole.sh")

let findLinux () =
    let defaultWineProgramFiles =
        let homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        Path.Combine(homeDir, ".wine", "drive_c", "Program Files (x86)")

    let wwiseRoot =
        tryFindWwiseInstallation defaultWineProgramFiles
        |> Option.defaultWith (fun () -> failwith "Could not find Wwise 2019 or 2021 installation in ~/.wine/drive_c/Program Files (x86)/Audiokinetic/")

    Path.Combine(wwiseRoot, "Authoring", "x64", "Release", "bin", "WwiseConsole.exe")
