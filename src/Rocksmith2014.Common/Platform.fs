namespace Rocksmith2014.Common

type Platform = PC | Mac

module Platform =
    [<RequireQualifiedAccess>]
    type Path =
        | Audio
        | SNG
        | PackageSuffix

    /// Returns a platform-specific string for the given path type.
    let getPathPart (platform: Platform) (path: Path) =
        match platform, path with
        | PC, Path.Audio -> "windows"
        | PC, Path.SNG   -> "generic"
        | PC, Path.PackageSuffix -> "_p"
        | Mac, Path.Audio -> "mac"
        | Mac, Path.SNG   -> "macos"
        | Mac, Path.PackageSuffix -> "_m"

    /// Attempts to detect the platform from a package filename.
    let fromPackageFileName (filePath: string) =
        match System.IO.Path.GetFileNameWithoutExtension(filePath) with
        | EndsWith "_m" -> Mac
        | _ -> PC
