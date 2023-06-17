module UtilsTests

open Expecto
open DLCBuilder

[<Tests>]
let tests =
    testList "Utility Function Tests" [
        testCase "Preview audio file path is created correctly" <| fun _ ->
            let previewPath = Utils.previewPathFromMainAudio "C:\Test\audio.wem"

            Expect.equal previewPath "C:\Test\audio_preview.wem" "Preview path is correct"

        testCase "Tone descriptors can be added to a tone" <| fun _ ->
            let originalTone = { testTone with Key = "tone_dist"; Name = "tone_dist" }

            let updatedTone = Utils.addDescriptors originalTone

            Expect.hasLength updatedTone.ToneDescriptors 1 "One descriptor was added"
            Expect.stringContains updatedTone.ToneDescriptors.[0] "DISTORTION" "Description contains correct word"

        testCase "removeAndShift returns correct array" <| fun _ ->
            let initial = [| Some 1; Some 2; Some 3 |]

            let updated = Utils.removeAndShift 0 initial

            Expect.equal updated [| Some 2; Some 3; None |] "Updated array is correct after one remove"

            let updated = Utils.removeAndShift 1 updated

            Expect.equal updated [| Some 2; None; None |] "Updated array is correct after two removes"

        testCase "removeSelected returns correct list and index" <| fun _ ->
            let initial = [ 1; 2; 3 ]

            let updated, index = Utils.removeSelected initial 0

            Expect.equal updated [ 2; 3 ] "Updated list is correct after one remove"
            Expect.equal index 0 "Updated index is correct after one remove"

            let updated, index = Utils.removeSelected updated 1

            Expect.equal updated [ 2 ] "Updated list is correct after two removes"
            Expect.equal index 0 "Updated index is correct after two remove"

            let updated, index = Utils.removeSelected updated 0

            Expect.equal updated [ ] "Updated list is correct after three removes"
            Expect.equal index -1 "Updated index is correct three two remove"
    ]
