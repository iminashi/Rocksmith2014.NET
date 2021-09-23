module PreviewTests

open Expecto
open Rocksmith2014.Audio
open System
open System.IO

let previewStart = TimeSpan.FromSeconds 0.
let expectedLength = TimeSpan.FromSeconds 28.

[<Tests>]
let previewTests =
    testList "Preview Audio Tests" [
        testCase "Preview audio file can be created from wave file" <| fun _ ->
            let previewFileName = "wavtest_preview.wav"
            if File.Exists(previewFileName) then File.Delete(previewFileName)

            Preview.create TestFiles.WaveFile previewFileName previewStart
            let length = Utils.getLength previewFileName

            Expect.equal length expectedLength "Length of preview audio is correct"

        testCase "Preview audio file can be created from vorbis file" <| fun _ ->
            let previewFileName = "oggtest_preview.wav"
            if File.Exists(previewFileName) then File.Delete(previewFileName)

            Preview.create TestFiles.VorbisFile previewFileName previewStart
            let length = Utils.getLength previewFileName

            Expect.equal length expectedLength "Length of preview audio is correct"
    ]
