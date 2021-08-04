module EditInstrumentalTests

open Expecto
open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Elmish
open System

let lead =
    { XML = "instrumental.xml"
      Name = ArrangementName.Lead
      RouteMask = RouteMask.Lead
      Priority = ArrangementPriority.Main
      TuningPitch = 440.
      Tuning = [|0s;0s;0s;0s;0s;0s|]
      BaseTone = "Base_Tone"
      Tones = ["Tone_1"; "Tone_2"; "Tone_3"; "Tone_4"]
      ScrollSpeed = 1.3
      BassPicked = false
      MasterID = 12345
      PersistentID = Guid.NewGuid()
      CustomAudio = None }
    |> Instrumental

let project = { initialState.Project with Arrangements = [ lead ] }
let state = { initialState with Project = project; SelectedArrangementIndex = 0 }

[<Tests>]
let tests =
    testList "EditInstrumental Message Tests" [
        testCase "SetRouteMask, SetPriority, SetBassPicked, SetScrollSpeed" <| fun _ ->
            let messages = [ SetRouteMask RouteMask.Bass
                             SetPriority ArrangementPriority.Bonus
                             SetBassPicked true
                             SetScrollSpeed 2.0 ] |> List.map EditInstrumental

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
            let newArr = newState.Project.Arrangements |> List.head
            
            match newArr with
            | Instrumental inst ->
                Expect.equal inst.RouteMask RouteMask.Bass "Route mask is correct"
                Expect.equal inst.Priority ArrangementPriority.Bonus "Priority is correct"
                Expect.isTrue inst.BassPicked "Bass picked is true"
                Expect.equal inst.ScrollSpeed 2.0 "Scroll speed is correct"
            | _ ->
                failwith "fail"

        testCase "SetArrangementName, SetTuning, SetTuningPitch, SetBaseTone" <| fun _ ->
            let messages = [ SetArrangementName ArrangementName.Bass
                             SetTuning (4, -2s)
                             SetTuningPitch 358.
                             SetBaseTone "new_tone" ] |> List.map EditInstrumental

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
            let newArr = newState.Project.Arrangements |> List.head
            
            match newArr with
            | Instrumental inst ->
                Expect.equal inst.Name ArrangementName.Bass "Arrangement name is correct"
                Expect.equal inst.Tuning.[4] -2s "Tuning was changed"
                Expect.equal inst.TuningPitch 358. "Tuning pitch is correct"
                Expect.equal inst.BaseTone "new_tone" "Base tone is correct"
            | _ ->
                failwith "fail"

        testCase "SetMasterId, SetPersistentId, SetCustomAudioPath, SetCustomAudioVolume" <| fun _ ->
            let id = Guid.NewGuid()
            let messages = [ SetMasterId 55555
                             SetPersistentId id
                             SetCustomAudioPath (Some "custom/audio")
                             SetCustomAudioVolume -10. ] |> List.map EditInstrumental

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
            let newArr = newState.Project.Arrangements |> List.head
            
            match newArr with
            | Instrumental inst ->
                Expect.equal inst.MasterID 55555 "Master ID is correct"
                Expect.equal inst.PersistentID id "Persistent ID is correct"
                Expect.equal inst.CustomAudio.Value.Path "custom/audio" "Custom audio path is correct"
                Expect.equal inst.CustomAudio.Value.Volume -10. "Custom audio volume is correct"
            | _ ->
                failwith "fail"
    ]