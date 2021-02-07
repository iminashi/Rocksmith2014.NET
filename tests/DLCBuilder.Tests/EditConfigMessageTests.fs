module EditMessageTests

open Expecto
open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open Elmish

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
let editConfigTests =
    testList "EditConfig Message Tests" [
        testCase "SetCharterName, SetAutoVolume, SetShowAdvanced, SetRemoveDDOnImport" <| fun _ ->
            let messages = [ SetCharterName "Tester"
                             SetAutoVolume true
                             SetShowAdvanced true
                             SetRemoveDDOnImport true ] |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
                
            Expect.equal newState.Config.CharterName "Tester" "Charter name is correct"
            Expect.equal newState.Config.AutoVolume true "Auto volume is correct"
            Expect.equal newState.Config.ShowAdvanced true "Show advanced is correct"
            Expect.equal newState.Config.RemoveDDOnImport true "Remove DD on import is correct"

        testCase "SetGenerateDD, SetDDPhraseSearchEnabled, SetDDPhraseSearchThreshold, SetApplyImprovements" <| fun _ ->
            let messages = [ SetGenerateDD false
                             SetDDPhraseSearchEnabled false
                             SetDDPhraseSearchThreshold 50
                             SetApplyImprovements false ] |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
                
            Expect.equal newState.Config.GenerateDD false "Generate DD is correct"
            Expect.equal newState.Config.DDPhraseSearchEnabled false "DD phrase search enabled is correct"
            Expect.equal newState.Config.DDPhraseSearchThreshold 50 "DD phrase search threshold is correct"
            Expect.equal newState.Config.ApplyImprovements false "Apply improvements is correct"

        testCase "SetSaveDebugFiles, SetCustomAppId, SetConvertAudio" <| fun _ ->
            let messages = [ SetSaveDebugFiles true
                             SetCustomAppId (Some "test")
                             SetConvertAudio ToWav ] |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
                
            Expect.equal newState.Config.SaveDebugFiles true "Save debug files is correct"
            Expect.equal newState.Config.CustomAppId (Some "test") "Custom app ID is correct"
            Expect.equal newState.Config.ConvertAudio ToWav "Convert audio is correct"

        testCase "AddReleasePlatform" <| fun _ ->
            let state = { initialState with Config = { initialState.Config with ReleasePlatforms = [] } }
            let messages = [ AddReleasePlatform PC
                             AddReleasePlatform Mac] |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
                
            Expect.contains newState.Config.ReleasePlatforms PC "Release platforms contains PC"
            Expect.contains newState.Config.ReleasePlatforms Mac "Release platforms contains Mac"

        testCase "RemoveReleasePlatform" <| fun _ ->
            let state = { initialState with Config = { initialState.Config with ReleasePlatforms = [ PC; Mac ] } }
            let messages = [ RemoveReleasePlatform PC
                             RemoveReleasePlatform Mac] |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
                
            Expect.isEmpty newState.Config.ReleasePlatforms "Release platforms is empty"

        testCase "SetTestFolderPath, SetProjectsFolderPath, SetWwiseConsolePath, SetProfilePath" <| fun _ ->
            let messages = [ SetTestFolderPath "TestFolder"
                             SetProjectsFolderPath "ProjectFolder"
                             SetWwiseConsolePath "WwiseConsole"
                             SetProfilePath "profile_prfldb" ] |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
                
            Expect.equal newState.Config.TestFolderPath "TestFolder" "Test folder path is correct"
            Expect.equal newState.Config.ProjectsFolderPath "ProjectFolder" "Projects folder path is correct"
            Expect.equal newState.Config.WwiseConsolePath (Some "WwiseConsole") "Wwise console path is correct"
            Expect.equal newState.Config.ProfilePath "profile_prfldb" "Profile path is correct"
    ]
