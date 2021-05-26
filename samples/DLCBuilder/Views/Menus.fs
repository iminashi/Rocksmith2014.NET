module DLCBuilder.Views.Menus

open Avalonia
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Input
open Avalonia.Layout
open System
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open DLCBuilder

let private keyModifierCtrl =
    if OperatingSystem.IsMacOS() then KeyModifiers.Meta else KeyModifiers.Control

let private separator = MenuItem.create [ MenuItem.header "-" ]

let audio notCalculatingVolume state dispatch =
    let noBuildInProgress = Utils.notBuilding state
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

let file state dispatch =
    let isImporting = state.RunningTasks.Contains PsarcImport

    MenuItem.create [
        MenuItem.header (translate "file")
        MenuItem.viewItems [
            // New project
            MenuItem.create [
                MenuItem.header (translate "newProject")
                MenuItem.inputGesture (KeyGesture(Key.N, keyModifierCtrl))
                MenuItem.onClick (fun _ -> dispatch NewProject)
            ]

            // Open project
            MenuItem.create [
                MenuItem.header (translate "openProject")
                MenuItem.inputGesture (KeyGesture(Key.O, keyModifierCtrl))
                MenuItem.onClick (fun _ -> Dialog.OpenProject |> ShowDialog |> dispatch)
                Button.isEnabled (not <| state.RunningTasks.Contains PsarcImport)
            ]

            // Save project
            MenuItem.create [
                MenuItem.header (translate "saveProject")
                MenuItem.inputGesture (KeyGesture(Key.S, keyModifierCtrl))
                MenuItem.onClick (fun _ -> dispatch ProjectSaveOrSaveAs)
                Button.isEnabled (state.Project <> state.SavedProject)
            ]

            // Save project as
            MenuItem.create [
                MenuItem.header (translate "saveProjectAs")
                MenuItem.inputGesture (KeyGesture(Key.S, keyModifierCtrl ||| KeyModifiers.Alt))
                MenuItem.onClick (fun _ -> dispatch SaveProjectAs)
            ]
   
            separator

            // Configuration
            MenuItem.create [
                MenuItem.header (translate "configuration")
                MenuItem.inputGesture (KeyGesture(Key.G, keyModifierCtrl))
                MenuItem.onClick (fun _ -> ShowOverlay ConfigEditor |> dispatch)
            ]

            separator

            // Import Toolkit template
            MenuItem.create [
                MenuItem.header (translate "toolkitImport")
                MenuItem.isEnabled (not isImporting)
                MenuItem.inputGesture (KeyGesture(Key.T, keyModifierCtrl))
                MenuItem.onClick (fun _ -> Dialog.ToolkitImport |> ShowDialog |> dispatch)
            ]

            // Import PSARC file
            MenuItem.create [
                MenuItem.header (translate "psarcImport")
                MenuItem.isEnabled (not isImporting)
                MenuItem.inputGesture (KeyGesture(Key.A, keyModifierCtrl))
                MenuItem.onClick (fun _ -> Dialog.PsarcImport |> ShowDialog |> dispatch)
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

            separator

            // Exit
            MenuItem.create [
                MenuItem.header (translate "exit")
                match state.CurrentPlatform with
                | Mac ->
                    MenuItem.inputGesture (KeyGesture(Key.Q, KeyModifiers.Meta))
                | PC ->
                    MenuItem.inputGesture (KeyGesture(Key.F4, KeyModifiers.Alt))
                MenuItem.onClick (fun _ ->
                    (Application.Current.ApplicationLifetime :?> IClassicDesktopStyleApplicationLifetime).Shutdown 0)
            ]
        ]
    ]

let tools state dispatch =
    let canBuild = Utils.canBuild state

    // Tools
    MenuItem.create [
        MenuItem.header (translate "tools")
        MenuItem.viewItems [
            // Build Pitch Shifted
            MenuItem.create [
                MenuItem.header (translate "buildPitchShifted")
                MenuItem.isEnabled canBuild
                MenuItem.onClick (fun _ -> ShowOverlay PitchShifter |> dispatch)
            ]

            // Unpack PSARC
            MenuItem.create [
                MenuItem.header (translate "unpackPSARC")
                MenuItem.isEnabled (not (state.RunningTasks |> Set.contains PsarcUnpack))
                MenuItem.onClick (fun _ -> Dialog.PsarcUnpack |> ShowDialog |> dispatch)
            ]

            // Remove DD
            MenuItem.create [
                MenuItem.header (translate "removeDD")
                MenuItem.onClick (fun _ -> Dialog.RemoveDD |> ShowDialog |> dispatch)
            ]

            // Convert Wem to Ogg
            MenuItem.create [
                MenuItem.header (translate "convertWemToOgg")
                MenuItem.onClick (fun _ -> Dialog.WemFiles |> ShowDialog |> dispatch)
            ]
        ]
    ]

let help dispatch =
    // Help
    MenuItem.create [
        MenuItem.header (translate "help")
        MenuItem.viewItems [
            // Check for Updates
            MenuItem.create [
                MenuItem.header (translate "checkForUpdates")
                MenuItem.onClick (fun _ -> CheckForUpdates |> dispatch)
            ]

            separator

            // About
            MenuItem.create [
                MenuItem.header (translate "about")
                MenuItem.onClick (fun _ -> ShowOverlay AboutMessage |> dispatch)
            ]
        ]
    ]

module Context =
    let tone state dispatch =
        ContextMenu.create [
            ContextMenu.isVisible (state.SelectedToneIndex <> -1)
            ContextMenu.viewItems [
                // Duplicate
                MenuItem.create [
                    MenuItem.header (translate "duplicate")
                    MenuItem.onClick (fun _ -> DuplicateTone |> dispatch)
                ]

                // Move Up
                MenuItem.create [
                    MenuItem.header (translate "moveUp")
                    MenuItem.inputGesture (KeyGesture(Key.Up, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Up |> MoveTone |> dispatch)
                ]

                // Move Down
                MenuItem.create [
                    MenuItem.header (translate "moveDown")
                    MenuItem.inputGesture (KeyGesture(Key.Down, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Down |> MoveTone |> dispatch)
                ]

                // Edit
                MenuItem.create [
                    MenuItem.header (translate "edit")
                    MenuItem.onClick (fun _ -> ShowToneEditor |> dispatch)
                ]

                // Export
                MenuItem.create [
                    MenuItem.header (translate "export")
                    MenuItem.onClick (fun _ -> ExportSelectedTone |> dispatch)
                ]

                separator

                // Remove
                MenuItem.create [
                    MenuItem.header (translate "remove")
                    MenuItem.inputGesture (KeyGesture(Key.Delete, KeyModifiers.None))
                    MenuItem.onClick (fun _ -> dispatch DeleteTone)
                ]
            ]
        ]

    let arrangement state dispatch =
        let isInstrumental, hasIds, canApplyTuningFix =
            match List.tryItem state.SelectedArrangementIndex state.Project.Arrangements with
            | Some (Instrumental inst) ->
                true, true, inst.TuningPitch > 220.
            | Some (Vocals _) ->
                false, true, false
            | _ ->
                false, false, false

        ContextMenu.create [
            ContextMenu.isVisible (state.SelectedArrangementIndex <> -1)
            ContextMenu.viewItems [
                // Generate New Arrangement IDs
                MenuItem.create [
                    MenuItem.header (translate "generateNewArrIDs")
                    MenuItem.isEnabled hasIds
                    MenuItem.onClick (fun _ -> GenerateNewIds |> dispatch)
                    ToolTip.tip (translate "generateNewArrIDsToolTip")
                ]

                // Reload Tone Keys
                MenuItem.create [
                    MenuItem.header (translate "reloadToneKeys")
                    MenuItem.isEnabled isInstrumental
                    MenuItem.onClick (fun _ -> UpdateToneInfo |> EditInstrumental |> dispatch)
                    ToolTip.tip (translate "reloadToneKeysTooltip")
                ]

                // Apply Low Tuning Fix
                MenuItem.create [
                    MenuItem.header (translate "applyLowTuningFix")
                    MenuItem.isEnabled canApplyTuningFix
                    MenuItem.onClick (fun _ -> ApplyLowTuningFix |> dispatch)
                ]

                // Move Up
                MenuItem.create [
                    MenuItem.header (translate "moveUp")
                    MenuItem.inputGesture (KeyGesture(Key.Up, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Up |> MoveArrangement |> dispatch)
                ]

                // Move Down
                MenuItem.create [
                    MenuItem.header (translate "moveDown")
                    MenuItem.inputGesture (KeyGesture(Key.Down, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Down |> MoveArrangement |> dispatch)
                ]

                separator

                // Generate New IDs for All Arrangements
                MenuItem.create [
                    MenuItem.header (translate "generateAllIDs")
                    MenuItem.onClick (fun _ -> GenerateAllIds |> dispatch)
                ]

                separator

                // Remove
                MenuItem.create [
                    MenuItem.header (translate "remove")
                    MenuItem.inputGesture (KeyGesture(Key.Delete, KeyModifiers.None))
                    MenuItem.onClick (fun _ -> dispatch DeleteArrangement)
                ]
            ]
        ]
