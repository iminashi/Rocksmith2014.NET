module DLCBuilder.VocalsDetails

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Rocksmith2014.DLCProject
open System.IO

let view state dispatch (v: Vocals) =
    Grid.create [
        Grid.columnDefinitions "*,3*"
        Grid.rowDefinitions "*,*,*,*"
        Grid.margin (0.0, 4.0)
        //Grid.showGridLines true
        Grid.children [
            TextBlock.create [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text "Japanese: "
            ]
            CheckBox.create [
                Grid.column 1
                CheckBox.margin 4.0
                CheckBox.isChecked v.Japanese
                CheckBox.onChecked (fun _ -> (fun v -> { v with Japanese = true }) |> EditVocals |> dispatch)
                CheckBox.onUnchecked (fun _ -> (fun v -> { v with Japanese = false }) |> EditVocals |> dispatch)
            ]

            // Custom font
            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text "Custom Font: "
            ]
            DockPanel.create [
                Grid.column 1
                Grid.row 1
                DockPanel.children [
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0.0, 4.0, 4.0, 4.0)
                        Button.content "X"
                        Button.isVisible (Option.isSome v.CustomFont)
                        Button.onClick (fun _ -> (fun v -> { v with CustomFont = None }) |> EditVocals |> dispatch)
                        ToolTip.tip "Click to remove the custom font from the arrangement."
                    ]
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0.0, 4.0, 4.0, 4.0)
                        Button.content "..."
                        Button.onClick (fun _ -> SelectCustomFont |> dispatch)
                        ToolTip.tip "Click to select a custom font file."
                    ]
                    TextBlock.create [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                        TextBlock.text (v.CustomFont |> Option.map Path.GetFileName |> Option.defaultValue "None")
                    ]
                ]
            ]
        ]
    ]
