module UtilsTests

open Expecto
open Rocksmith2014.DLCProject

[<Tests>]
let utilsTests =
    testList "Utils Tests" [
        test "Correct tuning name is returned for standard tuning" {
            let eStandard = [| 0s; 0s; 0s;  0s; 0s; 0s |]

            let name = Utils.getTuningName eStandard
            let expected = Utils.TranslatableTuning("Standard", [| box "E" |])

            Expect.equal name expected "Incorrect tuning name"
        }

        test "Correct tuning name is returned for DADGAD" {
            let dadgad = [| -2s; 0s; 0s; 0s; -2s; -2s |]

            let name = Utils.getTuningName dadgad
            let expected = Utils.CustomTuning("DADGAD")

            Expect.equal name expected "Incorrect tuning name"
        }

        test "Correct tuning name is returned for custom tuning" {
            let custom = [| -1s; 1s; -12s; 0s; 2s; -13s |]

            let name = Utils.getTuningName custom
            let expected = Utils.CustomTuning("EbBbDGDbEb")

            Expect.equal name expected "Incorrect tuning name"
        }
    ]
