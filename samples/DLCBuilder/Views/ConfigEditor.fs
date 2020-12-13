module DLCBuilder.ConfigEditor

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Layout
open Rocksmith2014.Common

let view state dispatch =
    Grid.create [
        Grid.columnDefinitions "*,*"
        Grid.rowDefinitions "*,*,*,*,*,*,*,*,*,*"
        Grid.children [
            TextBlock.create [
                Grid.columnSpan 2
                TextBlock.fontSize 16.
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "configuration")
            ]
            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "language")
            ]
            ComboBox.create [
                Grid.column 1
                Grid.row 1
                ComboBox.margin 2.
                ComboBox.dataItems [ Locales.English; Locales.Finnish ]
                ComboBox.selectedItem state.Config.Locale
                ComboBox.onSelectedItemChanged (fun item ->
                    match item with
                    | :? Locale as l -> l |> ChangeLocale |> dispatch
                    | _ -> ())
            ]
            TextBlock.create [
                Grid.row 2
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "releasePlatforms")
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 2
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
                Grid.row 3
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "charterName")
            ]
            TextBox.create [
                Grid.column 1
                Grid.row 3
                TextBox.margin (0., 4.)
                TextBox.text state.Config.CharterName
                TextBox.onTextChanged (fun name ->
                    fun c -> { c with CharterName = name }
                    |> EditConfig
                    |> dispatch)
            ]

            TextBlock.create [
                Grid.row 4
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "profilePath")
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 4
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.width 200.
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
                Grid.row 5
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "testFolder")
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 5
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.width 200.
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
                Grid.row 6
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "projectsFolder")
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 6
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.width 200.
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
                Grid.row 7
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "wwiseConsolePath")
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 7
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBox.create [
                        TextBox.margin (0., 4.)
                        TextBox.width 200.
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
                Grid.row 8
                TextBlock.margin (0., 0., 4., 0.)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "showAdvancedFeatures")
            ]
            CheckBox.create [
                Grid.column 1
                Grid.row 8
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

            Button.create [
                Grid.columnSpan 2
                Grid.row 9
                Button.margin 4.
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (state.Localization.GetString "close")
                Button.onClick (fun _ -> CloseOverlay |> dispatch)
            ]
        ]
    ] :> IView
