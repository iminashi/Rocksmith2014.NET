module DLCBuilder.ReleasePackageBuilder

open System.IO
open Rocksmith2014.DLCProject

/// Returns an async task for building packages for release.
let build (openProject: string option) config project =
    let project = Utils.addDefaultTonesIfNeeded project
    let releaseDir =
        openProject
        |> Option.map Path.GetDirectoryName
        |> Option.defaultWith (fun _ -> Path.GetDirectoryName project.AudioFile.Path)

    let fileName =
        sprintf "%s_%s_v%s" project.ArtistName.SortValue project.Title.SortValue (project.Version.Replace('.', '_'))
        |> StringValidator.fileName

    let path = Path.Combine(releaseDir, fileName)
    let buildConfig = Utils.createBuildConfig Release config project (Set.toList config.ReleasePlatforms)
    PackageBuilder.buildPackages path buildConfig project
