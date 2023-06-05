module DLCBuilder.Tools

open Elmish
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.PSARC
open Rocksmith2014.XML
open System
open System.IO
open System.Threading
open Newtonsoft.Json.Linq

module ToneInjector =
    open Newtonsoft.Json
    open Rocksmith2014.Common.Manifest

    let backupProfile profilePath =
        File.Copy(profilePath, $"%s{profilePath}.backup", overwrite = true)

    /// Converts an array of tones into a JArray.
    let private createToneJArray (tones: Tone array) =
        let dtos =
            tones
            |> Array.truncate 50
            |> Array.mapi (fun i tone ->
                { Tone.toDto tone with
                    SortOrder = Nullable(float32 (i + 1))
                    IsCustom = Nullable(true) })

        JArray.FromObject(dtos, JsonSerializer(NullValueHandling = NullValueHandling.Ignore))

    let private createToneImportTasks toneFiles =
        toneFiles
        |> Array.map (fun path ->
            async {
                match path with
                | EndsWith "xml" ->
                    return Tone.fromXmlFile path |> Array.singleton
                | EndsWith "json" ->
                    return! Tone.fromJsonFile path |> Async.AwaitTask |> Async.map Array.singleton
                | EndsWith "psarc" ->
                    return! Utils.importTonesFromPSARC path
                | _ ->
                    return Array.empty
            })

    /// Replaces the custom tones in the profile file with tones read from the files.
    let injectTones profilePath toneFiles =
        backgroundTask {
            let! tones =
                Async.Parallel(createToneImportTasks toneFiles, Environment.ProcessorCount)
                |> Async.map Array.concat

            let! profile, id = Profile.readAsJToken profilePath

            profile.Item("CustomTones").Replace(createToneJArray tones)

            backupProfile profilePath

            do! Profile.saveJToken profilePath id profile
        }

module ProfileCleanerTool =
    let readIdData maxDegreeOfParallelism dlcDirectory =
        async {
            let progressReporter =
                let mutable processedFiles = 0
                let reportFrequence = max 4 maxDegreeOfParallelism

                fun (p: ProfileCleaner.IdReadingProgress) ->
                    let num = Interlocked.Increment(&processedFiles)
                    // Do not trigger the event every time to keep the UI responsive
                    if num % reportFrequence = 0 then
                        (ProgressReporters.ProfileCleaner :> IProgress<float>).Report(100.0 * float processedFiles / float p.TotalFiles)

            return! ProfileCleaner.gatherIdAndKeyData progressReporter maxDegreeOfParallelism dlcDirectory
        }

    let cleanProfile isDryRun data profilePath =
        async {
            let filterIds, filterKeys = ProfileCleaner.getFilteringFunctions data

            if not isDryRun then
                ProfileCleaner.backupProfile profilePath

            let! profile, profileId = Profile.readAsJToken profilePath |> Async.AwaitTask

            let pnRecs = filterIds profile["Playnexts"].["Songs"]
            let songRecs = filterIds profile["Songs"]
            let saRecs = filterIds profile["SongsSA"]
            let statsRecs = filterIds profile["Stats"].["Songs"]

            profile["SongListsRoot"]["SongLists"] :?> JArray
            |> Seq.iter (fun songList -> songList :?> JArray |> filterKeys)

            profile["FavoritesListRoot"]["FavoritesList"] :?> JArray
            |> filterKeys

            if not isDryRun then
                do! Profile.saveJToken profilePath profileId profile |> Async.AwaitTask

            let result =
                { PlayNext = pnRecs
                  Songs = songRecs
                  ScoreAttack = saRecs
                  Stats = statsRecs }

            return result
        }

let update msg state =
    match msg with
    | ConvertWemToOgg files ->
        let task () =
            async {
                Array.iter Conversion.wemToOgg files
            }

        StateUtils.addTask WemToOggConversion state,
        Cmd.OfAsync.either task () (fun () -> WemToOggConversionCompleted) (fun ex -> TaskFailed(ex, WemToOggConversion))

    | ConvertAudioToWem files ->
        let task () =
            async {
                let tasks =
                    files
                    |> Array.map (Wwise.convertToWem state.Config.WwiseConsolePath)

                do! Async.Parallel(tasks, min 4 Environment.ProcessorCount) |> Async.Ignore

                return files
            }

        let longTask = WemConversion files

        StateUtils.addTask longTask state,
        Cmd.OfAsync.either task () WemConversionComplete (fun ex -> TaskFailed(ex, longTask))

    | UnpackPSARC (paths, targetRootDirectory) ->
        let totalFiles = float paths.Length

        let progress currentIndex currentProgress =
            let totalProgress =
                let cumulativeProgress = (float currentIndex / totalFiles) * 100.
                cumulativeProgress + (1. / totalFiles) * currentProgress
            (ProgressReporters.PsarcUnpack :> IProgress<_>).Report(totalProgress)

        let task () =
            paths
            |> Array.mapi (fun i path ->
                async {
                    let targetDirectory =
                        Path.Combine(targetRootDirectory, Path.GetFileNameWithoutExtension(path))

                    Directory.CreateDirectory(targetDirectory) |> ignore

                    use psarc = PSARC.OpenFile(path)
                    do! psarc.ExtractFiles(targetDirectory, progress i) |> Async.AwaitTask
                })
            |> Async.Sequential

        StateUtils.addTask PsarcUnpack state,
        Cmd.OfAsync.either task () (fun _ -> PsarcUnpacked) (fun ex -> TaskFailed(ex, PsarcUnpack))

    | PackDirectoryIntoPSARC (directory, targetFile) ->
        let task () = PSARC.PackDirectory(directory, targetFile, true) |> Async.AwaitTask

        state, Cmd.OfAsync.attempt task () ErrorOccurred

    | RemoveDD files ->
        let task () =
            let computations =
                files
                |> Array.map (fun file ->
                    async {
                        let arrangement = InstrumentalArrangement.Load(file)
                        do! arrangement.RemoveDD(matchPhrasesToSections = false) |> Async.AwaitTask
                        arrangement.Save(file)
                    })

            Async.Parallel(computations, max 1 (Environment.ProcessorCount / 4))

        state, Cmd.OfAsync.attempt task () ErrorOccurred

    | InjectTonesIntoProfile files ->
        let cmd =
            if String.notEmpty state.Config.ProfilePath then
                Cmd.OfTask.attempt (ToneInjector.injectTones state.Config.ProfilePath) files ErrorOccurred
            else
                Cmd.none

        state, cmd

    | StartProfileCleaner ->
        let pc = state.ProfileCleanerState
        match pc.CurrentStep with
        | ProfileCleanerStep.Idle
        | ProfileCleanerStep.Completed _ when File.Exists(state.Config.ProfilePath) && Directory.Exists(state.Config.DlcFolderPath) ->
            let task config =
                ProfileCleanerTool.readIdData config.ProfileCleanerIdParsingParallelism config.DlcFolderPath

            { state with
                ProfileCleanerState =
                    { pc with CurrentStep = ProfileCleanerStep.ReadingIds 0.0 }
            },
            Cmd.OfAsync.either task state.Config (IdDataReadingCompleted >> ToolsMsg) ErrorOccurred
        | _ ->
            state, Cmd.none

    | ProfileCleanerProgressChanged progress ->
        let pc = state.ProfileCleanerState
        match pc.CurrentStep with
        | ProfileCleanerStep.ReadingIds _ ->
            let newState = { pc with CurrentStep = ProfileCleanerStep.ReadingIds progress }
            { state with ProfileCleanerState = newState }, Cmd.none
        | _ ->
            state, Cmd.none

    | IdDataReadingCompleted data ->
        let task () = ProfileCleanerTool.cleanProfile state.ProfileCleanerState.IsDryRun data state.Config.ProfilePath
        let newState = { state.ProfileCleanerState with CurrentStep = ProfileCleanerStep.CleaningProfile }

        { state with ProfileCleanerState = newState },
        Cmd.OfAsync.either task () (ProfileCleaned >> ToolsMsg) ErrorOccurred

    | ProfileCleaned result ->
        // IsDryRun cannot be modified once the cleaner is started
        let newCurrentStep = ProfileCleanerStep.Completed (state.ProfileCleanerState.IsDryRun, result)

        { state with ProfileCleanerState = { state.ProfileCleanerState with CurrentStep = newCurrentStep } }, Cmd.none

    | SetProfileCleanerDryRun isDryRun ->
        { state with ProfileCleanerState = { state.ProfileCleanerState with IsDryRun = isDryRun } }, Cmd.none
