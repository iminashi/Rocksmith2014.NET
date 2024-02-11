module EditVocalsMessageTests

open Elmish
open Expecto
open DLCBuilder
open Rocksmith2014.DLCProject
open System

let vocals =
    { Id = ArrangementId.New
      XmlPath = ""
      Japanese = false
      CustomFont = None
      MasterId = 0
      PersistentId = Guid() }
    |> Vocals

let project = { initialState.Project with Arrangements = [ vocals ] }
let state = { initialState with Project = project; SelectedArrangementIndex = 0 }

[<Tests>]
let editVocalsTests =
    testList "EditVocals Message Tests" [
        testCase "EditVocals does nothing when no arrangement is selected" <| fun _ ->
            let newState, _ = Main.update (EditVocals (SetIsJapanese true)) initialState

            Expect.equal newState initialState "State was not changed"

        testCase "SetIsJapanese, SetCustomFont" <| fun _ ->
            let messages =
                [ SetIsJapanese true
                  SetCustomFont(Some "font") ]
                |> List.map EditVocals

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
            let newArr = newState.Project.Arrangements |> List.head

            Expect.equal newState.SelectedArrangementIndex 0 "Vocals arrangement is selected"
            match newArr with
            | Vocals vocals ->
                Expect.equal vocals.Japanese true "Japanese is true"
                Expect.equal vocals.CustomFont (Some "font") "Custom font is correct"
            | _ ->
                failwith "fail"

        testCase "SetVocalsMasterId, SetVocalsPersistentId" <| fun _ ->
            let newPersistentId = Guid.NewGuid()
            let messages =
                [ SetVocalsMasterId 5051
                  SetVocalsPersistentId newPersistentId ]
                |> List.map EditVocals

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
            let newArr = newState.Project.Arrangements |> List.head

            Expect.equal newState.SelectedArrangementIndex 0 "Vocals arrangement is selected"
            match newArr with
            | Vocals vocals ->
                Expect.equal vocals.MasterId 5051 "MasterID is correct"
                Expect.equal vocals.PersistentId newPersistentId "Persistent ID is correct"
            | _ ->
                failwith "fail"
    ]
