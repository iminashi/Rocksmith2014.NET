module EditToneMessageTests

open Expecto
open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Elmish

let testPedal =
    { Type = "Test"
      KnobValues = Map.empty
      Key = "test"
      Category = None
      Skin = None
      SkinIndex = None }

let tone: Tone =
    { GearList =
        { Racks = [| None; None; None; None |]
          Amp = testPedal
          Cabinet = testPedal
          PrePedals = [| None; None; None; None |]
          PostPedals = [| None; None; None; None |] }
      ToneDescriptors = [| "TEST"; "TONE" |]
      NameSeparator = " "
      IsCustom = None
      Volume = "-10.000"
      MacVolume = None
      Key = "key"
      Name = "name"
      SortOrder = None }

let project = { initialState.Project with Tones = [ tone ] }
let state = { initialState with Project = project; SelectedTone = Some tone }

[<Tests>]
let editToneTests =
    testList "EditTone Message Tests" [
        testCase "SetName, SetKey, SetVolume" <| fun _ ->
            let messages = [ SetName "Test Tone"
                             SetKey "Test_key"
                             SetVolume -8.5 ] |> List.map EditTone

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
            let newTone = newState.Project.Tones |> List.head

            Expect.equal newState.SelectedTone (Some newTone) "Tone is selected"
            Expect.equal newTone.Name "Test Tone" "Tone name is correct"
            Expect.equal newTone.Key "Test_key" "Tone key is correct"
            Expect.equal newTone.Volume "-8.500" "Tone key is correct"

        testCase "AddDescriptor" <| fun _ ->
            let newState, _ = Main.update (EditTone AddDescriptor) state
            let newTone = newState.Project.Tones |> List.head

            Expect.hasLength newTone.ToneDescriptors 3 "Tone has three descriptors"
            Expect.equal newTone.ToneDescriptors.[0] "$[35721]ACOUSTIC" "First tone descriptor is the default one (acoustic)"

        testCase "RemoveDescriptor" <| fun _ ->
            let newState, _ = Main.update (EditTone RemoveDescriptor) state
            let newTone = newState.Project.Tones |> List.head

            Expect.hasLength newTone.ToneDescriptors 1 "Tone has one descriptors"
            Expect.equal newTone.ToneDescriptors.[0] "TONE" "First tone descriptor is correct"

        testCase "ChangeDescriptor" <| fun _ ->
            let newState, _ = Main.update (EditTone (ChangeDescriptor(0, ToneDescriptor.all.[2]))) state
            let newTone = newState.Project.Tones |> List.head

            Expect.hasLength newTone.ToneDescriptors 2 "Tone has two descriptors"
            Expect.equal newTone.ToneDescriptors.[0] "$[35715]BASS" "First tone descriptor is correct"
    ]
