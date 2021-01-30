module ConversionTests

open Expecto
open Rocksmith2014.Audio
open System.IO

let targetFile = "convtest.wav"

[<Tests>]
let conversionTests =
    testList "Audio Conversion Tests" [
        testCase "Vorbis file can be converted to wave file" <| fun _ ->
            if File.Exists targetFile then File.Delete targetFile
            let oggLength = Utils.getLength TestFiles.VorbisFile

            Conversion.oggToWav TestFiles.VorbisFile targetFile
            let wavLength = Utils.getLength targetFile

            Expect.equal wavLength oggLength  "Converted file is same length as the original file"
    ]
