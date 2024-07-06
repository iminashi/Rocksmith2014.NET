module EditToneMessageTests

open Elmish
open Expecto
open DLCBuilder
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject

let testPedal =
    { Type = "Test"
      KnobValues = Map.empty
      Key = "test"
      Category = None
      Skin = None
      SkinIndex = None }

let tone: Tone =
    { GearList =
        { Racks = Array.replicate 4 (Some testPedal)
          Amp = testPedal
          Cabinet = testPedal
          PrePedals = Array.replicate 4 (Some testPedal)
          PostPedals = Array.replicate 4 (Some testPedal) }
      ToneDescriptors = [| "TEST"; "TONE" |]
      NameSeparator = " "
      Volume = -10.
      MacVolume = None
      Key = "key"
      Name = "name"
      SortOrder = None }

let project = { initialState.Project with Tones = [ tone ] }
let state = { initialState with Project = project; SelectedToneIndex = 0 }

let repository = ToneGear.loadRepository().Result

[<Tests>]
let editToneTests =
    testList "EditTone Message Tests" [
        testCase "EditTone does nothing when no tone selected" <| fun _ ->
            let newState, _ = Main.update (EditTone (SetKey "ABCDEFG")) initialState

            Expect.equal newState initialState "State was not changed"

        testCase "SetName, SetKey, SetVolume" <| fun _ ->
            let messages =
                [ SetName "Test Tone"
                  SetKey "Test_key"
                  SetVolume 8.5 ]
                |> List.map EditTone

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
            let newTone = newState.Project.Tones |> List.head

            Expect.equal newState.SelectedToneIndex 0 "First tone is selected"
            Expect.equal newTone.Name "Test Tone" "Name is correct"
            Expect.equal newTone.Key "Test_key" "Key is correct"
            Expect.equal newTone.Volume -8.5 "Volume is correct"

        testCase "AddDescriptor adds a descriptor" <| fun _ ->
            let newState, _ = Main.update (EditTone AddDescriptor) state
            let newTone = newState.Project.Tones |> List.head

            Expect.hasLength newTone.ToneDescriptors 3 "Tone has three descriptors"
            Expect.equal newTone.ToneDescriptors.[0] newTone.ToneDescriptors.[1] "First tone descriptor is the same as the previous first one"

        testCase "RemoveDescriptor removes a descriptor" <| fun _ ->
            let newState, _ = Main.update (EditTone RemoveDescriptor) state
            let newTone = newState.Project.Tones |> List.head

            Expect.hasLength newTone.ToneDescriptors 1 "Tone has one descriptors"
            Expect.equal newTone.ToneDescriptors.[0] "TONE" "First tone descriptor is correct"

        testCase "ChangeDescriptor changes a descriptor" <| fun _ ->
            let newState, _ = Main.update (EditTone(ChangeDescriptor(0, ToneDescriptor.all.[2]))) state
            let newTone = newState.Project.Tones |> List.head

            Expect.hasLength newTone.ToneDescriptors 2 "Tone has two descriptors"
            Expect.equal newTone.ToneDescriptors.[0] "$[35715]BASS" "First tone descriptor is correct"

        testCase "RemovePedal removes pre-pedal" <| fun _ ->
            let state = { state with SelectedGearSlot = ToneGear.PrePedal 0 }

            let newState, _ = Main.update (EditTone RemovePedal) state
            let newTone = newState.Project.Tones |> List.head

            Expect.isNone newTone.GearList.PrePedals.[3] "Fourth pre-pedal slot is empty"

        testCase "RemovePedal removes post-pedal" <| fun _ ->
            let state = { state with SelectedGearSlot = ToneGear.PostPedal 2 }
            let messages = [ RemovePedal; RemovePedal ] |> List.map EditTone

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)

            let newTone = newState.Project.Tones |> List.head

            Expect.isNone newTone.GearList.PostPedals.[2] "Third post-pedal slot is empty"
            Expect.isNone newTone.GearList.PostPedals.[3] "Fourth post-pedal slot is empty"

        testCase "RemovePedal removes rack" <| fun _ ->
            let state = { state with SelectedGearSlot = ToneGear.Rack 3 }

            let newState, _ = Main.update (EditTone RemovePedal) state
            let newTone = newState.Project.Tones |> List.head

            Expect.isNone newTone.GearList.Racks.[3] "Fourth rack slot is empty"

        testCase "SetPedal sets amp" <| fun _ ->
            let state = { state with SelectedGearSlot = ToneGear.Amp }
            let newAmp = repository.Amps.[0]

            let newState, _ = Main.update (EditTone (SetPedal newAmp)) state
            let newTone = newState.Project.Tones |> List.head

            Expect.equal newTone.GearList.Amp.Key newAmp.Key "Amp key is correct"
            Expect.equal newTone.GearList.Amp.Type newAmp.Type "Amp type is correct"
            Expect.equal newTone.GearList.Amp.KnobValues.Count newAmp.Knobs.Value.Length "Knob value count is correct"

        testCase "SetPedal sets cabinet" <| fun _ ->
            let state = { state with SelectedGearSlot = ToneGear.Cabinet }
            let newCab = repository.CabinetChoices.[0]

            let newState, _ = Main.update (EditTone (SetPedal newCab)) state
            let newTone = newState.Project.Tones |> List.head

            Expect.equal newTone.GearList.Cabinet.Key newCab.Key "Cabinet key is correct"
            Expect.equal newTone.GearList.Cabinet.Type newCab.Type "Cabinet type is correct"
            Expect.equal newTone.GearList.Cabinet.KnobValues.Count 0 "There are no knob values"

        testCase "SetPedal sets pre-pedal" <| fun _ ->
            let state = { state with SelectedGearSlot = ToneGear.PrePedal 0 }
            let newPedal = repository.Pedals.[0]

            let newState, _ = Main.update (EditTone (SetPedal newPedal)) state
            let newTone = newState.Project.Tones |> List.head

            Expect.equal newTone.GearList.PrePedals.[0].Value.Key newPedal.Key "Pedal key is correct"
            Expect.equal newTone.GearList.PrePedals.[0].Value.Type newPedal.Type "Pedal type is correct"
            Expect.equal newTone.GearList.PrePedals.[0].Value.KnobValues.Count newPedal.Knobs.Value.Length "Knob value count is correct"

        testCase "SetPedal sets post-pedal" <| fun _ ->
            let state = { state with SelectedGearSlot = ToneGear.PostPedal 1 }
            let newPedal = repository.Pedals.[1]

            let newState, _ = Main.update (EditTone (SetPedal newPedal)) state
            let newTone = newState.Project.Tones |> List.head

            Expect.equal newTone.GearList.PostPedals.[1].Value.Key newPedal.Key "Pedal key is correct"
            Expect.equal newTone.GearList.PostPedals.[1].Value.Type newPedal.Type "Pedal type is correct"
            Expect.equal newTone.GearList.PostPedals.[1].Value.KnobValues.Count newPedal.Knobs.Value.Length "Knob value count is correct"

        testCase "SetPedal sets rack" <| fun _ ->
            let state = { state with SelectedGearSlot = ToneGear.Rack 3 }
            let newPedal = repository.Pedals.[5]

            let newState, _ = Main.update (EditTone (SetPedal newPedal)) state
            let newTone = newState.Project.Tones |> List.head

            Expect.equal newTone.GearList.Racks.[3].Value.Key newPedal.Key "Pedal key is correct"
            Expect.equal newTone.GearList.Racks.[3].Value.Type newPedal.Type "Pedal type is correct"
            Expect.equal newTone.GearList.Racks.[3].Value.KnobValues.Count newPedal.Knobs.Value.Length "Knob value count is correct"

        testCase "SetKnobValue sets a knob value" <| fun _ ->
            let newPedal = repository.Pedals.[5]
            let knobKey = newPedal.Knobs.Value.[0].Key
            let initialState = { state with SelectedGearSlot = ToneGear.PrePedal 0 }
            let messages =
                [ SetPedal newPedal
                  SetKnobValue(knobKey, 99f) ]
                |> List.map EditTone

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
            let newTone = newState.Project.Tones |> List.head

            Expect.equal newTone.GearList.PrePedals.[0].Value.KnobValues.[knobKey] 99f "Knob value is correct"

        testCase "SetKnobValue does not add a new knob value" <| fun _ ->
            let newPedal = repository.Racks.[2]
            let knobKey = "noSuchKey"
            let initialState = { state with SelectedGearSlot = ToneGear.Rack 1 }
            let messages =
                [ SetPedal newPedal
                  SetKnobValue(knobKey, 99f) ]
                |> List.map EditTone

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
            let newTone = newState.Project.Tones |> List.head

            Expect.equal newTone.GearList.Racks.[1].Value.KnobValues.Count newPedal.Knobs.Value.Length "Knob value count is correct"
            Expect.isFalse (newTone.GearList.Racks.[1].Value.KnobValues.ContainsKey knobKey) "Knob value was not added"
    ]
