module DLCBuilder.Views.ImportTonesSelector

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Layout
open Rocksmith2014.Common.Manifest
open DLCBuilder

let view state dispatch (tones: Tone array) =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            // Header
            TextBlock.create [
                TextBlock.fontSize 16.
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "selectImportTone")
            ]

            // Tones list
            ListBox.create [
                ListBox.dataItems tones
                ListBox.selection state.SelectedImportTones
                ListBox.maxHeight 300.
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
                        Button.content (translate "import")
                        Button.onClick (fun _ -> dispatch ImportSelectedTones)
                        Button.isDefault true
                    ]

                    // All
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (20., 10.)
                        Button.content (translate "all")
                        Button.onClick (fun _ -> tones |> List.ofArray |> ImportTones |> dispatch)
                    ]

                    // Cancel
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (30., 10.)
                        Button.content (translate "cancel")
                        Button.onClick (fun _ -> dispatch CloseOverlay)
                    ]
                ]
            ]
        ]
    ] :> IView
