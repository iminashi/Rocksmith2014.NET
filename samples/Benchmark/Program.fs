open System
open System.Diagnostics
open System.IO
open Rocksmith2014.Common
open Rocksmith2014.Conversion
open Rocksmith2014.DD
open Rocksmith2014.PSARC
open Rocksmith2014.SNG

let config =
    { PhraseSearch = PhraseSearch.WithThreshold 80
      LevelCountGeneration = LevelCountGeneration.Simple }

[<EntryPoint>]
let main argv =
    let directory = argv.[0]
    Directory.EnumerateFiles(directory, "*_p.psarc", SearchOption.AllDirectories)
    |> Seq.map (fun psarcPath -> async {
        printfn "Processing: %s" (Path.GetFileName psarcPath)

        use psarc = PSARC.ReadFile psarcPath

        return!
            psarc.Manifest
            |> List.filter (fun x -> x.EndsWith ".sng" && not <| x.Contains "vocal")
            |> List.map (fun sngPath -> async {
                //printfn "Arrangement: %s" sngPath

                let sw = Stopwatch.StartNew()

                use! data = psarc.GetEntryStream sngPath
                let! sng = SNG.fromStream data PC
                let xml = ConvertInstrumental.sngToXml None sng

                do! xml.RemoveDD() |> Async.AwaitTask
                ignore <| Generator.generateForArrangement config xml

                sw.Stop()
                //printfn "Time: %i ms" sw.ElapsedMilliseconds
                return sngPath, sw.ElapsedMilliseconds })
            |> Async.Sequential })
    |> Async.Sequential
    |> Async.RunSynchronously
    |> Seq.collect id
    |> Seq.filter (fun (_, time) -> time > 500L)
    |> Seq.sortByDescending snd
    |> Seq.iteri (fun i (fileName, time) -> printfn "\n%i: %s, %i ms" (i + 1) fileName time)

    0
