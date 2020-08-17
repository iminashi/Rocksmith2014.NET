module DLCBuilder.ConfigEditor

open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Rocksmith2014.Common

let view state dispatch =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.text "Release Platforms: "
                    ]
                    CheckBox.create [
                        CheckBox.margin 2.
                        CheckBox.content "PC"
                        CheckBox.isEnabled (state.Config.ReleasePlatforms |> List.contains Mac)
                        CheckBox.isChecked (state.Config.ReleasePlatforms |> List.contains PC)
                        CheckBox.onChecked (fun _ -> (fun c -> { c with ReleasePlatforms = PC::c.ReleasePlatforms }) |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> (fun c -> { c with ReleasePlatforms = c.ReleasePlatforms |> List.remove PC }) |> EditConfig |> dispatch)
                    ]
                    CheckBox.create [
                        CheckBox.margin 2.
                        CheckBox.content "Mac"
                        CheckBox.isEnabled (state.Config.ReleasePlatforms |> List.contains PC)
                        CheckBox.isChecked (state.Config.ReleasePlatforms |> List.contains Mac)
                        CheckBox.onChecked (fun _ -> (fun c -> { c with ReleasePlatforms = Mac::c.ReleasePlatforms }) |> EditConfig |> dispatch)
                        CheckBox.onUnchecked (fun _ -> (fun c -> { c with ReleasePlatforms = c.ReleasePlatforms |> List.remove Mac }) |> EditConfig |> dispatch)
                    ]
                ]
            ]
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBlock.create [
                        TextBox.margin 4.
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.text "Charter Name:"
                    ]
                    TextBox.create [
                        Button.margin (0., 4.)
                        TextBox.width 200.
                        TextBox.text state.Config.CharterName
                        TextBox.onTextChanged (fun name -> (fun c -> { c with CharterName = name }) |> EditConfig |> dispatch)
                    ]
                ]
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBlock.create [
                        TextBox.margin 4.
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.text "Profile Path:"
                    ]
                    TextBox.create [
                        Button.margin (0., 4.)
                        TextBox.width 200.
                        TextBox.text state.Config.ProfilePath
                        TextBox.onTextChanged (fun name -> (fun c -> { c with ProfilePath = name }) |> EditConfig |> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> SelectProfilePath |> dispatch)
                    ]
                ]
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBlock.create [
                        TextBox.margin 4.
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.text "Test Folder:"
                    ]
                    TextBox.create [
                        Button.margin (0., 4.)
                        TextBox.width 200.
                        TextBox.text state.Config.TestFolderPath
                        TextBox.onTextChanged (fun name -> (fun c -> { c with TestFolderPath = name }) |> EditConfig |> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> SelectTestFolderPath |> dispatch)
                    ]
                ]
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    TextBlock.create [
                        TextBox.margin 4.
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.text "Projects Folder:"
                    ]
                    TextBox.create [
                        Button.margin (0., 4.)
                        TextBox.width 200.
                        TextBox.text state.Config.ProjectsFolderPath
                        TextBox.onTextChanged (fun name -> (fun c -> { c with ProjectsFolderPath = name }) |> EditConfig |> dispatch)
                    ]
                    Button.create [
                        Button.margin (0., 4.)
                        Button.content "..."
                        Button.onClick (fun _ -> SelectProjectsFolderPath |> dispatch)
                    ]
                ]
            ]

            CheckBox.create [
                CheckBox.isChecked state.Config.ShowAdvanced
                CheckBox.content "Show Advanced Features"
                CheckBox.onChecked (fun _ -> (fun c -> { c with ShowAdvanced = true }) |> EditConfig |> dispatch)
                CheckBox.onUnchecked (fun _ -> (fun c -> { c with ShowAdvanced = false }) |> EditConfig |> dispatch)
            ]

            Button.create [
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content "Close"
                Button.onClick (fun _ -> SaveConfiguration |> dispatch)
            ]

            //StackPanel.create [
            //    StackPanel.orientation Orientation.Horizontal
            //    StackPanel.spacing 8.
            //    StackPanel.children [
            //        Button.create [
            //            Button.fontSize 16.
            //            Button.padding (50., 10.)
            //            Button.horizontalAlignment HorizontalAlignment.Center
            //            Button.content "OK"
            //            Button.onClick (fun _ -> SaveConfiguration |> dispatch)
            //        ]
            //        Button.create [
            //            Button.fontSize 16.
            //            Button.padding (50., 10.)
            //            Button.horizontalAlignment HorizontalAlignment.Center
            //            Button.content "Cancel"
            //            Button.onClick (fun _ -> CloseOverlay |> dispatch)
            //        ]
            //    ]
            //]
        ]
    ]
