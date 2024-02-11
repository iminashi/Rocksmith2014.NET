module EditInstrumentalTests

open DLCBuilder
open Elmish
open Expecto
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System

let lead =
    { Instrumental.Empty with
          XmlPath = "instrumental.xml"
          BaseTone = "Base_Tone"
          Tones = [ "Tone_1"; "Tone_2"; "Tone_3"; "Tone_4" ]
          MasterId = 12345 }
    |> Instrumental

let project = { initialState.Project with Arrangements = [ lead ] }
let state = { initialState with Project = project; SelectedArrangementIndex = 0 }

[<Tests>]
let tests =
    testList "EditInstrumental Message Tests" [
        testCase "EditInstrumental does nothing when no arrangement selected" <| fun _ ->
            let newState, _ = Main.update (EditInstrumental (SetRouteMask RouteMask.Rhythm)) initialState

            Expect.equal newState initialState "State was not changed"

        testCase "SetRouteMask, SetPriority, SetBassPicked, SetScrollSpeed" <| fun _ ->
            let messages =
                [ SetRouteMask RouteMask.Bass
                  SetPriority ArrangementPriority.Bonus
                  SetBassPicked true
                  SetScrollSpeed 2.0 ]
                |> List.map EditInstrumental

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
            let messages =
                [ SetArrangementName ArrangementName.Bass
                  SetTuning(4, -2s)
                  SetTuningPitch 358.
                  SetBaseTone "new_tone" ]
                |> List.map EditInstrumental

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
            let messages =
                [ SetMasterId 55555
                  SetPersistentId id
                  SetCustomAudioPath(Some "custom/audio")
                  SetCustomAudioVolume -10. ]
                |> List.map EditInstrumental

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
            let newArr = newState.Project.Arrangements |> List.head

            match newArr with
            | Instrumental inst ->
                Expect.equal inst.MasterId 55555 "Master ID is correct"
                Expect.equal inst.PersistentId id "Persistent ID is correct"
                Expect.equal inst.CustomAudio.Value.Path "custom/audio" "Custom audio path is correct"
                Expect.equal inst.CustomAudio.Value.Volume -10. "Custom audio volume is correct"
            | _ ->
                failwith "fail"

        testCase "SetCustomAudioPath" <| fun _ ->
            let messages = [ SetCustomAudioPath (Some "custom_audio") ] |> List.map EditInstrumental

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
            let newArr = newState.Project.Arrangements |> List.head

            match newArr with
            | Instrumental { CustomAudio = Some customAudio } ->
                Expect.equal customAudio.Path "custom_audio" "Custom audio path is correct"
            | _ ->
                failwith "fail"

        testCase "ChangeTuning" <| fun _ ->
            let messages = [ ChangeTuning(0, Up); ChangeTuning(4, Down) ] |> List.map EditInstrumental

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
            let newArr = newState.Project.Arrangements |> List.head

            match newArr with
            | Instrumental inst ->
                Expect.equal inst.Tuning.[0] 1s "Tuning of first string was changed"
                Expect.equal inst.Tuning.[4] -1s "Tuning of fourth string was changed"
            | _ ->
                failwith "fail"

        testCase "ChangeTuningAll Down" <| fun _ ->
            let newState, _ = Main.update (ChangeTuningAll Down |> EditInstrumental) state
            let newArr = newState.Project.Arrangements |> List.head

            match newArr with
            | Instrumental inst ->
                Expect.all inst.Tuning (fun x -> x = -1s) "Tuning of all strings was lowered"
            | _ ->
                failwith "fail"

        testCase "ChangeTuningAll Up" <| fun _ ->
            let newState, _ = Main.update (ChangeTuningAll Up |> EditInstrumental) state
            let newArr = newState.Project.Arrangements |> List.head

            match newArr with
            | Instrumental inst ->
                Expect.all inst.Tuning (fun x -> x = 1s) "Tuning of all strings was raised"
            | _ ->
                failwith "fail"
    ]
