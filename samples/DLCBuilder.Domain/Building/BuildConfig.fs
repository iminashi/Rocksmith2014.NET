module DLCBuilder.BuildConfig

open System
open Rocksmith2014.Audio
open Rocksmith2014.DD
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.PackageBuilder

/// Creates a build configuration data structure.
let create buildType config project platforms =
    let convTask =
        let tasks =
            DLCProject.getFilesThatNeedConverting project
            |> Seq.map (Wwise.convertToWem config.WwiseConsolePath)

        Async.Parallel(tasks, maxDegreeOfParallelism = 2)
        |> Async.Ignore

    let phraseSearch =
        match config.DDPhraseSearchEnabled with
        | true -> WithThreshold config.DDPhraseSearchThreshold
        | false -> SearchDisabled

    let appId =
        match buildType, config.CustomAppId with
        | Test, Some customId ->
            customId
        | ReplacePsarc { AppId = Some appId }, _ ->
            appId
        | _ ->
            AppId.CherubRock

    let disableDDGeneration =
        buildType = Test && not config.GenerateDD

    { Platforms = platforms
      BuilderVersion = $"DLC Builder {AppVersion.versionString}"
      Author = project.Author |> Option.defaultValue config.CharterName
      AppId = appId
      GenerateDD = not disableDDGeneration
      DDConfig =
        { PhraseSearch = phraseSearch
          LevelCountGeneration = config.DDLevelCountGeneration }
      ApplyImprovements = config.ApplyImprovements
      SaveDebugFiles = config.SaveDebugFiles && buildType = Test
      AudioConversionTask = convTask
      IdResetConfig = None
      ProgressReporter = Some(ProgressReporters.PackageBuild :> IProgress<float>) }
