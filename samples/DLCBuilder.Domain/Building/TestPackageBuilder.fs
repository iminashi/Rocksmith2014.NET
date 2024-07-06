module DLCBuilder.TestPackageBuilder

open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System.Diagnostics
open System
open System.IO
open System.Text.RegularExpressions

/// Creates a test build package name for the project.
let createPackageName project =
    project.DLCKey.ToLowerInvariant()

/// Generates new IDs for all the arrangements.
let generateAllIds (arrangements: Arrangement list) =
    // Sort the new IDs to keep multiple alternative arrangements for the same path in the same order
    let guidsArray =
        Array.init arrangements.Length (fun _ -> Guid.NewGuid())
        |> Array.sort

    arrangements
    |> List.mapi (fun i arr ->
        match arr with
        | Instrumental inst ->
            { inst with
                MasterId = RandomGenerator.next ()
                PersistentId = guidsArray[i] }
            |> Instrumental
        | Vocals vocals ->
            { vocals with
                MasterId = RandomGenerator.next ()
                PersistentId = guidsArray[i] }
            |> Vocals
        | Showlights _ as sl ->
            sl)

/// Returns a list of paths to the test builds for the project.
let getTestBuildFiles config project =
    let packageName = createPackageName project

    if packageName.Length >= DLCKey.MinimumLength && Directory.Exists(config.TestFolderPath) then
        Directory.EnumerateFiles(config.TestFolderPath)
        |> Seq.filter (Path.GetFileName >> (String.startsWith packageName))
        |> List.ofSeq
    else
        List.empty

let private getNewestVersionNumber existingPackages =
    existingPackages
    |> List.choose (fun fn ->
        let m = Regex.Match(fn, @"_v(\d+)")
        if m.Success then
            Some(int m.Groups[1].Captures[0].Value)
        else
            None)
    |> function
        | [] -> existingPackages.Length
        | list -> List.max list

let updateProject versionStringOpt project =
    ({ project with Version = "test" }, versionStringOpt)
    ||> Option.fold (fun project versionString ->
        let arrangements = generateAllIds project.Arrangements

        let titleValue = $"{project.Title.Value} %s{versionString}"
        let titleSortValue =
            project.Title.SortValue
            |> Option.ofString
            |> Option.map (fun sort -> sprintf "%s %s" sort versionString)
            |> Option.defaultWith (fun () ->
                StringValidator.convertToSortField (StringValidator.FieldType.Title titleValue))

        { project with
            DLCKey = $"{project.DLCKey}{versionString}"
            Title = SortableString.Create(titleValue, titleSortValue)
            JapaneseTitle =
                project.JapaneseTitle
                |> Option.map (fun title -> $"{title} {versionString}")
            Arrangements = arrangements }
    )

/// Returns an async computation for building a package for testing.
let build platform projectDir config project =
    async {
        let isRocksmithRunning =
            Process.GetProcessesByName("Rocksmith2014").Length > 0

        let packageFileName = createPackageName project

        if packageFileName.Length < DLCKey.MinimumLength then
            failwith "DLC key length too short."

        let targetFolder = config.TestFolderPath

        let existingPackages =
            Directory.EnumerateFiles(targetFolder)
            |> Seq.filter (Path.GetFileName >> (String.startsWith packageFileName))
            |> Seq.toList

        let latestVersion = getNewestVersionNumber existingPackages

        let project, packageFileName, buildType =
            match isRocksmithRunning with
            | false ->
                // Delete any previous versions
                List.iter File.Delete existingPackages

                updateProject None project, packageFileName, BuildCompleteType.Test
            | true ->
                let versionString = $"v{latestVersion + 1}"

                updateProject (Some versionString) project,
                $"{packageFileName}_{versionString}",
                BuildCompleteType.TestNewVersion versionString

        let path =
            Path.Combine(targetFolder, packageFileName)
            |> PackageBuilder.WithoutPlatformOrExtension

        let projectDir = projectDir |> Option.filter (fun _ -> config.ComparePhraseLevelsOnTestBuild)
        let buildConfig = BuildConfig.create Test config projectDir project [ platform ]

        let! _, toneKeysMap = PackageBuilder.buildPackages path buildConfig project
        return buildType, toneKeysMap
    }
