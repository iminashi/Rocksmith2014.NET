module DLCBuilder.Views.Menus

open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Layout
open System
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open DLCBuilder

let private separator = MenuItem.create [ MenuItem.header "-" ]

let audio notCalculatingVolume noBuildInProgress state dispatch =
    let audioPath = state.Project.AudioFile.Path

    Menu.create [
        Menu.fontSize 16.
        Menu.margin (0., 0., 4., 0.)
        Menu.isVisible (String.notEmpty audioPath && not <| String.endsWith ".wem" audioPath)
        Menu.viewItems [
            MenuItem.create [
                MenuItem.header (TextBlock.create [
                    TextBlock.text "..."
                    TextBlock.verticalAlignment VerticalAlignment.Center
                ])

                MenuItem.viewItems [
                    // Calculate volumes
                    MenuItem.create [
                        MenuItem.header (translate "calculateVolumes")
                        MenuItem.isEnabled (noBuildInProgress && notCalculatingVolume)
                        MenuItem.onClick (fun _ -> dispatch CalculateVolumes)
                    ]

                    // Wem conversion
                    MenuItem.create [
                        MenuItem.header (translate "convert")
                        MenuItem.isEnabled noBuildInProgress
                        MenuItem.onClick (fun _ -> dispatch ConvertToWem)
                        ToolTip.tip (translate "convertMultipleToWemTooltip")
                    ]
                ]
            ]
        ]
    ]

let file state dispatch canBuild =
    Menu.create [
        Menu.fontSize 16.
        Menu.margin (0., 0., 4., 0.)
        Menu.viewItems [
            MenuItem.create [
                MenuItem.isEnabled (not <| state.RunningTasks.Contains PsarcImport)
                MenuItem.header (TextBlock.create [
                    TextBlock.text "..."
                    TextBlock.verticalAlignment VerticalAlignment.Center
                ])
                MenuItem.viewItems [
                    // New project
                    MenuItem.create [
                        MenuItem.header (translate "newProject")
                        MenuItem.inputGesture (KeyGesture(Key.N, KeyModifiers.Control))
                        MenuItem.onClick (fun _ -> dispatch NewProject)
                    ]

                    // Save project as
                    MenuItem.create [
                        MenuItem.header (translate "saveProjectAs")
                        MenuItem.inputGesture (KeyGesture(Key.S, KeyModifiers.Control ||| KeyModifiers.Alt))
                        MenuItem.onClick (fun _ -> dispatch ProjectSaveAs)
                    ]

                    // Build Pitch Shifted
                    MenuItem.create [
                        MenuItem.header (translate "buildPitchShifted")
                        MenuItem.isEnabled canBuild
                        MenuItem.onClick (fun _ -> dispatch ShowPitchShifter)
                    ]
    
                    separator
    
                    // Import Toolkit template
                    MenuItem.create [
                        MenuItem.header (translate "toolkitImport")
                        MenuItem.inputGesture (KeyGesture(Key.T, KeyModifiers.Control))
                        MenuItem.onClick (fun _ ->
                            Msg.OpenFileDialog("selectImportToolkitTemplate", Dialogs.toolkitFilter, ImportToolkitTemplate)
                            |> dispatch)
                    ]
    
                    // Import PSARC file
                    MenuItem.create [
                        MenuItem.header (translate "psarcImport")
                        MenuItem.inputGesture (KeyGesture(Key.A, KeyModifiers.Control))
                        MenuItem.onClick (fun _ ->
                            Msg.OpenFileDialog("selectImportPsarc", Dialogs.psarcFilter, SelectImportPsarcFolder)
                            |> dispatch)
                    ]

                    MenuItem.create [
                        MenuItem.header (translate "tools")
                        MenuItem.viewItems [
                            MenuItem.create [
                                MenuItem.header (translate "unpackPSARC")
                                MenuItem.onClick (fun _ ->
                                    Msg.OpenFileDialog("selectUnpackPsarc", Dialogs.psarcFilter, UnpackPSARC)
                                    |> dispatch)
                            ]
                        ]
                    ]
    
                    separator

                    // Delete test builds
                    MenuItem.create [
                        MenuItem.header (translate "deleteTestBuilds")
                        MenuItem.isEnabled (String.notEmpty state.Config.TestFolderPath && state.OpenProjectFile.IsSome)
                        MenuItem.onClick (fun _ -> dispatch DeleteTestBuilds)
                        ToolTip.tip (translate "deleteTestBuildsTooltip")
                    ]

                    separator

                    // Recent files
                    MenuItem.create [
                        MenuItem.header (translate "recentProjects")

                        MenuItem.viewItems (
                            state.RecentFiles
                            |> List.mapi (fun i fileName ->
                                MenuItem.create [
                                    MenuItem.header ($"_{i + 1} {IO.Path.GetFileName fileName}")
                                    MenuItem.onClick (
                                        (fun _ -> OpenProject fileName |> dispatch),
                                        SubPatchOptions.OnChangeOf state.RecentFiles)
                                ] |> generalize
                            )
                        )
                    ]
                ]
                
            ]
        ]
    ]

module Context =
    let tone state dispatch =
        ContextMenu.create [
            ContextMenu.isVisible (state.SelectedToneIndex <> -1)
            ContextMenu.viewItems [
                MenuItem.create [
                    MenuItem.header (translate "duplicate")
                    MenuItem.onClick (fun _ -> DuplicateTone |> dispatch)
                ]
    
                MenuItem.create [
                    MenuItem.header (translate "moveUp")
                    MenuItem.inputGesture (KeyGesture(Key.Up, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Up |> MoveTone |> dispatch)
                ]
    
                MenuItem.create [
                    MenuItem.header (translate "moveDown")
                    MenuItem.inputGesture (KeyGesture(Key.Down, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Down |> MoveTone |> dispatch)
                ]
    
                MenuItem.create [
                    MenuItem.header (translate "edit")
                    MenuItem.onClick (fun _ -> ShowToneEditor |> dispatch)
                ]
    
                MenuItem.create [
                    MenuItem.header (translate "export")
                    MenuItem.onClick (fun _ -> ExportSelectedTone |> dispatch)
                ]
    
                separator
    
                MenuItem.create [
                    MenuItem.header (translate "remove")
                    MenuItem.inputGesture (KeyGesture(Key.Delete, KeyModifiers.None))
                    MenuItem.onClick (fun _ -> dispatch DeleteTone)
                ]
            ]
        ]

    let arrangement state dispatch =
        let isInstrumental, hasIds =
            match state.SelectedArrangementIndex with
            | -1 -> false, false
            | index ->
                match state.Project.Arrangements.[index] with
                | Instrumental _ -> true, true
                | Vocals _ -> false, true
                | _ -> false, false
    
        ContextMenu.create [
            ContextMenu.isVisible (state.SelectedArrangementIndex <> -1)
            ContextMenu.viewItems [
                MenuItem.create [
                    MenuItem.header (translate "generateNewArrIDs")
                    MenuItem.isEnabled hasIds
                    MenuItem.onClick (fun _ -> GenerateNewIds |> dispatch)
                    ToolTip.tip (translate "generateNewArrIDsToolTip")
                ]
    
                MenuItem.create [
                    MenuItem.header (translate "reloadToneKeys")
                    MenuItem.isEnabled isInstrumental
                    MenuItem.onClick (fun _ -> UpdateToneInfo |> EditInstrumental |> dispatch)
                    ToolTip.tip (translate "reloadToneKeysTooltip")
                ]
    
                MenuItem.create [
                    MenuItem.header (translate "moveUp")
                    MenuItem.inputGesture (KeyGesture(Key.Up, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Up |> MoveArrangement |> dispatch)
                ]
    
                MenuItem.create [
                    MenuItem.header (translate "moveDown")
                    MenuItem.inputGesture (KeyGesture(Key.Down, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Down |> MoveArrangement |> dispatch)
                ]
    
                separator
    
                MenuItem.create [
                    MenuItem.header (translate "generateAllIDs")
                    MenuItem.onClick (fun _ -> GenerateAllIds |> dispatch)
                ]
    
                separator
    
                MenuItem.create [
                    MenuItem.header (translate "remove")
                    MenuItem.inputGesture (KeyGesture(Key.Delete, KeyModifiers.None))
                    MenuItem.onClick (fun _ -> dispatch DeleteArrangement)
                ]
            ]
        ]
