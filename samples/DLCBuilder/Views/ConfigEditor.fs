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
        Grid.columnDefinitions "*,2*"
        Grid.rowDefinitions "*,*,*,*,*,*,*,*"
        Grid.children [
            TextBlock.create [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "language")
            ]
            ComboBox.create [
                Grid.column 1
                ComboBox.dataItems [ Locales.English; Locales.Finnish ]
                ComboBox.selectedItem state.Config.Locale
                ComboBox.onSelectedItemChanged (fun item ->
                    match item with
                    | :? Locale as l -> l |> ChangeLocale |> dispatch
                    | _ -> ())
            ]

            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "charterName")
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
                TextBlock.text (state.Localization.GetString "profilePath")
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 2
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.width 250.
                        TextBox.text state.Config.ProfilePath
                        TextBox.onTextChanged (SetProfilePath >> EditConfig >> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> dispatch (Msg.OpenFileDialog("selectProfile", Dialogs.profileFilter, SetProfilePath >> EditConfig)))
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 3
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "testFolder")
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 3
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.width 250.
                        TextBox.text state.Config.TestFolderPath
                        TextBox.onTextChanged (SetTestFolderPath >> EditConfig >> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> dispatch (Msg.OpenFolderDialog("selectTestFolder", SetTestFolderPath >> EditConfig)))
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 4
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "projectsFolder")
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 4
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.width 250.
                        TextBox.text state.Config.ProjectsFolderPath
                        TextBox.onTextChanged (SetProjectsFolderPath >> EditConfig >> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> dispatch (Msg.OpenFolderDialog("selectProjectFolder", SetProjectsFolderPath >> EditConfig)))
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 5
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "wwiseConsolePath")
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 5
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.width 250.
                        TextBox.text (Option.toObj state.Config.WwiseConsolePath)
                        TextBox.onTextChanged (SetWwiseConsolePath >> EditConfig >> dispatch)
                        ToolTip.tip (state.Localization.GetString "wwiseConsolePathTooltip")
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ ->
                            dispatch (Msg.OpenFileDialog("selectWwiseConsolePath",
                                                         Dialogs.wwiseConsoleAppFilter state.CurrentPlatform,
                                                         SetWwiseConsolePath >> EditConfig))
                        )
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 6
                TextBlock.margin (0., 0., 4., 0.)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "calculateVolumes")
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
                TextBlock.text (state.Localization.GetString "showAdvancedFeatures")
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
        |> state.Localization.GetString

    StackPanel.create [
        StackPanel.margin (8., 0.)
        StackPanel.children [
            TextBlock.create [
                TextBlock.text (state.Localization.GetString "psarcImportHeader")
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.fontSize 17.
                TextBlock.margin (0., 0., 0., 4.)
            ]

            TextBlock.create [
                TextBlock.text (state.Localization.GetString "convertWemOnImport")
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
                CheckBox.content (state.Localization.GetString "removeDDLevels")
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
        Grid.columnDefinitions "*,2*"
        Grid.rowDefinitions "*,*,*,*,*,*,*"
        Grid.verticalAlignment VerticalAlignment.Top
        Grid.children [
            TextBlock.create [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "releasePlatforms")
            ]
            StackPanel.create [
                Grid.column 1
                StackPanel.margin (0., 2.)
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    CheckBox.create [
                        CheckBox.margin 2.
                        CheckBox.content "PC"
                        CheckBox.isEnabled (state.Config.ReleasePlatforms |> List.contains Mac)
                        CheckBox.isChecked (state.Config.ReleasePlatforms |> List.contains PC)
                        CheckBox.onChecked (fun _ -> PC |> AddReleasePlatform |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> PC |> RemoveReleasePlatform |> EditConfig |> dispatch)
                    ]
                    CheckBox.create [
                        CheckBox.margin 2.
                        CheckBox.content "Mac"
                        CheckBox.isEnabled (state.Config.ReleasePlatforms |> List.contains PC)
                        CheckBox.isChecked (state.Config.ReleasePlatforms |> List.contains Mac)
                        CheckBox.onChecked (fun _ -> Mac |> AddReleasePlatform |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> Mac |> RemoveReleasePlatform |> EditConfig |> dispatch)
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "testingAppId")
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
                                        TextBlock.text (state.Localization.GetString "custom")
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
                TextBlock.text (state.Localization.GetString "generateDD")
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
                TextBlock.text (state.Localization.GetString "findSimilarPhrases")
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
                TextBlock.text (state.Localization.GetString "similarityThreshold")
                TextBlock.isEnabled (state.Config.DDPhraseSearchEnabled && state.Config.GenerateDD)
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 4
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    NumericUpDown.create [
                        NumericUpDown.margin (0., 2., 2., 2.)
                        NumericUpDown.width 60.
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
                TextBlock.text (state.Localization.GetString "applyImprovements")
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
                TextBlock.text (state.Localization.GetString "saveDebugFiles")
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

let private tabHeader (icon: Geometry) text =
    StackPanel.create [
        StackPanel.minWidth 70.
        StackPanel.children [
            Path.create [
                Path.fill Brushes.DarkGray
                Path.data icon
                Path.horizontalAlignment HorizontalAlignment.Center
            ]

            TextBlock.create [
                TextBlock.text text
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.foreground Brushes.WhiteSmoke
                TextBlock.margin (0., 4., 0., 0.)
            ]
        ]
    ]

let view state dispatch =
    DockPanel.create [
        DockPanel.children [
            TextBlock.create [
                DockPanel.dock Dock.Top
                TextBlock.fontSize 16.
                TextBlock.margin (0., 0., 0., 5.)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "configuration")
            ]

            Button.create [
                DockPanel.dock Dock.Bottom
                Button.margin 4.
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (state.Localization.GetString "close")
                Button.onClick (fun _ -> CloseOverlay |> dispatch)
            ]

            TabControl.create [
                TabControl.minHeight 250.
                TabControl.minWidth 520.
                TabControl.tabStripPlacement Dock.Left
                TabControl.viewItems [
                    TabItem.create [
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.cog (state.Localization.GetString "general"))
                        TabItem.content (generalConfig state dispatch)
                    ]
                    TabItem.create [
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.package (state.Localization.GetString "build"))
                        TabItem.content (buildConfig state dispatch)
                    ]
                    TabItem.create [
                        TabItem.horizontalAlignment HorizontalAlignment.Center
                        TabItem.header (tabHeader Media.Icons.import (state.Localization.GetString "importHeader"))
                        TabItem.content (importConfig state dispatch)
                    ]
                ]
            ]
        ]
    ] |> Helpers.generalize
