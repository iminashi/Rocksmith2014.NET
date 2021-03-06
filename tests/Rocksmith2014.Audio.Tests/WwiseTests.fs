﻿module WwiseTests

open Expecto
open Rocksmith2014.Audio
open System.IO

let testConversion testFile = async {
    let wemPath = Path.ChangeExtension(testFile, "wem")
    if File.Exists wemPath then File.Delete wemPath

    do! Wwise.convertToWem None testFile
    let info = FileInfo(wemPath)          

    Expect.isTrue info.Exists "Wem file was created"
    Expect.isGreaterThan info.Length 100_000L "File is larger than 100KB" }

[<Tests>]
let wwiseTests =
    testList "Wwise Conversion Tests" [
        testAsync "Wave file can be converted" { do! testConversion TestFiles.WaveFile }
        testAsync "Vorbis file can be converted" { do! testConversion TestFiles.VorbisFile }
    ]
