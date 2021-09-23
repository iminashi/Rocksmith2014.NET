module DLCProjectTests

open Expecto
open Rocksmith2014.DLCProject
open System.IO

[<Tests>]
let dlcProjectTests =
    testSequenced <| testList "DLC Project Tests" [
        test "Audio files that need converting can be discovered" {
            let files = DLCProject.getFilesThatNeedConverting testProject

            Expect.hasLength files 2 "Two files need converting"
            Expect.sequenceEqual files [ "audio.ogg"; "audio_preview.wav" ] "Filenames are correct"
        }

        test "Existing audio file does not need converting" {
            File.WriteAllText("audio.wem", "dummy content")
            let files = DLCProject.getFilesThatNeedConverting testProject

            Expect.hasLength files 1 "One file needs converting"
            File.Delete "audio.wem"
        }

        test "Custom audio files that need converting can be discovered" {
            let lead = { testLead with CustomAudio = Some { Path = "custom.ogg"; Volume = 0. } }
            let project = { testProject with Arrangements = [ Instrumental lead ] }
            let files = DLCProject.getFilesThatNeedConverting project

            Expect.hasLength files 3 "Three files need converting"
        }
    ]
