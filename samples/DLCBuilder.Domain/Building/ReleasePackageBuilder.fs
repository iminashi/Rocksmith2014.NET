module DLCBuilder.ReleasePackageBuilder

open System.IO
open Rocksmith2014.DLCProject
open Rocksmith2014.Common.Manifest
open Rocksmith2014.Common

/// Returns the target directory for the project.
let getTargetDirectory (projectPath: string option) project =
    projectPath
    |> Option.defaultValue (Arrangement.getFile project.Arrangements.Head)
    |> Path.GetDirectoryName

/// Returns an async task for building packages for release.
let build (openProject: string option) config project = async {
    let project = Utils.addDefaultTonesIfNeeded project
    let releaseDir = getTargetDirectory openProject project

    let fileNameWithoutExtension =
        sprintf "%s_%s_v%s" project.ArtistName.SortValue project.Title.SortValue (project.Version.Replace('.', '_'))
        |> StringValidator.fileName

    let path =
        Path.Combine(releaseDir, fileNameWithoutExtension)
        |> PackageBuilder.WithoutPlatformOrExtension

    let buildConfig =
        let baseConfig = BuildConfig.create Release config project (Set.toList config.ReleasePlatforms)

        { baseConfig with
            IdResetConfig =
                Some
                    { ProjectDirectory = releaseDir
                      ConfirmIdRegeneration = IdRegenerationHelper.getConfirmation
                      PostNewIds = IdRegenerationHelper.postNewIds } }

    do! PackageBuilder.buildPackages path buildConfig project

    return BuildCompleteType.Release }

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

let private pitchShiftTones shift (tones: Tone list) =
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

let buildPitchShifted (openProject: string option) config project = async {
    let shift = project.PitchShift |> Option.defaultValue 0s
    let title = { project.Title with SortValue = $"{project.Title.SortValue} Pitch" }

    let pitchProject =
        { project with
            DLCKey = $"Pitch{project.DLCKey}"
            Title = title
            Arrangements = processArrangements shift project.Arrangements
            Tones = pitchShiftTones shift project.Tones }

    let! _ = build openProject config pitchProject

    return BuildCompleteType.PitchShifted }

let buildReplacePsarc psarcPath config project =
    async {
        let platform = Platform.fromPackageFileName psarcPath
        let buildConfig = BuildConfig.create Release config project [ platform ]

        // TODO: Preserve App ID from imported PSARC
        do! PackageBuilder.buildPackages (PackageBuilder.WithPlatformAndExtension psarcPath) buildConfig project

        return BuildCompleteType.ReplacePsarc
    }
