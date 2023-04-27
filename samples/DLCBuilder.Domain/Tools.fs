module DLCBuilder.Tools

open Elmish
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.PSARC
open Rocksmith2014.XML
open System
open System.IO

module ToneInjector =
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq
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

                    use psarc = PSARC.ReadFile(path)
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
