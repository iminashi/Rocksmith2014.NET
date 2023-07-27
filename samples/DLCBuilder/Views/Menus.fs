module DLCBuilder.Views.Menus

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open System
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open DLCBuilder

let private keyModifierCtrl =
    PlatformSpecific.Value(mac=KeyModifiers.Meta, windows=KeyModifiers.Control, linux=KeyModifiers.Control)

let private separator = MenuItem.create [ MenuItem.header "-" ]

let private headerWithLine (locString: string) =
    DockPanel.create [
        DockPanel.children [
            TextBlock.create [
                DockPanel.dock Dock.Left
                TextBlock.fontSize 14.
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate locString)
                TextBlock.foreground Brushes.LightGray
            ]

            Rectangle.create [
                Rectangle.height 1.
                Rectangle.fill Brushes.Gray
                Rectangle.margin (8., 0.)
            ]
        ]
    ]

let buildOptions state dispatch =
    Menu.create [
        Grid.column 3
        Menu.fontSize 16.
        //Menu.margin (0., 0., 4., 0.)
        Menu.viewItems [
            MenuItem.create [
                MenuItem.header (
                    PathIcon.create [
                        PathIcon.data Media.Icons.ellipsis
                    ]
                )

                MenuItem.viewItems [
                    MenuItem.create [
                        MenuItem.header (headerWithLine "Common")
                        MenuItem.isHitTestVisible false
                    ]

                    MenuItem.create [
                        MenuItem.header (
                            CheckBox.create [
                                CheckBox.content (translate "ApplyImprovements")
                                CheckBox.isChecked state.Config.ApplyImprovements
                                CheckBox.onChecked (fun _ -> true |> SetApplyImprovements |> EditConfig |> dispatch)
                                CheckBox.onUnchecked (fun _ -> false |> SetApplyImprovements |> EditConfig |> dispatch)
                            ]
                        )
                    ]

                    MenuItem.create [
                        MenuItem.header (
                            CheckBox.create [
                                CheckBox.content (translate "ForceAutomaticPhraseCreation")
                                CheckBox.isChecked state.Config.ForcePhraseCreation
                                CheckBox.onChecked (fun _ -> true |> SetForcePhraseCreation |> EditConfig |> dispatch)
                                CheckBox.onUnchecked (fun _ -> false |> SetForcePhraseCreation |> EditConfig |> dispatch)
                            ]
                        )
                    ]

                    MenuItem.create [
                        MenuItem.header (headerWithLine "Release")
                        MenuItem.isHitTestVisible false
                    ]

                    MenuItem.create [
                        MenuItem.header (
                            CheckBox.create [
                                CheckBox.content (translate "ValidateBeforeBuild")
                                CheckBox.isChecked state.Config.ValidateBeforeReleaseBuild
                                CheckBox.onChecked (fun _ -> true |> SetValidateBeforeReleaseBuild |> EditConfig |> dispatch)
                                CheckBox.onUnchecked (fun _ -> false |> SetValidateBeforeReleaseBuild |> EditConfig |> dispatch)
                            ]
                        )
                    ]

                    MenuItem.create [
                        MenuItem.header (headerWithLine "Test")
                        MenuItem.isHitTestVisible false
                    ]

                    MenuItem.create [
                        MenuItem.header (
                            CheckBox.create [
                                CheckBox.content (translate "ComparePhraseLevelsOnTestBuild")
                                CheckBox.isChecked state.Config.ComparePhraseLevelsOnTestBuild
                                CheckBox.onChecked (fun _ -> true |> SetComparePhraseLevelsOnTestBuild |> EditConfig |> dispatch)
                                CheckBox.onUnchecked (fun _ -> false |> SetComparePhraseLevelsOnTestBuild |> EditConfig |> dispatch)
                            ]
                        )
                    ]

                    MenuItem.create [
                        MenuItem.header (
                            CheckBox.create [
                                CheckBox.content (translate "GenerateDDLevels")
                                CheckBox.isChecked state.Config.GenerateDD
                                CheckBox.onChecked (fun _ -> true |> SetGenerateDD |> EditConfig |> dispatch)
                                CheckBox.onUnchecked (fun _ -> false |> SetGenerateDD |> EditConfig |> dispatch)
                            ]
                        )
                    ]

                    MenuItem.create [
                        MenuItem.header (
                            CheckBox.create [
                                CheckBox.content (translate "SaveDebugFiles")
                                CheckBox.isChecked state.Config.SaveDebugFiles
                                CheckBox.onChecked (fun _ -> true |> SetSaveDebugFiles |> EditConfig |> dispatch)
                                CheckBox.onUnchecked (fun _ -> false |> SetSaveDebugFiles |> EditConfig |> dispatch)
                            ]
                        )
                    ]
                ]
            ]
        ]
    ]

let audio notCalculatingVolume state dispatch =
    let noBuildInProgress = StateUtils.notBuilding state
    let audioPath = state.Project.AudioFile.Path

    Menu.create [
        Menu.fontSize 16.
        Menu.isVisible (String.notEmpty audioPath)
        Menu.viewItems [
            MenuItem.create [
                MenuItem.header (
                    PathIcon.create [
                        PathIcon.data Media.Icons.ellipsis
                    ]
                )

                MenuItem.viewItems [
                    // Calculate volumes
                    MenuItem.create [
                        MenuItem.header (translate "CalculateVolumes")
                        MenuItem.isEnabled (noBuildInProgress && notCalculatingVolume)
                        MenuItem.onClick (fun _ -> dispatch CalculateVolumes)
                    ]

                    // Wem conversion
                    MenuItem.create [
                        MenuItem.header (translate "Convert")
                        MenuItem.isEnabled (noBuildInProgress && not <| String.endsWith ".wem" audioPath)
                        MenuItem.onClick (fun _ -> dispatch ConvertToWem)
                        ToolTip.tip (translate "ConvertMultipleToWemToolTip")
                    ]
                ]
            ]
        ]
    ]

let file state dispatch =
    let isImporting = state.RunningTasks.Contains PsarcImport

    MenuItem.create [
        MenuItem.header (translate "FileMenuItem")
        MenuItem.viewItems [
            // New project
            MenuItem.create [
                MenuItem.header (translate "NewProjectMenuItem")
                MenuItem.inputGesture (KeyGesture(Key.N, keyModifierCtrl))
                MenuItem.onClick (fun _ -> dispatch NewProject)
            ]

            // Open project
            MenuItem.create [
                MenuItem.header (translate "OpenProjectMenuItem")
                MenuItem.inputGesture (KeyGesture(Key.O, keyModifierCtrl))
                MenuItem.onClick (fun _ -> Dialog.OpenProject |> ShowDialog |> dispatch)
                Button.isEnabled (not <| state.RunningTasks.Contains PsarcImport)
            ]

            // Save project
            MenuItem.create [
                MenuItem.header (translate "SaveProjectMenuItem")
                MenuItem.inputGesture (KeyGesture(Key.S, keyModifierCtrl))
                MenuItem.onClick (fun _ -> dispatch ProjectSaveOrSaveAs)
                Button.isEnabled (state.Project <> state.SavedProject)
            ]

            // Save project as
            MenuItem.create [
                MenuItem.header (translate "SaveProjectAsMenuItem")
                MenuItem.inputGesture (KeyGesture(Key.S, keyModifierCtrl ||| KeyModifiers.Alt))
                MenuItem.onClick (fun _ -> dispatch SaveProjectAs)
            ]

            separator

            // Configuration
            MenuItem.create [
                MenuItem.header (translate "ConfigurationMenuItem")
                MenuItem.inputGesture (KeyGesture(Key.G, keyModifierCtrl))
                MenuItem.onClick (fun _ -> ShowOverlay (ConfigEditor None) |> dispatch)
            ]

            separator

            // Import Toolkit template
            MenuItem.create [
                MenuItem.header (translate "ImportToolkitTemplateMenuItem")
                MenuItem.isEnabled (not isImporting)
                MenuItem.inputGesture (KeyGesture(Key.I, keyModifierCtrl))
                MenuItem.onClick (fun _ -> Dialog.ToolkitImport |> ShowDialog |> dispatch)
            ]

            // Import PSARC file
            MenuItem.create [
                MenuItem.header (translate "ImportPSARCFileMenuItem")
                MenuItem.isEnabled (not isImporting)
                MenuItem.inputGesture (KeyGesture(Key.A, keyModifierCtrl))
                MenuItem.onClick (fun _ -> Dialog.PsarcImport |> ShowDialog |> dispatch)
            ]

            // Import PSARC file for quick editing
            MenuItem.create [
                MenuItem.header (translate "ImportPSARCQuickFileMenuItem")
                MenuItem.isEnabled (not isImporting)
                MenuItem.inputGesture (KeyGesture(Key.E, keyModifierCtrl))
                MenuItem.onClick (fun _ -> Dialog.PsarcImportQuick |> ShowDialog |> dispatch)
            ]

            separator

            // Recent projects
            MenuItem.create [
                MenuItem.header (translate "RecentProjectsMenuItem")
                MenuItem.isEnabled (not state.RecentFiles.IsEmpty)

                MenuItem.viewItems (
                    state.RecentFiles
                    |> List.mapi (fun i fileName ->
                        MenuItem.create [
                            MenuItem.header $"_{i + 1} {IO.Path.GetFileName(fileName)}"
                            ToolTip.tip fileName
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
                MenuItem.header (translate "ExitMenuItem")
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

let build state dispatch =
    let canBuild = StateUtils.canBuild state

    // Build
    MenuItem.create [
        MenuItem.header (translate "BuildMenuItem")
        MenuItem.viewItems [
            // Build Test
            MenuItem.create [
                MenuItem.header (translate "TestMenuItem")
                MenuItem.isEnabled canBuild
                MenuItem.onClick (fun _ -> Build Test |> dispatch)
                MenuItem.inputGesture (KeyGesture(Key.B, keyModifierCtrl))
            ]

            // Build Release
            MenuItem.create [
                MenuItem.header (translate "ReleaseMenuItem")
                MenuItem.isEnabled canBuild
                MenuItem.onClick (fun _ -> Build Release |> dispatch)
                MenuItem.inputGesture (KeyGesture(Key.R, keyModifierCtrl))
            ]

            // Build Pitch Shifted
            MenuItem.create [
                MenuItem.header (translate "PitchShiftedMenuItem")
                MenuItem.isEnabled canBuild
                MenuItem.onClick (fun _ -> ShowOverlay PitchShifter |> dispatch)
            ]
        ]
    ]

let tools state dispatch =
    // Tools
    MenuItem.create [
        MenuItem.header (translate "ToolsMenuItem")
        MenuItem.viewItems [
            // Unpack PSARC
            MenuItem.create [
                MenuItem.header (translate "UnpackPSARCMenuItem")
                MenuItem.isEnabled (not (state.RunningTasks |> Set.contains PsarcUnpack))
                MenuItem.onClick (fun _ -> Dialog.PsarcUnpack |> ShowDialog |> dispatch)
            ]

            // Pack Directory into PSARC
            MenuItem.create [
                MenuItem.header (translate "PackDirectoryIntoPSARCMenuItem")
                MenuItem.onClick (fun _ -> Dialog.PsarcPackDirectory |> ShowDialog |> dispatch)
            ]

            separator

            // Convert Wem to Ogg
            MenuItem.create [
                MenuItem.header (translate "ConvertWemToOggMenuItem")
                MenuItem.onClick (fun _ -> Dialog.WemFiles |> ShowDialog |> dispatch)
            ]

            // Convert Audio to Wem
            MenuItem.create [
                MenuItem.header (translate "ConvertAudioToWemMenuItem")
                MenuItem.onClick (fun _ -> Dialog.AudioFileConversion |> ShowDialog |> dispatch)
            ]

            separator

            // Remove DD
            MenuItem.create [
                MenuItem.header (translate "RemoveDDMenuItem")
                MenuItem.onClick (fun _ -> Dialog.RemoveDD |> ShowDialog |> dispatch)
            ]

            separator

            // Inject Tones into Profile
            MenuItem.create [
                MenuItem.header (translate "InjectTonesIntoProfileMenuItem")
                MenuItem.onClick (fun _ -> Dialog.ToneInject |> ShowDialog |> dispatch)
                MenuItem.isEnabled (String.notEmpty state.Config.ProfilePath)
                ToolTip.tip (translate "InjectTonesIntoProfileToolTip")
            ]

            // Profile Cleaner
            MenuItem.create [
                MenuItem.header (translate "ProfileCleanerMenuItem")
                MenuItem.onClick (fun _ -> ShowOverlay ProfileCleaner |> dispatch)
            ]
        ]
    ]

let help dispatch =
    let crashLogExists = IO.File.Exists Configuration.crashLogPath

    // Help
    MenuItem.create [
        MenuItem.header (translate "HelpMenuItem")
        MenuItem.viewItems [
            // View ReadMe
            MenuItem.create [
                MenuItem.header (translate "ViewReadMeMenuItem")
                MenuItem.onClick (fun _ ->
                    IO.Path.Combine(AppContext.BaseDirectory, "ReadMe.html")
                    |> OpenWithShell
                    |> dispatch
                )
            ]

            // View Release Notes
            MenuItem.create [
                MenuItem.header (translate "ViewReleaseNotesMenuItem")
                MenuItem.onClick (fun _ ->
                    "https://github.com/iminashi/Rocksmith2014.NET/blob/main/samples/DLCBuilder/RELEASE_NOTES.md"
                    |> OpenWithShell
                    |> dispatch
                )
            ]

            // Check for Updates
            MenuItem.create [
                MenuItem.header (translate "CheckForUpdatesMenuItem")
                MenuItem.onClick (fun _ -> CheckForUpdates |> dispatch)
            ]

            MenuItem.create [
                MenuItem.header "-"
                MenuItem.isVisible crashLogExists
            ]

            // View Crash Log
            MenuItem.create [
                MenuItem.header (translate "ViewCrashLogMenuItem")
                MenuItem.isVisible crashLogExists
                MenuItem.onClick (fun _ ->
                    Configuration.crashLogPath
                    |> OpenWithShell
                    |> dispatch)
            ]

            separator

            // About
            MenuItem.create [
                MenuItem.header (translate "AboutMenuItem")
                MenuItem.onClick (fun _ -> ShowOverlay AboutMessage |> dispatch)
            ]
        ]
    ]

module Context =
    let tone state dispatch =
        ContextMenu.create [
            ContextMenu.isVisible (state.SelectedToneIndex <> -1)
            ContextMenu.viewItems [
                // Add to collection
                MenuItem.create [
                    MenuItem.header (translate "AddToCollectionMenuItem")
                    MenuItem.onClick (fun _ -> AddToneToCollection |> dispatch)
                ]

                separator

                // Move Up
                MenuItem.create [
                    MenuItem.header (translate "MoveUpMenuItem")
                    MenuItem.inputGesture (KeyGesture(Key.Up, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Up |> MoveTone |> dispatch)
                ]

                // Move Down
                MenuItem.create [
                    MenuItem.header (translate "MoveDownMenuItem")
                    MenuItem.inputGesture (KeyGesture(Key.Down, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Down |> MoveTone |> dispatch)
                ]

                separator

                // Duplicate
                MenuItem.create [
                    MenuItem.header (translate "DuplicateMenuItem")
                    MenuItem.onClick (fun _ -> DuplicateTone |> dispatch)
                ]

                // Edit
                MenuItem.create [
                    MenuItem.header (translate "EditMenuItem")
                    MenuItem.onClick (fun _ -> ShowToneEditor |> dispatch)
                ]

                // Export
                MenuItem.create [
                    MenuItem.header (translate "ExportMenuItem")
                    MenuItem.onClick (fun _ -> ExportSelectedTone |> dispatch)
                ]

                separator

                // Remove
                MenuItem.create [
                    MenuItem.header (translate "RemoveMenuItem")
                    MenuItem.inputGesture (KeyGesture(Key.Delete, KeyModifiers.None))
                    MenuItem.onClick (fun _ -> dispatch DeleteSelectedTone)
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
                    MenuItem.header (translate "GenerateNewArrIDsMenuItem")
                    MenuItem.isEnabled hasIds
                    MenuItem.onClick (fun _ -> GenerateNewIds |> dispatch)
                    ToolTip.tip (translate "GenerateNewArrIDsToolTip")
                ]

                // Reload Tone Keys
                MenuItem.create [
                    MenuItem.header (translate "ReloadToneKeysMenuItem")
                    MenuItem.isEnabled isInstrumental
                    MenuItem.onClick (fun _ -> UpdateToneInfo |> EditInstrumental |> dispatch)
                    ToolTip.tip (translate "ReloadToneKeysToolTip")
                ]

                // Apply Low Tuning Fix
                MenuItem.create [
                    MenuItem.header (translate "ApplyLowTuningFixMenuItem")
                    MenuItem.isEnabled canApplyTuningFix
                    MenuItem.onClick (fun _ -> ApplyLowTuningFix |> dispatch)
                ]

                separator

                // Move Up
                MenuItem.create [
                    MenuItem.header (translate "MoveUpMenuItem")
                    MenuItem.inputGesture (KeyGesture(Key.Up, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Up |> MoveArrangement |> dispatch)
                ]

                // Move Down
                MenuItem.create [
                    MenuItem.header (translate "MoveDownMenuItem")
                    MenuItem.inputGesture (KeyGesture(Key.Down, KeyModifiers.Alt))
                    MenuItem.onClick (fun _ -> Down |> MoveArrangement |> dispatch)
                ]

                separator

                // Generate New IDs for All Arrangements
                MenuItem.create [
                    MenuItem.header (translate "GenerateNewIDsAllMenuItem")
                    MenuItem.onClick (fun _ -> GenerateAllIds |> dispatch)
                ]

                separator

                // Remove
                MenuItem.create [
                    MenuItem.header (translate "RemoveMenuItem")
                    MenuItem.inputGesture (KeyGesture(Key.Delete, KeyModifiers.None))
                    MenuItem.onClick (fun _ -> dispatch DeleteSelectedArrangement)
                ]
            ]
        ]

let private addToneItems dispatch : IView list =
    [
        // Add a new "empty" tone
        MenuItem.create [
            MenuItem.header (translate "NewToneMenuItem")
            MenuItem.onClick (fun _ -> dispatch AddNewTone)
        ]

        separator

        // Import from profile
        MenuItem.create [
            MenuItem.header (translate "FromProfileMenuItem")
            MenuItem.onClick (fun _ -> dispatch ImportProfileTones)
            MenuItem.inputGesture (KeyGesture(Key.P, keyModifierCtrl))
            ToolTip.tip (translate "ProfileImportToolTip")
        ]

        // Add from collection
        MenuItem.create [
            MenuItem.header (translate "FromCollectionMenuItem")
            MenuItem.onClick (fun _ -> ShowToneCollection |> dispatch)
            MenuItem.inputGesture (KeyGesture(Key.T, keyModifierCtrl))
        ]

        separator

        // Import from file
        MenuItem.create [
            MenuItem.header (translate "ImportToneFromFileMenuItem")
            MenuItem.onClick (fun _ -> Dialog.ToneImport |> ShowDialog |> dispatch)
        ]
    ]

let addTone dispatch =
    Menu.create [
        Grid.column 1
        Menu.viewItems [
            MenuItem.create [
                MenuItem.header (
                    PathIcon.create [
                        PathIcon.data Media.Icons.plus
                        PathIcon.width 16.
                        PathIcon.height 16.
                    ])
                MenuItem.viewItems (addToneItems dispatch)
            ]
        ]
    ]

let project state dispatch =
    MenuItem.create [
        MenuItem.header (translate "ProjectMenuItem")

        MenuItem.viewItems [
            // Add Arrangement
            MenuItem.create [
                MenuItem.header (translate "AddArrangementMenuItem")
                MenuItem.onClick (fun _ -> Dialog.AddArrangements |> ShowDialog |> dispatch)
                MenuItem.inputGesture (KeyGesture(Key.OemPlus, keyModifierCtrl))
            ]

            // Validate Arrangements
            MenuItem.create [
                MenuItem.header (translate "ValidateArrangementsMenuItem")
                MenuItem.onClick (fun _ -> dispatch CheckArrangements)
                MenuItem.inputGesture (KeyGesture(Key.V, keyModifierCtrl))
                MenuItem.isEnabled (StateUtils.canRunValidation state)
            ]

            // Generate New IDs for All Arrangements
            MenuItem.create [
                MenuItem.header (translate "GenerateNewIDsAllMenuItem")
                MenuItem.onClick (fun _ -> GenerateAllIds |> dispatch)
                MenuItem.isEnabled (state.Project.Arrangements.Length > 0)
            ]

            separator

            // Add Tone Menu
            MenuItem.create [
                MenuItem.header (translate "AddToneMenuItem")
                MenuItem.viewItems (addToneItems dispatch)
            ]

            separator

            MenuItem.create [
                MenuItem.header (translate "CreateJapaneseLyricsMenuItem")
                MenuItem.onClick (fun _ -> ShowJapaneseLyricsCreator |> dispatch)
                MenuItem.inputGesture (KeyGesture(Key.J, keyModifierCtrl))
                MenuItem.isEnabled (state.Project.Arrangements |> List.exists (function Vocals v -> not v.Japanese | _ -> false))
            ]

            MenuItem.create [
                MenuItem.header (translate "OpenProjectFolderMenuItem")
                MenuItem.onClick (fun _ -> OpenProjectFolder |> dispatch)
                MenuItem.isEnabled state.OpenProjectFile.IsSome
            ]

            separator

            // Delete test builds
            MenuItem.create [
                MenuItem.header (translate "DeleteTestBuildsMenuItem")
                MenuItem.isEnabled (String.notEmpty state.Config.TestFolderPath && String.notEmpty state.Project.DLCKey)
                MenuItem.onClick (fun _ -> dispatch DeleteTestBuilds)
                ToolTip.tip (translate "DeleteTestBuildsToolTip")
            ]
        ]
    ]
