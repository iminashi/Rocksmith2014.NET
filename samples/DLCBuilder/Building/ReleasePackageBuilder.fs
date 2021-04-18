module DLCBuilder.ReleasePackageBuilder

open System.IO
open Rocksmith2014.DLCProject
open Rocksmith2014.Common.Manifest

/// Returns the target directory for the project.
let getTargetDirectory (projectPath: string option) project =
    projectPath
    |> Option.defaultValue (Arrangement.getFile project.Arrangements.Head)
    |> Path.GetDirectoryName

/// Returns an async task for building packages for release.
let build (openProject: string option) config project =
    let project = Utils.addDefaultTonesIfNeeded project
    let releaseDir = getTargetDirectory openProject project

    let fileName =
        sprintf "%s_%s_v%s" project.ArtistName.SortValue project.Title.SortValue (project.Version.Replace('.', '_'))
        |> StringValidator.fileName

    let path = Path.Combine(releaseDir, fileName)
    let buildConfig = Utils.createBuildConfig Release config project (Set.toList config.ReleasePlatforms)
    PackageBuilder.buildPackages path buildConfig project

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

let buildPitchShifted (openProject: string option) config project =
    let shift = project.PitchShift |> Option.defaultValue 0s
    let title = { project.Title with SortValue = $"{project.Title.SortValue} Pitch" }
    let dlcKey = $"Pitch{project.DLCKey}"
    let arrangements =
        project.Arrangements
        |> List.map (function
            | Instrumental inst ->
                let tuning = inst.Tuning |> Array.map ((+) shift)
                Instrumental { inst with Tuning = tuning }
            | other ->
                other)
        |> TestPackageBuilder.generateAllIds

    let tones =
        project.Tones
        |> List.map (fun tone ->
            let freeIndex =
                tone.GearList.PrePedals
                |> Array.tryFindIndex Option.isNone
            match freeIndex with
            | Some index ->
                { tone with GearList = addPitchPedal index shift tone.GearList }
            | None ->
                failwith $"Could not add pitch shift pre-pedal to tone {tone.Key}")

    let pitchProject =
        { project with DLCKey = dlcKey
                       Title = title
                       Arrangements = arrangements
                       Tones = tones }

    build openProject config pitchProject
