module ToneImportTests

open Expecto
open DLCBuilder
open Rocksmith2014.Common.Manifest
open Avalonia.Controls.Selection

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
        testCase "ImportSelectedTones imports selected tones" <| fun _ ->
            let selection = SelectionModel([ testTone; testTone ], SingleSelect = false)
            selection.SelectAll()
            let state = { initialState with SelectedImportTones = selection }
            let newState, _ = Main.update ImportSelectedTones state

            Expect.hasLength newState.Project.Tones 2 "Two tones were added to the project"
            Expect.equal newState.Project.Tones.Head.ToneDescriptors [| "$[35720]CLEAN" |] "A descriptor was added to the tone"

        testCase "ImportTones imports tones" <| fun _ ->
            let newState, _ = Main.update (ImportTones [ testTone ]) initialState

            Expect.hasLength newState.Project.Tones 1 "One tone was added to the project"
            Expect.equal newState.Project.Tones.Head.ToneDescriptors [| "$[35720]CLEAN" |] "A descriptor was added to the tone"

        testCase "ShowImportToneSelector opens tone selector for multiple tones" <| fun _ ->
            let newState, _ = Main.update (ShowImportToneSelector [| testTone; testTone |]) initialState

            Expect.equal newState.Overlay (ImportToneSelector [| testTone; testTone |]) "Overlay was opened with two tones"

        testCase "ShowImportToneSelector shows error message for empty array" <| fun _ ->
            let newState, _ = Main.update (ShowImportToneSelector Array.empty) initialState

            match newState.Overlay with
            | ErrorMessage (_, info) ->
                Expect.isNone info "Error message overlay was opened"
            | _ ->
                failwith "Overlay was not set to error message"
    ]
