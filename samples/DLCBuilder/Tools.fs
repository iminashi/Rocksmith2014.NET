module DLCBuilder.Tools

open Elmish
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.PSARC
open Rocksmith2014.XML
open System
open System.IO

let psarcUnpackProgress = Progress<float>()

module ToneInjector =
    open Newtonsoft.Json
    open Newtonsoft.Json.Linq
    open Rocksmith2014.Common.Manifest

    let backupProfile profilePath =
        File.Copy(profilePath, $"%s{profilePath}.backup", overwrite=true)

    /// Converts an array of tones into a JArray.
    let private createToneJArray (tones: Tone array) =
        let dtos =
            tones
            |> Array.truncate 50
            |> Array.mapi (fun i tone ->
                let dto = Tone.toDto tone
                { dto with SortOrder = Nullable(float32 (i + 1))
                           IsCustom = Nullable(true) })

        JArray.FromObject(dtos, JsonSerializer(NullValueHandling = NullValueHandling.Ignore))

    /// Replaces the custom tones in the profile file with tones read from the files.
    let injectTones profilePath toneFiles = async  {
        let! tones =
            let tasks =
                toneFiles
                |> Array.map (fun path -> async {
                    match path with
                    | EndsWith "xml" ->
                        return Tone.fromXmlFile path |> Array.singleton
                    | EndsWith "json" ->
                        return! Tone.fromJsonFile path |> Async.map Array.singleton
                    | EndsWith "psarc" ->
                        return! Utils.importTonesFromPSARC path
                    | _ ->
                        return Array.empty })
            Async.Parallel(tasks, Environment.ProcessorCount)
            |> Async.map Array.concat

        let! profile, id = Profile.readAsJToken profilePath

        profile.Item("CustomTones").Replace(createToneJArray tones)

        backupProfile profilePath
    
        do! Profile.saveJToken profilePath id profile }

let update msg state =
    match msg with
    | ConvertWemToOgg files ->
        let task () = async {
            files
            |> Array.iter Conversion.wemToOgg }

        Utils.addTask WemToOggConversion state,
        Cmd.OfAsync.either task () (fun () -> WemToOggConversionCompleted) (fun ex -> TaskFailed(ex, WemToOggConversion))

    | UnpackPSARC file ->
        let targetDirectory = Path.Combine(Path.GetDirectoryName file, Path.GetFileNameWithoutExtension file)
        Directory.CreateDirectory targetDirectory |> ignore

        let task () = async {
            use psarc = PSARC.ReadFile file
            do! psarc.ExtractFiles(targetDirectory, psarcUnpackProgress) }

        Utils.addTask PsarcUnpack state,
        Cmd.OfAsync.either task () (fun () -> PsarcUnpacked) (fun ex -> TaskFailed(ex, PsarcUnpack))

    | RemoveDD files ->
        let task () =
            let computations =
                files
                |> Array.map (fun file -> async {
                    let arrangement = InstrumentalArrangement.Load file
                    do! arrangement.RemoveDD false
                    arrangement.Save file })
            Async.Parallel(computations, max 1 (Environment.ProcessorCount / 4))
        state, Cmd.OfAsync.attempt task () ErrorOccurred

    | InjectTonesIntoProfile files ->
        let cmd =
            if String.notEmpty state.Config.ProfilePath then
                let task() = async { do! ToneInjector.injectTones state.Config.ProfilePath files }
                Cmd.OfAsync.attempt task () ErrorOccurred
            else
                Cmd.none
        state, cmd
