module DLCBuilder.Views.ConfigEditor

open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.Common
open DLCBuilder

let private generalConfig state dispatch =
    Grid.create [
        Grid.columnDefinitions "auto,*"
        Grid.rowDefinitions "*,*,*,*,*,*,*,*"
        Grid.children [
            TextBlock.create [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "language")
            ]
            ComboBox.create [
                Grid.column 1
                ComboBox.dataItems Locales.All
                ComboBox.selectedItem state.Config.Locale
                ComboBox.onSelectedItemChanged (function
                    | :? Locale as l -> l |> ChangeLocale |> dispatch
                    | _ -> ())
            ]

            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "charterName")
            ]
            TextBox.create [
                Grid.column 1
                Grid.row 1
                TextBox.margin (0., 4.)
                TextBox.text state.Config.CharterName
                TextBox.onTextChanged (SetCharterName >> EditConfig >> dispatch)
            ]

            TextBlock.create [
                Grid.row 2
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "profilePath")
            ]
            DockPanel.create [
                Grid.column 1
                Grid.row 2
                DockPanel.children [
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ ->
                            Msg.OpenFileDialog("selectProfile", Dialogs.profileFilter, SetProfilePath >> EditConfig)
                            |> dispatch)
                    ]
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.text state.Config.ProfilePath
                        TextBox.onTextChanged (SetProfilePath >> EditConfig >> dispatch)
                    ]

                ]
            ]

            TextBlock.create [
                Grid.row 3
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "testFolder")
            ]
            DockPanel.create [
                Grid.column 1
                Grid.row 3
                DockPanel.children [
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ ->
                            Msg.OpenFolderDialog("selectTestFolder", SetTestFolderPath >> EditConfig)
                            |> dispatch)
                    ]
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.text state.Config.TestFolderPath
                        TextBox.watermark (translate "testFolderPlaceholder")
                        TextBox.onTextChanged (SetTestFolderPath >> EditConfig >> dispatch)
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 4
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "projectsFolder")
            ]
            DockPanel.create [
                Grid.column 1
                Grid.row 4
                DockPanel.children [
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ ->
                            Msg.OpenFolderDialog("selectProjectFolder", SetProjectsFolderPath >> EditConfig)
                            |> dispatch)
                    ]
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.text state.Config.ProjectsFolderPath
                        TextBox.onTextChanged (SetProjectsFolderPath >> EditConfig >> dispatch)
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 5
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "wwiseConsolePath")
            ]
            DockPanel.create [
                Grid.column 1
                Grid.row 5
                DockPanel.children [
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ ->
                            Msg.OpenFileDialog("selectWwiseConsolePath",
                                               Dialogs.wwiseConsoleAppFilter state.CurrentPlatform,
                                               SetWwiseConsolePath >> EditConfig)
                            |> dispatch)
                    ]
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.text (Option.toObj state.Config.WwiseConsolePath)
                        TextBox.watermark (translate "wwiseConsolePathPlaceholder")
                        TextBox.onTextChanged (SetWwiseConsolePath >> EditConfig >> dispatch)
                        ToolTip.tip (translate "wwiseConsolePathTooltip")
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 6
                TextBlock.margin (0., 0., 4., 0.)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "calculateVolumes")
            ]
            CheckBox.create [
                Grid.column 1
                Grid.row 6
                CheckBox.isChecked state.Config.AutoVolume
                CheckBox.onChecked (fun _ -> true |> SetAutoVolume |> EditConfig |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetAutoVolume |> EditConfig |> dispatch)
            ]

            TextBlock.create [
                Grid.row 7
                TextBlock.margin (0., 0., 4., 0.)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "showAdvancedFeatures")
            ]
            CheckBox.create [
                Grid.column 1
                Grid.row 7
                CheckBox.isChecked state.Config.ShowAdvanced
                CheckBox.onChecked (fun _ -> true |> SetShowAdvanced |> EditConfig |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetShowAdvanced |> EditConfig |> dispatch)
            ]
        ]
     ]

let private importConfig state dispatch =
    let localize conv =
        match conv with
        | NoConversion -> "noConversion"
        | ToOgg -> "toOggFile"
        | ToWav -> "toWavFile"
        |> translate

    StackPanel.create [
        StackPanel.margin (8., 0.)
        StackPanel.children [
            TextBlock.create [
                TextBlock.text (translate "psarcImportHeader")
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.fontSize 17.
                TextBlock.margin (0., 0., 0., 4.)
            ]

            TextBlock.create [
                TextBlock.text (translate "convertWemOnImport")
                TextBlock.horizontalAlignment HorizontalAlignment.Left
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

            CheckBox.create [
                CheckBox.content (translate "removeDDLevels")
                CheckBox.horizontalAlignment HorizontalAlignment.Left
                CheckBox.margin (0., 8., 0., 4.)
                CheckBox.isChecked state.Config.RemoveDDOnImport
                CheckBox.onChecked (fun _ -> true |> SetRemoveDDOnImport |> EditConfig |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetRemoveDDOnImport |> EditConfig |> dispatch)
            ]
        ]
    ]

let private buildConfig state dispatch =
    Grid.create [
        Grid.columnDefinitions "auto,*"
        Grid.rowDefinitions "auto,auto,auto,auto,auto,auto,auto"
        Grid.verticalAlignment VerticalAlignment.Top
        Grid.children [
            TextBlock.create [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "releasePlatforms")
            ]
            StackPanel.create [
                Grid.column 1
                StackPanel.margin (0., 2.)
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    CheckBox.create [
                        CheckBox.margin 2.
                        CheckBox.content "PC"
                        CheckBox.isEnabled (state.Config.ReleasePlatforms |> Set.contains Mac)
                        CheckBox.isChecked (state.Config.ReleasePlatforms |> Set.contains PC)
                        CheckBox.onChecked (fun _ -> PC |> AddReleasePlatform |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> PC |> RemoveReleasePlatform |> EditConfig |> dispatch)
                    ]
                    CheckBox.create [
                        CheckBox.margin 2.
                        CheckBox.content "Mac"
                        CheckBox.isEnabled (state.Config.ReleasePlatforms |> Set.contains PC)
                        CheckBox.isChecked (state.Config.ReleasePlatforms |> Set.contains Mac)
                        CheckBox.onChecked (fun _ -> Mac |> AddReleasePlatform |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> Mac |> RemoveReleasePlatform |> EditConfig |> dispatch)
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "testingAppId")
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 1
                StackPanel.margin (0., 2.)
                StackPanel.children [
                    RadioButton.create [
                        RadioButton.content "Cherub Rock (248750)"
                        RadioButton.isChecked (state.Config.CustomAppId.IsNone)
                        RadioButton.onChecked (fun _ -> None |> SetCustomAppId |> EditConfig |> dispatch)
                    ]
                    RadioButton.create [
                        RadioButton.isChecked (state.Config.CustomAppId.IsSome)
                        RadioButton.content (
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.children [
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text (translate "custom")
                                    ]
                                    TextBox.create [
                                        TextBox.verticalAlignment VerticalAlignment.Center
                                        TextBox.width 120.
                                        TextBox.text (Option.toObj state.Config.CustomAppId)
                                        TextBox.onTextChanged (Option.ofString >> SetCustomAppId >> EditConfig >> dispatch)
                                    ]
                                ]
                            ]
                        )
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 2
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "generateDD")
            ]
            CheckBox.create [
                Grid.column 1
                Grid.row 2
                CheckBox.margin (0., 2.)
                CheckBox.isChecked state.Config.GenerateDD
                CheckBox.onChecked (fun _ -> true |> SetGenerateDD |> EditConfig |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetGenerateDD |> EditConfig |> dispatch)
            ]

            TextBlock.create [
                Grid.row 3
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "findSimilarPhrases")
            ]
            CheckBox.create [
                Grid.column 1
                Grid.row 3
                CheckBox.margin (0., 2.)
                CheckBox.isEnabled state.Config.GenerateDD
                CheckBox.isChecked state.Config.DDPhraseSearchEnabled
                CheckBox.onChecked (fun _ -> true |> SetDDPhraseSearchEnabled |> EditConfig |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetDDPhraseSearchEnabled |> EditConfig |> dispatch)
            ]

            TextBlock.create [
                Grid.row 4
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "similarityThreshold")
                TextBlock.isEnabled (state.Config.DDPhraseSearchEnabled && state.Config.GenerateDD)
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 4
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    NumericUpDown.create [
                        NumericUpDown.margin (0., 2., 2., 2.)
                        NumericUpDown.width 140.
                        NumericUpDown.value (float state.Config.DDPhraseSearchThreshold)
                        NumericUpDown.isEnabled (state.Config.DDPhraseSearchEnabled && state.Config.GenerateDD)
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

            TextBlock.create [
                Grid.row 5
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "applyImprovements")
            ]
            CheckBox.create [
                Grid.column 1
                Grid.row 5
                CheckBox.margin (0., 2.)
                CheckBox.isChecked state.Config.ApplyImprovements
                CheckBox.onChecked (fun _ -> true |> SetApplyImprovements |> EditConfig |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetApplyImprovements |> EditConfig |> dispatch)
            ]

            TextBlock.create [
                Grid.row 6
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "saveDebugFiles")
            ]
            CheckBox.create [
                Grid.column 1
                Grid.row 6
                CheckBox.margin (0., 2.)
                CheckBox.isChecked state.Config.SaveDebugFiles
                CheckBox.onChecked (fun _ -> true |> SetSaveDebugFiles |> EditConfig |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetSaveDebugFiles |> EditConfig |> dispatch)
            ]
        ]
     ]

let private tabHeader (icon: Geometry) locText =
    StackPanel.create [
        StackPanel.children [
            Path.create [
                Path.fill Brushes.DarkGray
                Path.data icon
                Path.horizontalAlignment HorizontalAlignment.Center
            ]

            TextBlock.create [
                TextBlock.text (translate locText)
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.margin (0., 4., 0., 0.)
            ]
        ]
    ]

let view state dispatch =
    DockPanel.create [
        DockPanel.width 600.
        DockPanel.height 400.
        DockPanel.children [
            // Close button
            Button.create [
                DockPanel.dock Dock.Bottom
                Button.margin 4.
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
                        TabItem.header (tabHeader Media.Icons.import "importHeader")
                        TabItem.content (importConfig state dispatch)
                    ]
                ]
            ]
        ]
    ] |> generalize
