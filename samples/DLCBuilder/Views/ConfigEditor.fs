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
                TextBox.onTextChanged (fun name ->
                    fun c -> { c with CharterName = name }
                    |> EditConfig
                    |> dispatch)
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
                        TextBox.onTextChanged (SetProfilePath >> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> dispatch (Msg.OpenFileDialog("selectProfile", Dialogs.profileFilter, SetProfilePath)))
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
                        TextBox.onTextChanged (SetTestFolderPath >> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> dispatch (Msg.OpenFolderDialog("selectTestFolder", SetTestFolderPath)))
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
                        TextBox.onTextChanged (SetProjectsFolderPath >> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> dispatch (Msg.OpenFolderDialog("selectProjectFolder", SetProjectsFolderPath)))
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
                        TextBox.onTextChanged (SetWwiseConsolePath >> dispatch)
                        ToolTip.tip (state.Localization.GetString "wwiseConsolePathTooltip")
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ ->
                            dispatch (Msg.OpenFileDialog("selectWwiseConsolePath",
                                                         Dialogs.wwiseConsoleAppFilter state.CurrentPlatform,
                                                         SetWwiseConsolePath))
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
                CheckBox.onChecked (fun _ ->
                    fun c -> { c with AutoVolume = true }
                    |> EditConfig
                    |> dispatch)
                CheckBox.onUnchecked (fun _ ->
                    fun c -> { c with AutoVolume = false }
                    |> EditConfig
                    |> dispatch)
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
                CheckBox.onChecked (fun _ ->
                    fun c -> { c with ShowAdvanced = true }
                    |> EditConfig
                    |> dispatch)
                CheckBox.onUnchecked (fun _ ->
                    fun c -> { c with ShowAdvanced = false }
                    |> EditConfig
                    |> dispatch)
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
                        CheckBox.onChecked (fun _ ->
                            fun c -> { c with ReleasePlatforms = PC::c.ReleasePlatforms }
                            |> EditConfig
                            |> dispatch)
                        CheckBox.onUnchecked (fun _ ->
                            fun c -> { c with ReleasePlatforms = c.ReleasePlatforms |> List.remove PC }
                            |> EditConfig
                            |> dispatch)
                    ]
                    CheckBox.create [
                        CheckBox.margin 2.
                        CheckBox.content "Mac"
                        CheckBox.isEnabled (state.Config.ReleasePlatforms |> List.contains PC)
                        CheckBox.isChecked (state.Config.ReleasePlatforms |> List.contains Mac)
                        CheckBox.onChecked (fun _ ->
                            fun c -> { c with ReleasePlatforms = Mac::c.ReleasePlatforms }
                            |> EditConfig
                            |> dispatch)
                        CheckBox.onUnchecked (fun _ ->
                            fun c -> { c with ReleasePlatforms = c.ReleasePlatforms |> List.remove Mac }
                            |> EditConfig
                            |> dispatch)
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
                        RadioButton.onChecked (fun _ -> None |> SetCustomAppId |> dispatch)
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
                                        TextBox.onTextChanged (fun appId ->
                                            if String.notEmpty appId then
                                                Some appId
                                                |> SetCustomAppId
                                                |> dispatch
                                        )
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
                CheckBox.onChecked (fun _ ->
                    fun c -> { c with GenerateDD = true }
                    |> EditConfig
                    |> dispatch)
                CheckBox.onUnchecked (fun _ ->
                    fun c -> { c with GenerateDD = false }
                    |> EditConfig
                    |> dispatch)
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
                CheckBox.onChecked (fun _ ->
                    fun c -> { c with DDPhraseSearchEnabled = true }
                    |> EditConfig
                    |> dispatch)
                CheckBox.onUnchecked (fun _ ->
                    fun c -> { c with DDPhraseSearchEnabled = false }
                    |> EditConfig
                    |> dispatch)
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
                        NumericUpDown.onValueChanged (fun value ->
                            fun c -> { c with DDPhraseSearchThreshold = int value }
                            |> EditConfig
                            |> dispatch)
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
                CheckBox.onChecked (fun _ ->
                    fun c -> { c with ApplyImprovements = true }
                    |> EditConfig
                    |> dispatch)
                CheckBox.onUnchecked (fun _ ->
                    fun c -> { c with ApplyImprovements = false }
                    |> EditConfig
                    |> dispatch)
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
                CheckBox.onChecked (fun _ ->
                    fun c -> { c with SaveDebugFiles = true }
                    |> EditConfig
                    |> dispatch)
                CheckBox.onUnchecked (fun _ ->
                    fun c -> { c with SaveDebugFiles = false }
                    |> EditConfig
                    |> dispatch)
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
                ]
            ]
        ]
    ] |> Helpers.generalize
