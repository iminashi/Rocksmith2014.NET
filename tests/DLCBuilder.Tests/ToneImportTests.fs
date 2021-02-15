module ToneImportTests

open Expecto
open DLCBuilder
open Rocksmith2014.Common.Manifest
open System

let testTone =
    { GearList =
        { Rack1 = Pedal()
          Rack2 = Unchecked.defaultof<Pedal>
          Rack3 = Unchecked.defaultof<Pedal>
          Rack4 = Unchecked.defaultof<Pedal>
          Amp = Pedal()
          Cabinet = Pedal()
          PrePedal1 = Pedal()
          PrePedal2 = Unchecked.defaultof<Pedal>
          PrePedal3 = Unchecked.defaultof<Pedal>
          PrePedal4 = Unchecked.defaultof<Pedal>
          PostPedal1 = Pedal()
          PostPedal2 = Unchecked.defaultof<Pedal>
          PostPedal3 = Unchecked.defaultof<Pedal>
          PostPedal4 = Unchecked.defaultof<Pedal> }
      ToneDescriptors = [||]
      NameSeparator = " - "
      IsCustom = Nullable(true)
      Volume = "1"
      MacVolume = null
      Key = "tone"
      Name = "tone"
      SortOrder = Nullable() }

[<Tests>]
let tests =
    testList "Tone Import Tests" [
        testCase "ImportSelectedTones imports selected tones" <| fun _ ->
            let state = { initialState with ImportTones = [ testTone; testTone ] }
            let newState, _ = Main.update ImportSelectedTones state

            Expect.hasLength newState.Project.Tones 2 "Two tones were added to the project"
            Expect.equal newState.Project.Tones.Head.ToneDescriptors [| "$[35720]CLEAN" |] "A descriptor was added to the tone"

        testCase "ImportTones imports tones" <| fun _ ->
            let newState, _ = Main.update (ImportTones [ testTone ]) initialState

            Expect.hasLength newState.Project.Tones 1 "One tone was added to the project"
            Expect.equal newState.Project.Tones.Head.ToneDescriptors [| "$[35720]CLEAN" |] "A descriptor was added to the tone"

        testCase "ShowImportToneSelector imports one tone immediately" <| fun _ ->
            let newState, _ = Main.update (ShowImportToneSelector [| testTone |]) initialState

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
