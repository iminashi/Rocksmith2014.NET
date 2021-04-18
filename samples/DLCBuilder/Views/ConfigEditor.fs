module DLCBuilder.Views.ConfigEditor

open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.Common
open DLCBuilder
open System.IO
open System

let private tryFindWwiseExecutable basePath =
    let ext = if OperatingSystem.IsMacOS() then "sh" else "exe"
    Directory.EnumerateFiles(basePath, $"WwiseConsole.{ext}", SearchOption.AllDirectories)
    |> Seq.tryHead

let private generalConfig state dispatch =
    vStack [
        Grid.create [
            Grid.columnDefinitions "auto,5,*"
            Grid.rowDefinitions "auto,auto,auto,auto,auto,auto"
            Grid.children [
                // Language
                locText "language" [
                    TextBlock.verticalAlignment VerticalAlignment.Center
                ]
                ComboBox.create [
                    Grid.column 2
                    ComboBox.verticalAlignment VerticalAlignment.Center
                    ComboBox.dataItems Locales.All
                    ComboBox.selectedItem state.Config.Locale
                    ComboBox.onSelectedItemChanged (function
                        | :? Locale as l -> l |> ChangeLocale |> dispatch
                        | _ -> ())
                ]

                // Charter Name
                locText "charterName" [
                    Grid.row 1
                    TextBlock.verticalAlignment VerticalAlignment.Center
                ]
                TextBox.create [
                    Grid.column 2
                    Grid.row 1
                    TextBox.margin (0., 4.)
                    TextBox.text state.Config.CharterName
                    TextBox.onTextChanged (SetCharterName >> EditConfig >> dispatch)
                ]

                // Profile Path
                locText "profilePath" [
                    Grid.row 2
                    TextBlock.verticalAlignment VerticalAlignment.Center
                ]
                DockPanel.create [
                    Grid.column 2
                    Grid.row 2
                    DockPanel.children [
                        Button.create [
                            DockPanel.dock Dock.Right
                            Button.margin (0., 4.)
                            Button.content "..."
                            Button.onClick (fun _ -> Dialog.ProfileFile |> ShowDialog |> dispatch)
                        ]
                        TextBox.create [
                            TextBox.margin (0., 4.)
                            TextBox.text state.Config.ProfilePath
                            TextBox.onTextChanged (SetProfilePath >> EditConfig >> dispatch)
                        ]
                    ]
                ]

                // Test Folder
                locText "testFolder" [
                    Grid.row 3
                    TextBlock.verticalAlignment VerticalAlignment.Center
                ]
                DockPanel.create [
                    Grid.column 2
                    Grid.row 3
                    DockPanel.children [
                        Button.create [
                            DockPanel.dock Dock.Right
                            Button.margin (0., 4.)
                            Button.content "..."
                            Button.onClick (fun _ -> Dialog.TestFolder |> ShowDialog |> dispatch)
                        ]
                        TextBox.create [
                            TextBox.margin (0., 4.)
                            TextBox.text state.Config.TestFolderPath
                            TextBox.watermark (translate "testFolderPlaceholder")
                            TextBox.onTextChanged (SetTestFolderPath >> EditConfig >> dispatch)
                        ]
                    ]
                ]

                // Projects Folder
                locText "projectsFolder" [
                    Grid.row 4
                    TextBlock.verticalAlignment VerticalAlignment.Center
                ]
                DockPanel.create [
                    Grid.column 2
                    Grid.row 4
                    DockPanel.children [
                        Button.create [
                            DockPanel.dock Dock.Right
                            Button.margin (0., 4.)
                            Button.content "..."
                            Button.onClick (fun _ -> Dialog.ProjectFolder |> ShowDialog |> dispatch)
                        ]
                        TextBox.create [
                            TextBox.margin (0., 4.)
                            TextBox.text state.Config.ProjectsFolderPath
                            TextBox.onTextChanged (SetProjectsFolderPath >> EditConfig >> dispatch)
                        ]
                    ]
                ]

                // WWise Console Path
                locText "wwiseConsolePath" [
                    Grid.row 5
                    TextBlock.verticalAlignment VerticalAlignment.Center
                ]
                DockPanel.create [
                    Grid.column 2
                    Grid.row 5
                    DockPanel.children [
                        Button.create [
                            DockPanel.dock Dock.Right
                            Button.margin (0., 4.)
                            Button.content "..."
                            Button.onClick (fun _ -> Dialog.WwiseConsole |> ShowDialog |> dispatch)
                        ]
                        TextBox.create [
                            TextBox.margin (0., 4.)
                            TextBox.text (Option.toObj state.Config.WwiseConsolePath)
                            TextBox.watermark (translate "wwiseConsolePathPlaceholder")
                            TextBox.onTextChanged (SetWwiseConsolePath >> EditConfig >> dispatch)
                            TextBox.onLostFocus (fun e ->
                                let t = e.Source :?> TextBox
                                match t.Text with
                                | path when Directory.Exists path ->
                                    tryFindWwiseExecutable path
                                    |> Option.iter (SetWwiseConsolePath >> EditConfig >> dispatch)
                                | _ -> ()
                            )
                            ToolTip.tip (translate "wwiseConsolePathTooltip")
                        ]
                    ]
                ]
            ]
        ]

        // Calculate Volumes Automatically
        CheckBox.create [
            CheckBox.content (translate "calculateVolumesAutomatically")
            CheckBox.isChecked state.Config.AutoVolume
            CheckBox.onChecked (fun _ -> true |> SetAutoVolume |> EditConfig |> dispatch)
            CheckBox.onUnchecked (fun _ -> false |> SetAutoVolume |> EditConfig |> dispatch)
        ]

        // Load Previously Opened Project Automatically
        CheckBox.create [
            CheckBox.content (translate "loadPreviousProjectAutomatically")
            CheckBox.isChecked state.Config.LoadPreviousOpenedProject
            CheckBox.onChecked (fun _ -> true |> SetLoadPreviousProject |> EditConfig |> dispatch)
            CheckBox.onUnchecked (fun _ -> false |> SetLoadPreviousProject |> EditConfig |> dispatch)
        ]

        // Auto Save
        CheckBox.create [
            CheckBox.content (translate "autoSave")
            CheckBox.isChecked state.Config.AutoSave
            CheckBox.onChecked (fun _ -> true |> SetAutoSave |> EditConfig |> dispatch)
            CheckBox.onUnchecked (fun _ -> false |> SetAutoSave |> EditConfig |> dispatch)
        ]

        // Show Advanced Features
        CheckBox.create [
            CheckBox.content (translate "showAdvancedFeatures")
            CheckBox.isChecked state.Config.ShowAdvanced
            CheckBox.onChecked (fun _ -> true |> SetShowAdvanced |> EditConfig |> dispatch)
            CheckBox.onUnchecked (fun _ -> false |> SetShowAdvanced |> EditConfig |> dispatch)
        ]
    ]

let private importConfig state dispatch =
    let localize conv =
        match conv with
        | NoConversion -> "noConversion"
        | ToOgg -> "toOggFile"
        | ToWav -> "toWavFile"
        |> translate

    vStack [
        // Header
        locText "psarcImportHeader" [
            TextBlock.fontSize 16.
            TextBlock.margin (4., 4., 0., 0.)
        ]
        Border.create [
            Border.borderThickness 1.
            Border.borderBrush Brushes.Gray
            Border.cornerRadius 4.
            Border.padding 6.
            Border.child (
                vStack [
                    // Convert Audio Options
                    locText "convertWemOnImport" [
                        TextBlock.margin (0., 0., 0., 4.)
                    ]
                    yield! [ NoConversion; ToOgg; ToWav ]
                    |> List.map(fun conv ->
                        RadioButton.create [
                            RadioButton.margin (0., 2.)
                            RadioButton.isChecked (state.Config.ConvertAudio = conv)
                            RadioButton.content (localize conv)
                            RadioButton.onChecked (fun _ -> conv |> SetConvertAudio |> EditConfig |> dispatch)
                        ] |> generalize)

                    // Remove DD Levels
                    CheckBox.create [
                        CheckBox.content (translate "removeDDLevels")
                        CheckBox.isChecked state.Config.RemoveDDOnImport
                        CheckBox.onChecked (fun _ -> true |> SetRemoveDDOnImport |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> SetRemoveDDOnImport |> EditConfig |> dispatch)
                    ]
                ]
            )
        ]
    ]

let private ddConfig state dispatch =
    vStack [
        // Find Similar Phrases
        CheckBox.create [
            CheckBox.margin (0., 2.)
            CheckBox.verticalAlignment VerticalAlignment.Center
            CheckBox.content (translate "findSimilarPhrases")
            CheckBox.isChecked state.Config.DDPhraseSearchEnabled
            CheckBox.onChecked (fun _ -> true |> SetDDPhraseSearchEnabled |> EditConfig |> dispatch)
            CheckBox.onUnchecked (fun _ -> false |> SetDDPhraseSearchEnabled |> EditConfig |> dispatch)
        ]

        // Similarity Threshold
        locText "similarityThreshold" [
            TextBlock.verticalAlignment VerticalAlignment.Center
            TextBlock.isEnabled state.Config.DDPhraseSearchEnabled
        ]
        hStack [
            NumericUpDown.create [
                NumericUpDown.margin (0., 2., 2., 2.)
                NumericUpDown.width 140.
                NumericUpDown.value (float state.Config.DDPhraseSearchThreshold)
                NumericUpDown.isEnabled state.Config.DDPhraseSearchEnabled
                NumericUpDown.onValueChanged (int >> SetDDPhraseSearchThreshold >> EditConfig >> dispatch)
                NumericUpDown.minimum 0.
                NumericUpDown.maximum 100.
                NumericUpDown.formatString "F0"
            ]

            TextBlock.create [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.fontSize 16.
                TextBlock.text "%"
            ]
        ]
    ]

let private buildConfig state dispatch =
    vStack [
        // Apply Improvements
        CheckBox.create [
            CheckBox.verticalAlignment VerticalAlignment.Center
            CheckBox.content (translate "applyImprovements")
            CheckBox.isChecked state.Config.ApplyImprovements
            CheckBox.onChecked (fun _ -> true |> SetApplyImprovements |> EditConfig |> dispatch)
            CheckBox.onUnchecked (fun _ -> false |> SetApplyImprovements |> EditConfig |> dispatch)
        ]

        // Release Build Options
        locText "release" [
            TextBlock.fontSize 16.
            TextBlock.margin (4., 4., 0., 0.)
        ]
        Border.create [
            Border.borderThickness 1.
            Border.borderBrush Brushes.Gray
            Border.cornerRadius 4.
            Border.padding 6.
            Border.child (
                vStack [
                    hStack [
                        // Release Platforms
                        locText "releasePlatforms" [
                            TextBlock.margin (0., 0., 10., 0.)
                            TextBlock.verticalAlignment VerticalAlignment.Center
                        ]
                        CheckBox.create [
                            CheckBox.margin 2.
                            CheckBox.minWidth 0.
                            CheckBox.content "PC"
                            CheckBox.isEnabled (state.Config.ReleasePlatforms |> Set.contains Mac)
                            CheckBox.isChecked (state.Config.ReleasePlatforms |> Set.contains PC)
                            CheckBox.onChecked (fun _ -> PC |> AddReleasePlatform |> EditConfig |> dispatch)
                            CheckBox.onUnchecked (fun _ -> PC |> RemoveReleasePlatform |> EditConfig |> dispatch)
                        ]
                        CheckBox.create [
                            CheckBox.margin 2.
                            CheckBox.minWidth 0.
                            CheckBox.content "Mac"
                            CheckBox.isEnabled (state.Config.ReleasePlatforms |> Set.contains PC)
                            CheckBox.isChecked (state.Config.ReleasePlatforms |> Set.contains Mac)
                            CheckBox.onChecked (fun _ -> Mac |> AddReleasePlatform |> EditConfig |> dispatch)
                            CheckBox.onUnchecked (fun _ -> Mac |> RemoveReleasePlatform |> EditConfig |> dispatch)
                        ]
                    ]

                    // Open Containing Folder
                    CheckBox.create [
                        CheckBox.verticalAlignment VerticalAlignment.Center
                        CheckBox.content (translate "openContainingFolderAfterBuild")
                        CheckBox.isChecked state.Config.OpenFolderAfterReleaseBuild
                        CheckBox.onChecked (fun _ -> true |> SetOpenFolderAfterReleaseBuild |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> SetOpenFolderAfterReleaseBuild |> EditConfig |> dispatch)
                    ]
                ]
            )
        ]

        // Test Build Options
        locText "test" [
            TextBlock.fontSize 16.
            TextBlock.margin (4., 4., 0., 0.)
        ]
        Border.create [
            Border.borderThickness 1.
            Border.borderBrush Brushes.Gray
            Border.cornerRadius 4.
            Border.padding 6.
            Border.child (
                vStack [
                    hStack [
                        // App ID
                        locText "testingAppId" [
                            TextBlock.verticalAlignment VerticalAlignment.Center
                            TextBlock.margin (0., 0., 10., 0.)
                        ]
                        vStack [
                            RadioButton.create [
                                RadioButton.content "Cherub Rock (248750)"
                                RadioButton.isChecked state.Config.CustomAppId.IsNone
                                RadioButton.onChecked (fun _ -> None |> SetCustomAppId |> EditConfig |> dispatch)
                            ]
                            RadioButton.create [
                                RadioButton.isChecked state.Config.CustomAppId.IsSome
                                RadioButton.content (
                                    hStack [
                                        locText "custom" [
                                            TextBlock.verticalAlignment VerticalAlignment.Center
                                        ]
                                        TextBox.create [
                                            TextBox.verticalAlignment VerticalAlignment.Center
                                            TextBox.width 120.
                                            TextBox.text (Option.toObj state.Config.CustomAppId)
                                            TextBox.onTextChanged (Option.ofString >> SetCustomAppId >> EditConfig >> dispatch)
                                        ]
                                    ]
                                )
                            ]
                        ]
                    ]

                    // Generate DD
                    CheckBox.create [
                        CheckBox.verticalAlignment VerticalAlignment.Center
                        CheckBox.content (translate "generateDD")
                        CheckBox.isChecked state.Config.GenerateDD
                        CheckBox.onChecked (fun _ -> true |> SetGenerateDD |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> SetGenerateDD |> EditConfig |> dispatch)
                    ]

                    // Save Debug Files
                    CheckBox.create [
                        CheckBox.verticalAlignment VerticalAlignment.Center
                        CheckBox.content (translate "saveDebugFiles")
                        CheckBox.isChecked state.Config.SaveDebugFiles
                        CheckBox.onChecked (fun _ -> true |> SetSaveDebugFiles |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> SetSaveDebugFiles |> EditConfig |> dispatch)
                    ]
                ]
            )
        ]
    ]

let private tabHeader (icon: Geometry) locKey =
    vStack [
        Path.create [
            Path.fill Brushes.DarkGray
            Path.data icon
            Path.horizontalAlignment HorizontalAlignment.Center
        ]

        locText locKey [
            TextBlock.horizontalAlignment HorizontalAlignment.Center
            TextBlock.margin (0., 4., 0., 0.)
        ]
    ]

let view state dispatch =
    DockPanel.create [
        DockPanel.width 600.
        DockPanel.height 460.
        DockPanel.children [
            // Close button
            Button.create [
                DockPanel.dock Dock.Bottom
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (translate "close")
                Button.onClick (fun _ -> CloseOverlay |> dispatch)
            ]

            TabControl.create [
                TabControl.viewItems [
                    TabItem.create [
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.cog "general")
                        TabItem.content (generalConfig state dispatch)
                    ]
                    TabItem.create [
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.package "build")
                        TabItem.content (buildConfig state dispatch)
                    ]
                    TabItem.create [
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.bars "dd")
                        TabItem.content (ddConfig state dispatch)
                    ]
                    TabItem.create [
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.import "importHeader")
                        TabItem.content (importConfig state dispatch)
                    ]
                ]
            ]
        ]
    ] |> generalize
