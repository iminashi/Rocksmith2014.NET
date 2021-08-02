module DLCBuilder.Views.ImportTonesSelector

open Avalonia.Controls
open Avalonia.Controls.Selection
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Rocksmith2014.Common.Manifest
open DLCBuilder

let private selectedTones = SelectionModel<Tone>(SingleSelect = false)

let private getSelectedTones () =
    Seq.toList selectedTones.SelectedItems

let view dispatch (tones: Tone array) =
    selectedTones.Clear()
    selectedTones.Source <- null

    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            // Header
            locText "SelectTonesToImport" [
                TextBlock.fontSize 16.
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            // Tones list
            ListBox.create [
                ListBox.dataItems tones
                ListBox.selection selectedTones
                ListBox.maxHeight 300.
                ListBox.width 320.
            ]

            // Buttons
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.children [
                    // Import
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (30., 10.)
                        Button.content (translate "Import")
                        Button.onClick (fun _ -> getSelectedTones() |> ImportTones |> dispatch)
                        Button.isDefault true
                    ]

                    // All
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (20., 10.)
                        Button.content (translate "All")
                        Button.onClick (fun _ -> tones |> List.ofArray |> ImportTones |> dispatch)
                    ]

                    // Cancel
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (30., 10.)
                        Button.content (translate "Cancel")
                        Button.onClick (fun _ -> dispatch CloseOverlay)
                    ]
                ]
            ]
        ]
    ] |> generalize
