module DLCProjectTests

open Expecto
open Rocksmith2014.DLCProject
open System.IO
open System.Threading
open System

let private projectWithExistingFiles =
    let existingFile1, existingFile2 =
        let dir = AppDomain.CurrentDomain.BaseDirectory
        let path1, path2 = Path.Combine(dir, "dummy_audio.ogg"), Path.Combine(dir, "dummy_preview.ogg")
        if not <| File.Exists path1 then
            File.WriteAllText(path1, "dummy content")
        if not <| File.Exists path2 then
            File.WriteAllText(path2, "dummy content")
        path1, path2

    { testProject with
        AudioFile = { testProject.AudioFile with Path = existingFile1 }
        AudioPreviewFile = { testProject.AudioPreviewFile with Path = existingFile2 } }

let private defaultConversionTimeSpan = TimeSpan.FromSeconds(3.0)

[<Tests>]
let dlcProjectTests =
    testSequenced <| testList "DLC Project Tests" [
        test "Audio files that need converting can be discovered" {
            let files = DLCProject.getFilesThatNeedConverting defaultConversionTimeSpan projectWithExistingFiles

            Expect.hasLength files 2 "Two files need converting"
            Expect.sequenceEqual files [ projectWithExistingFiles.AudioFile.Path; projectWithExistingFiles.AudioPreviewFile.Path ] "Filenames are correct"
        }

        test "Audio file that is older than wem file does not need converting" {
            let audioPath = projectWithExistingFiles.AudioFile.Path
            let wemPath = Path.ChangeExtension(audioPath, "wem")
            File.WriteAllText(wemPath, "dummy content")

            let files = DLCProject.getFilesThatNeedConverting defaultConversionTimeSpan projectWithExistingFiles

            Expect.hasLength files 1 "One file needs converting"
            File.Delete wemPath
        }

        test "Custom audio files that need converting can be discovered" {
            let lead = { testLead with CustomAudio = Some projectWithExistingFiles.AudioFile }
            let project = { projectWithExistingFiles with Arrangements = [ Instrumental lead ] }

            let files = DLCProject.getFilesThatNeedConverting defaultConversionTimeSpan project

            Expect.hasLength files 3 "Three files need converting"
        }

        test "Audio file that is newer than converted wem file needs converting" {
            let audioPath = projectWithExistingFiles.AudioFile.Path
            let wemPath = Path.ChangeExtension(audioPath, "wem")
            File.WriteAllText(wemPath, "dummy content")
            // Ensure that the source audio file is newer
            let waitTime = TimeSpan.FromSeconds(0.5)
            Thread.Sleep(int waitTime.TotalMilliseconds + 200)
            File.WriteAllText(audioPath, "dummy content")

            let files = DLCProject.getFilesThatNeedConverting waitTime projectWithExistingFiles

            Expect.hasLength files 2 "Two files need converting"
            File.Delete audioPath
            File.Delete wemPath
        }
    ]
