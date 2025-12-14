module ToneImportTests

open Expecto
open DLCBuilder
open Rocksmith2014.Common.Manifest

let testPedal =
    { Type = "Test"
      KnobValues = Map.empty
      Key = "test"
      Category = None
      Skin = None
      SkinIndex = None }

let testTone: Tone =
    { GearList =
        { Racks = [| Some testPedal; None; None; None |]
          Amp = testPedal
          Cabinet = testPedal
          PrePedals = [| Some testPedal; None; None; None |]
          PostPedals = [| Some testPedal; None; None; None |] }
      ToneDescriptors = [||]
      NameSeparator = " - "
      Volume = 1.
      MacVolume = None
      Key = "tone"
      Name = "tone"
      SortOrder = None }

[<Tests>]
let tests =
    testList "Tone Import Tests" [
        testCase "ImportTones imports tones" <| fun _ ->
            let newState, _ = Main.update (ImportTones [ testTone ]) initialState

            Expect.hasLength newState.Project.Tones 1 "One tone was added to the project"
            Expect.equal newState.Project.Tones.Head.ToneDescriptors [| "$[35720]CLEAN" |] "A descriptor was added to the tone"

        testCase "ShowImportToneSelector opens tone selector for multiple tones" <| fun _ ->
            let newState, _ = Main.update (ShowImportToneSelector [| testTone; testTone |]) initialState

            Expect.equal newState.Modal (ImportToneSelector [| testTone; testTone |]) "Modal was opened with two tones"

        testCase "ShowImportToneSelector shows error message for empty array" <| fun _ ->
            let newState, _ = Main.update (ShowImportToneSelector Array.empty) initialState

            match newState.Modal with
            | ErrorMessage (_, info) ->
                Expect.isNone info "Error message modal was opened"
            | _ ->
                failwith "Modal was not set to error message"
    ]
