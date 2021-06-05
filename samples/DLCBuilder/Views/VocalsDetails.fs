module DLCBuilder.Views.VocalsDetails

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Rocksmith2014.DLCProject
open System.IO
open DLCBuilder

let view dispatch (v: Vocals) =
    Grid.create [
        Grid.columnDefinitions "auto,*"
        Grid.rowDefinitions "*,*,*,*"
        Grid.margin 6.
        Grid.children [
            // Japanese lyrics
            locText "japanese" [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            CheckBox.create [
                Grid.column 1
                CheckBox.margin 4.0
                CheckBox.isChecked v.Japanese
                CheckBox.onChecked (fun _ -> true |> SetIsJapanese |> EditVocals |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetIsJapanese |> EditVocals |> dispatch)
            ]

            // Custom font
            locText "customFont" [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            DockPanel.create [
                Grid.column 1
                Grid.row 1
                DockPanel.children [
                    // Remove font
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0.0, 4.0, 4.0, 4.0)
                        Button.content "X"
                        Button.isVisible (Option.isSome v.CustomFont)
                        Button.onClick (fun _ -> None |> SetCustomFont |> EditVocals |> dispatch)
                        ToolTip.tip (translate "removeCustomFontToolTip")
                    ]

                    // Select font
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0.0, 4.0, 4.0, 4.0)
                        Button.content "..."
                        Button.onClick (fun _ -> Dialog.CustomFont |> ShowDialog |> dispatch)
                        ToolTip.tip (translate "selectCustomFontToolTip")
                    ]

                    // Custom font filename
                    TextBlock.create [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                        TextBlock.text (
                            v.CustomFont
                            |> Option.map Path.GetFileName
                            |> Option.defaultValue (translate "none"))
                    ]
                ]
            ]
        ]
    ]
