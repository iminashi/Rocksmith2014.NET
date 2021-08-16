module DLCBuilder.TestPackageBuilder

open Rocksmith2014.DLCProject
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions

/// Creates a test build package name for the project.
let createPackageName project =
    project.DLCKey.ToLowerInvariant()

/// Generates new IDs for all the arrangements.
let generateAllIds arrangements = List.map Arrangement.generateIds arrangements

/// Returns a list of paths to the test builds for the project.
let getTestBuildFiles config project =
    let packageName = createPackageName project

    if packageName.Length >= DLCKey.MinimumLength && Directory.Exists config.TestFolderPath then
        Directory.EnumerateFiles config.TestFolderPath
        |> Seq.filter (Path.GetFileName >> (String.startsWith packageName))
        |> List.ofSeq
    else
        List.empty

/// Returns an async computation for building a package for testing.
let build platform config project = async {
    let isRocksmithRunning =
        Process.GetProcessesByName "Rocksmith2014"
        |> (Array.isEmpty >> not)

    let packageFileName = createPackageName project
    if packageFileName.Length < DLCKey.MinimumLength then failwith "DLC key length too short."
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

    let project, packageFileName, buildType =
        match isRocksmithRunning with
        | false ->
            // Delete any previous versions
            List.iter File.Delete existingPackages

            project, packageFileName, BuildCompleteType.Test
        | true ->
            let arrangements = generateAllIds project.Arrangements
            let versionString = $"v{maxVersion + 1}"
            let title =
                { project.Title with Value = $"{project.Title.Value} {versionString}"
                                     SortValue = $"{project.Title.SortValue} {versionString}" }

            { project with DLCKey = $"{project.DLCKey}{versionString}"
                           Title = title
                           JapaneseTitle = project.JapaneseTitle |> Option.map (fun title -> $"{title} {versionString}")
                           Arrangements = arrangements },
            $"{packageFileName}_{versionString}",
            BuildCompleteType.TestNewVersion versionString

    let path = Path.Combine(targetFolder, packageFileName)
    let buildConfig = BuildConfig.create Test config project [ platform ]

    do! PackageBuilder.buildPackages path buildConfig project
    return buildType }
