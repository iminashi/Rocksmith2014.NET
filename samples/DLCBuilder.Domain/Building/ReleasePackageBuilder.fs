module DLCBuilder.ReleasePackageBuilder

open System.IO
open Rocksmith2014.DLCProject
open Rocksmith2014.Common.Manifest
open Rocksmith2014.Common

/// Returns the target directory for the project.
let getTargetDirectory (projectPath: string option) (project: DLCProject) =
    projectPath
    |> Option.defaultValue (Arrangement.getFile project.Arrangements.Head)
    |> Path.GetDirectoryName

let private getArtistAndTitle project =
    let artist =
        project.ArtistName.SortValue
        |> Option.ofString
        |> Option.defaultWith (fun () ->
            StringValidator.convertToSortField (StringValidator.FieldType.ArtistName project.ArtistName.Value))

    let title =
        project.Title.SortValue
        |> Option.ofString
        |> Option.defaultWith (fun () ->
            StringValidator.convertToSortField (StringValidator.FieldType.Title project.Title.Value))

    {| Artist = artist; Title = title |}

/// Returns an async task for building packages for release.
let build (openProject: string option) (config: Configuration) (project: DLCProject) =
    async {
        let project = Utils.addDefaultTonesIfNeeded project
        let releaseDir = getTargetDirectory openProject project

        let fileNameWithoutExtension =
            let data = getArtistAndTitle project
            sprintf "%s_%s_v%s" data.Artist data.Title (project.Version.Replace('.', '_'))
            |> StringValidator.fileName

        let path =
            Path.Combine(releaseDir, fileNameWithoutExtension)
            |> PackageBuilder.WithoutPlatformOrExtension

        let buildConfig =
            BuildConfig.create Release config (Some releaseDir) project (Set.toList config.ReleasePlatforms)

        let! packagePaths, toneKeysMap = PackageBuilder.buildPackages path buildConfig project

        return BuildCompleteType.Release packagePaths, toneKeysMap
    }

let private addPitchPedal index shift gearList =
    let knobs =
        [ "Pedal_MultiPitch_Mix", 100f
          "Pedal_MultiPitch_Pitch1", float32 (-shift)
          "Pedal_MultiPitch_Tone", 50f ]
        |> Map.ofList

    let pitchPedal =
        { Type = "Pedals"
          KnobValues = knobs
          Key = "Pedal_MultiPitch"
          Category = None
          Skin = None
          SkinIndex = None }

    let prePedals =
        gearList.PrePedals
        |> Array.mapi (fun i p -> if i = index then Some pitchPedal else p)

    { gearList with PrePedals = prePedals }

let private pitchShiftTones (shift: int16) (tones: Tone list) =
    tones
    |> List.map (fun tone ->
        tone.GearList.PrePedals
        |> Array.tryFindIndex Option.isNone
        |> function
            | Some freeIndex ->
                { tone with GearList = addPitchPedal freeIndex shift tone.GearList }
            | None ->
                failwith $"Could not add pitch shift pedal to tone {tone.Key}.\nThere needs to be at least one free pre-pedal slot.")

let private processArrangements shift arrangements =
    arrangements
    |> List.map (function
        | Instrumental inst ->
            let tuning = inst.Tuning |> Array.map ((+) shift)
            Instrumental { inst with Tuning = tuning }
        | other ->
            other)
    |> TestPackageBuilder.generateAllIds

let buildPitchShifted (openProject: string option) (config: Configuration) (project: DLCProject) =
    async {
        let shift = project.PitchShift |> Option.defaultValue 0s
        let title = { project.Title with SortValue = $"{project.Title.SortValue} Pitch" }

        let pitchProject =
            { project with
                DLCKey = $"Pitch{project.DLCKey}"
                Title = title
                Arrangements = processArrangements shift project.Arrangements
                Tones = pitchShiftTones shift project.Tones }

        let! _, toneKeysMap = build openProject config pitchProject

        return BuildCompleteType.PitchShifted, toneKeysMap
    }

let buildReplacePsarc quickEditData config project =
    async {
        let platform = Platform.fromPackageFileName quickEditData.PsarcPath
        let buildConfig = BuildConfig.create (ReplacePsarc quickEditData) config None project [ platform ]

        let! _ = PackageBuilder.buildPackages (PackageBuilder.WithPlatformAndExtension quickEditData.PsarcPath) buildConfig project

        return BuildCompleteType.ReplacePsarc, Map.empty
    }

let executePostBuildTasks (config: Configuration) (currentPlatform: Platform) (project: DLCProject) (packagePaths: string array) =
    for copyTask in config.PostReleaseBuildTasks do
        let baseTargetPath = copyTask.TargetPath
        let targetDirectory =
            let data = getArtistAndTitle project

            match copyTask.CreateSubfolder with
            | DoNotCreate ->
                baseTargetPath
            | ArtistName ->
                let subfolder = StringValidator.fileName data.Artist
                Directory.CreateDirectory(Path.Combine(baseTargetPath, subfolder)).FullName
            | ArtistNameAndTitle ->
                let subfolder = StringValidator.fileName $"{data.Artist} - {data.Title}"
                Directory.CreateDirectory(Path.Combine(baseTargetPath, subfolder)).FullName
            | UseOnlyExistingSubfolder ->
                let artistTitleSubfolder = StringValidator.fileName $"{data.Artist} - {data.Title}"
                let artistTitlePath = Path.Combine(baseTargetPath, artistTitleSubfolder)

                if Directory.Exists(artistTitlePath) then
                    artistTitlePath
                else
                    let artistSubfolder = StringValidator.fileName data.Artist
                    let artistPath = Path.Combine(baseTargetPath, artistSubfolder)

                    if Directory.Exists(artistPath) then
                        artistTitlePath
                    else
                        baseTargetPath

        for path in packagePaths do
            let packagePlatform = Platform.fromPackageFileName path
            let shouldCopy = copyTask.AllPlatforms || packagePlatform = currentPlatform

            if shouldCopy then
                let targetPath = Path.Combine(targetDirectory, Path.GetFileName(path))
                File.Copy(path, targetPath, overwrite = true)
                if copyTask.OpenFolder then
                    Utils.openWithShell targetDirectory
