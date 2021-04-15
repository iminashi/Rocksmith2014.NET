module DLCBuilder.Tools

open System
open System.IO
open Elmish
open Rocksmith2014.PSARC
open Rocksmith2014.XML

let psarcUnpackProgress = Progress<float>()

let update msg state =
    match msg with
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
            Async.Parallel(computations, Math.Max (1, Environment.ProcessorCount / 4))
        state, Cmd.OfAsync.attempt task () ErrorOccurred
