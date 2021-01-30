module WwiseTests

open Expecto
open Rocksmith2014.Audio
open System.IO
open System

let previewPath = Utils.createPreviewAudioPath TestFiles.WaveFile
let mainWemPath = Path.ChangeExtension(TestFiles.WaveFile, "wem")
let previewWemPath = Path.ChangeExtension(previewPath, "wem")

do if not <| File.Exists previewPath then
       Preview.create TestFiles.WaveFile previewPath (TimeSpan.FromSeconds 0.)

[<Tests>]
let wwiseTests =
    testSequenced <| testList "Wwise Conversion Tests" [
        testAsync "Main and preview audio can be converted (Wave files)" {
            if File.Exists mainWemPath then File.Delete mainWemPath
            if File.Exists previewWemPath then File.Delete previewWemPath

            do! Wwise.convertToWem None TestFiles.WaveFile

            Expect.isTrue (File.Exists mainWemPath) "Main wem file was created"
            Expect.isTrue (File.Exists previewWemPath) "Preview wem file was created" }
    ]
