module SoundBankTests

open Expecto
open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open System.IO

[<Tests>]
let soundBankTests =
    testList "Sound Bank Tests" [
        test "Volume can be read" {
            use testFile = File.OpenRead "test.bnk"

            let result = SoundBank.readVolume testFile PC

            Expect.equal result (Ok -8.0f) "Volume is correct"
        }

        test "File ID can be read" {
            use testFile = File.OpenRead "test.bnk"

            let result = SoundBank.readFileId testFile PC

            Expect.equal result (Ok 1882293402) "File ID is correct"
        }
    ]
