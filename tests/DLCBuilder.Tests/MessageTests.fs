module MessageTests

open Expecto
open DLCBuilder
open System

[<Tests>]
let messageTests =
    testList "Message Tests" [
        testCase "CloseOverlay closes overlay" <| fun _ ->
            let state = { initialState with Overlay = SelectPreviewStart(TimeSpan.MinValue)}

            let newState, _ = Main.update CloseOverlay state

            Expect.equal newState.Overlay NoOverlay "Overlay was closed"

        testCase "ChangeLocale changes locale" <| fun _ ->
            let newState, _ = Main.update (ChangeLocale(Locales.All.[1])) initialState

            Expect.equal newState.Config.Locale Locales.All.[1] "Locale was changed"

        testCase "ErrorOccurred opens error message" <| fun _ ->
            let ex = exn("TEST")

            let newState, _ = Main.update (ErrorOccurred(ex)) initialState

            Expect.equal newState.Overlay (ErrorMessage(ex.Message, None)) "Overlay is ErrorMessage"

        testCase "CheckArrangements adds long running task" <| fun _ ->
            let newState, cmd = Main.update CheckArrangements initialState

            Expect.isTrue (newState.RunningTasks.Contains ArrangementCheck) "Correct task was added"
            Expect.isNonEmpty cmd "Command was created"

        testCase "Build Release fails with empty project" <| fun _ ->
            let newState, _ = Main.update (Build Release) initialState

            match newState.Overlay with
            | ErrorMessage (message, info) ->
                Expect.isNotEmpty message "Message is not empty"
                Expect.isNone info "No extra information"
            | _ ->
                failwith "Unexpected overlay type"

        testCase "BuildComplete removes build task" <| fun _ ->
            let newState, _ = Main.update (BuildComplete()) { initialState with RunningTasks = Set([ BuildPackage ]) }

            Expect.isFalse (newState.RunningTasks.Contains BuildPackage) "Build task was removed"

        testCase "SetRecentFiles sets recent files" <| fun _ ->
            let newState, _ = Main.update (SetRecentFiles [ "recent_file" ]) initialState

            Expect.equal newState.RecentFiles [ "recent_file" ] "Recent file list was changed"

        testCase "ShowConfigEditor shows configuration editor" <| fun _ ->
            let newState, _ = Main.update ShowConfigEditor initialState

            Expect.equal newState.Overlay ConfigEditor "Overlay is set to configuration editor"
    ]
