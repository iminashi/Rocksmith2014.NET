module DLCBuilder.Views.ImportTonesSelector

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Rocksmith2014.Common.Manifest
open DLCBuilder

let view state dispatch (tones: Tone array) =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            // Header
            locText "SelectTonesToImport" [
                TextBlock.fontSize 16.
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            // Tones list
            ToneImportListBox.create [
                ListBox.dataItems tones
                ListBox.maxHeight 300.
                ListBox.width 320.
                ToneImportListBox.onSelectedTonesChanged (SetSelectedImportTones >> dispatch)
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
                        Button.onClick (fun _ -> ImportSelectedTones |> dispatch)
                        Button.isEnabled (not state.SelectedImportTones.IsEmpty)
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
