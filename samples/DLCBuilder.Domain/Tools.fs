module DLCBuilder.Tools

open Elmish
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.PSARC
open Rocksmith2014.XML
open System
open System.IO
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
    let readIdData dlcDirectory =
        async {
            let progressReporter (p: ProfileCleaner.IdReadingProgress) =
                // Only update the UI for every third file to keep it responsive
                if p.CurrentFileIndex % 3 = 0 then
                    (ProgressReporters.ProfileCleaner :> IProgress<float>).Report(100.0 * float p.CurrentFileIndex / float p.TotalFiles)

            return! ProfileCleaner.gatherIdAndKeyData progressReporter dlcDirectory
        }

    let cleanProfile data profilePath =
        async {
            let filterIds, filterKeys = ProfileCleaner.getFilteringFunctions data

            //ProfileCleaner.backupProfile profilePath

            let! profile, _profileId = Profile.readAsJToken profilePath |> Async.AwaitTask

            let pnRecs = filterIds profile["Playnexts"].["Songs"]
            let songRecs = filterIds profile["Songs"]
            let saRecs = filterIds profile["SongsSA"]
            let statsRecs = filterIds profile["Stats"].["Songs"]

            profile["SongListsRoot"]["SongLists"] :?> JArray
            |> Seq.iter (fun songList -> songList :?> JArray |> filterKeys)

            profile["FavoritesListRoot"]["FavoritesList"] :?> JArray
            |> filterKeys

            //do! Profile.saveJToken profilePath profileId profile |> Async.AwaitTask
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

                do! Async.Parallel(tasks, 4) |> Async.Ignore
            }

        StateUtils.addTask WemConversion state,
        Cmd.OfAsync.either task () WemConversionComplete (fun ex -> TaskFailed(ex, WemConversion))

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
        match state.ProfileCleanerState with
        | ProfileCleanerState.Idle
        | ProfileCleanerState.Completed _ when File.Exists(state.Config.ProfilePath) && Directory.Exists(state.Config.DlcFolderPath) ->
            { state with ProfileCleanerState = ProfileCleanerState.ReadingIds 0.0 },
            Cmd.OfAsync.either ProfileCleanerTool.readIdData state.Config.DlcFolderPath (IdDataReadingCompleted >> ToolsMsg) ErrorOccurred
        | _ ->
            state, Cmd.none

    | ProfileCleanerProgressChanged progress ->
        match state.ProfileCleanerState with
        | ProfileCleanerState.ReadingIds _ ->
            { state with ProfileCleanerState = ProfileCleanerState.ReadingIds progress }, Cmd.none
        | _ ->
            state, Cmd.none

    | IdDataReadingCompleted data ->
        let task () = ProfileCleanerTool.cleanProfile data state.Config.ProfilePath

        { state with ProfileCleanerState = ProfileCleanerState.CleaningProfile },
        Cmd.OfAsync.either task () (ProfileCleaned >> ToolsMsg) ErrorOccurred

    | ProfileCleaned result ->
        { state with ProfileCleanerState = ProfileCleanerState.Completed result }, Cmd.none
