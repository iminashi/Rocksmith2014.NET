module ConversionTests

open Expecto
open Rocksmith2014.Audio
open System.IO

[<Tests>]
let conversionTests =
    testList "Audio Conversion Tests" [
        testCase "Vorbis file can be converted to wave file" <| fun _ ->
            let targetFile = "convtest.wav"
            if File.Exists targetFile then File.Delete targetFile
            let oggLength = Utils.getLength TestFiles.VorbisFile

            Conversion.oggToWav TestFiles.VorbisFile targetFile
            let wavLength = Utils.getLength targetFile

            Expect.equal wavLength oggLength "Converted file is same length as the original file"

        testCase "Wem file can be converted to vorbis file" <| fun _ ->
            let targetFile = Path.ChangeExtension(TestFiles.WemFile, "ogg")
            if File.Exists targetFile then File.Delete targetFile

            Conversion.wemToOgg <| Path.GetFullPath(TestFiles.WemFile)
            let oggLength = Utils.getLength targetFile

            Expect.equal 42 oggLength.Seconds "Converted file is same length as the original file"

        testCase "Conversion throws exception on error" <| fun _ ->
            let wemFile = "nosuchfile.wem"

            Expect.throwsC (fun () -> Conversion.wemToOgg wemFile)
                           (fun ex -> Expect.stringContains ex.Message "Process failed with output" "Exception contains correct message")
    ]
