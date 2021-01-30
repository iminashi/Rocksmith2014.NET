module UtilsTests

open Expecto
open Rocksmith2014.Audio

[<Tests>]
let utilsTests =
    testList "Audio Utility Tests" [
        testCase "Length of wave file can be read" <| fun _ ->
            let expectedLength = 42_479.5918

            let length = Utils.getLength TestFiles.WaveFile
            let length = length.TotalMilliseconds

            Expect.equal length expectedLength "Wave file length is correct"

        testCase "Length of vorbis file can be read" <| fun _ ->
            let expectedLength = 42_479.5918

            let length = Utils.getLength TestFiles.VorbisFile
            let length = length.TotalMilliseconds

            Expect.equal length expectedLength "Vorbis file length is correct"

        testCase "Preview audio path can be created from main file path" <| fun _ ->
            let path = "C:\path\to\file.ext"

            let previewPath = Utils.createPreviewAudioPath path

            Expect.equal previewPath "C:\path\to\file_preview.wav" "Preview file path is correct"
    ]
