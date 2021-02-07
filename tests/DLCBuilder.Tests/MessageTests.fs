module MessageTests

open Expecto
open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System

let initialState =
    { Project = DLCProject.Empty
      SavedProject = DLCProject.Empty
      RecentFiles = []
      Config = Configuration.Default
      CoverArt = None
      SelectedArrangement = None
      SelectedTone = None
      ShowSortFields = false
      ShowJapaneseFields = false
      Overlay = NoOverlay
      ImportTones = []
      PreviewStartTime = TimeSpan()
      RunningTasks = Set.empty
      CurrentPlatform = if OperatingSystem.IsMacOS() then Mac else PC
      OpenProjectFile = None
      ArrangementIssues = Map.empty
      Localization = Localization(Locales.English) }

[<Tests>]
let messageTests =
    testList "Message Tests" [
        testCase "CloseOverlay closes overlay" <| fun _ ->
            let state = { initialState with Overlay = SelectPreviewStart(TimeSpan.MinValue)}

            let newState, _ = Main.update CloseOverlay state

            Expect.equal newState.Overlay NoOverlay "Overlay was closed"

        testCase "ChangeLocale changes locale" <| fun _ ->
            let newState, _ = Main.update (ChangeLocale(Locales.Finnish)) initialState

            Expect.equal newState.Config.Locale Locales.Finnish "Locale was changed"

        testCase "ErrorOccurred opens error message" <| fun _ ->
            let ex = exn("TEST")

            let newState, _ = Main.update (ErrorOccurred(ex)) initialState

            Expect.equal newState.Overlay (ErrorMessage(ex.Message, Some ex.StackTrace)) "Overlay is ErrorMessage"

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
    ]
