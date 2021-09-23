module EditMessageTests

open Elmish
open Expecto
open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.DD.Types

[<Tests>]
let editConfigTests =
    testList "EditConfig Message Tests" [
        testCase "SetCharterName, SetAutoVolume, SetShowAdvanced, SetRemoveDDOnImport" <| fun _ ->
            let autoVolume = not initialState.Config.AutoVolume
            let removeDD = not initialState.Config.RemoveDDOnImport
            let showAdvanced = not initialState.Config.ShowAdvanced
            let messages =
                [ SetCharterName "Tester"
                  SetAutoVolume autoVolume
                  SetShowAdvanced showAdvanced
                  SetRemoveDDOnImport removeDD ]
                |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
                
            Expect.equal newState.Config.CharterName "Tester" "Charter name is correct"
            Expect.equal newState.Config.AutoVolume autoVolume "Auto volume is correct"
            Expect.equal newState.Config.ShowAdvanced showAdvanced "Show advanced is correct"
            Expect.equal newState.Config.RemoveDDOnImport removeDD "Remove DD on import is correct"

        testCase "SetGenerateDD, SetDDPhraseSearchEnabled, SetDDPhraseSearchThreshold, SetApplyImprovements" <| fun _ ->
            let generateDD = not initialState.Config.GenerateDD
            let phraseSearch = not initialState.Config.DDPhraseSearchEnabled
            let applyImprovements = not initialState.Config.ApplyImprovements
            let messages =
                [ SetGenerateDD generateDD
                  SetDDPhraseSearchEnabled phraseSearch
                  SetDDPhraseSearchThreshold 50
                  SetApplyImprovements applyImprovements ]
                |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
                
            Expect.equal newState.Config.GenerateDD generateDD "Generate DD is correct"
            Expect.equal newState.Config.DDPhraseSearchEnabled phraseSearch "DD phrase search enabled is correct"
            Expect.equal newState.Config.DDPhraseSearchThreshold 50 "DD phrase search threshold is correct"
            Expect.equal newState.Config.ApplyImprovements applyImprovements "Apply improvements is correct"

        testCase "SetSaveDebugFiles, SetCustomAppId, SetConvertAudio" <| fun _ ->
            let saveDebug = not initialState.Config.SaveDebugFiles
            let messages =
                [ SetSaveDebugFiles saveDebug
                  SetCustomAppId(Some "test")
                  SetConvertAudio ToWav ]
                |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
                
            Expect.equal newState.Config.SaveDebugFiles saveDebug "Save debug files is correct"
            Expect.equal newState.Config.CustomAppId (Some "test") "Custom app ID is correct"
            Expect.equal newState.Config.ConvertAudio ToWav "Convert audio is correct"

        testCase "AddReleasePlatform" <| fun _ ->
            let state = { initialState with Config = { initialState.Config with ReleasePlatforms = Set.empty } }
            let messages =
                [ AddReleasePlatform PC
                  AddReleasePlatform Mac ]
                |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
                
            Expect.contains newState.Config.ReleasePlatforms PC "Release platforms contains PC"
            Expect.contains newState.Config.ReleasePlatforms Mac "Release platforms contains Mac"

        testCase "RemoveReleasePlatform" <| fun _ ->
            let state = { initialState with Config = { initialState.Config with ReleasePlatforms = Set([ PC; Mac ]) } }
            let messages =
                [ RemoveReleasePlatform PC
                  RemoveReleasePlatform Mac ]
                |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)
                
            Expect.isEmpty newState.Config.ReleasePlatforms "Release platforms is empty"

        testCase "SetTestFolderPath, SetProjectsFolderPath, SetWwiseConsolePath, SetProfilePath" <| fun _ ->
            let messages =
                [ SetTestFolderPath "TestFolder"
                  SetProjectsFolderPath "ProjectFolder"
                  SetWwiseConsolePath "WwiseConsole"
                  SetProfilePath "profile_prfldb" ]
                |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
                
            Expect.equal newState.Config.TestFolderPath "TestFolder" "Test folder path is correct"
            Expect.equal newState.Config.ProjectsFolderPath "ProjectFolder" "Projects folder path is correct"
            Expect.equal newState.Config.WwiseConsolePath (Some "WwiseConsole") "Wwise console path is correct"
            Expect.equal newState.Config.ProfilePath "profile_prfldb" "Profile path is correct"

        testCase "SetOpenFolderAfterReleaseBuild, SetLoadPreviousProject" <| fun _ ->
            let openFolder = not initialState.Config.OpenFolderAfterReleaseBuild
            let loadProject = not initialState.Config.LoadPreviousOpenedProject
            let messages =
                [ SetOpenFolderAfterReleaseBuild openFolder
                  SetLoadPreviousProject loadProject ]
                |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
                
            Expect.equal newState.Config.OpenFolderAfterReleaseBuild openFolder "Open folder after release is correct"
            Expect.equal newState.Config.LoadPreviousOpenedProject loadProject "Load previous opened project is correct"

        testCase "SetDDLevelCountGeneration, SetAutoSave" <| fun _ ->
            let autoSave = not initialState.Config.AutoSave
            let messages =
                [ SetDDLevelCountGeneration LevelCountGeneration.MLModel
                  SetAutoSave autoSave ]
                |> List.map EditConfig

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
                
            Expect.equal newState.Config.DDLevelCountGeneration LevelCountGeneration.MLModel "DD level count generation is correct"
            Expect.equal newState.Config.AutoSave autoSave "Auto-save is correct"
    ]
