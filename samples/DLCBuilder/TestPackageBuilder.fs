module DLCBuilder.TestPackageBuilder

open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions

/// Creates a test build package name for the project.
let createPackageName project =
    project.DLCKey.ToLowerInvariant()

let private generateNewIds arrangements =
    arrangements
    |> List.map (function
    | Instrumental inst ->
        { inst with MasterID = RandomGenerator.next()
                    PersistentID = Guid.NewGuid() }
        |> Instrumental
    | Vocals vocals ->
        { vocals with MasterID = RandomGenerator.next()
                      PersistentID = Guid.NewGuid() }
        |> Vocals
    | other ->
        other)

/// Returns an async task for building the test package.
let build platform config project =
    let isRocksmithRunning =
        Process.GetProcesses()
        |> Array.exists (fun x -> x.ProcessName = "Rocksmith2014")

    let packageFileName = createPackageName project
    let targetFolder = config.TestFolderPath
    let existingPackages =
        Directory.EnumerateFiles targetFolder
        |> Seq.filter (Path.GetFileName >> (String.startsWith packageFileName))
        |> Seq.toList

    let maxVersion =
        existingPackages
        |> List.choose (fun fn ->
            let m = Regex.Match(fn, @"_v(\d+)")
            if m.Success then
                Some (int m.Groups.[1].Captures.[0].Value)
            else
                None)
        |> function
        | [] -> existingPackages.Length
        | list -> List.max list

    let project, packageFileName =
        match isRocksmithRunning with
        | false ->
            // Delete any previous versions
            List.iter File.Delete existingPackages

            project, packageFileName
        | true ->
            let arrangements = generateNewIds project.Arrangements
            let versionString = $"v{maxVersion + 1}"
            let title =
                { project.Title with Value = $"{project.Title.Value} {versionString}"
                                     SortValue = $"{project.Title.SortValue} {versionString}" }

            { project with DLCKey = $"{project.DLCKey}{versionString}"
                           Title = title
                           Arrangements = arrangements },
            packageFileName + $"_{versionString}"

    let path = Path.Combine(targetFolder, packageFileName)
    let buildConfig = Utils.createBuildConfig Test config project [ platform ]

    PackageBuilder.buildPackages path buildConfig project
