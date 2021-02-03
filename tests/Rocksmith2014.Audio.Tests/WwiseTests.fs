module WwiseTests

open Expecto
open Rocksmith2014.Audio
open System.IO

let wemPath = Path.ChangeExtension(TestFiles.WaveFile, "wem")

[<Tests>]
let wwiseTests =
    testSequenced <| testList "Wwise Conversion Tests" [
        testAsync "Wave file can be converted" {
            if File.Exists wemPath then File.Delete wemPath

            do! Wwise.convertToWem None TestFiles.WaveFile
            let info = FileInfo(wemPath)          

            Expect.isTrue info.Exists "Wem file was created"
            Expect.isGreaterThan info.Length 10_000L "File is larger than 10KB" }

        testAsync "Vorbis file can be converted" {
            if File.Exists wemPath then File.Delete wemPath

            do! Wwise.convertToWem None TestFiles.VorbisFile
            let info = FileInfo(wemPath)

            Expect.isTrue info.Exists "Wem file was created"
            Expect.isGreaterThan info.Length 10_000L "File is larger than 10KB" }
    ]
