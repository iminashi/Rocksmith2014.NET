module DLCBuilder.SelectImportTones

open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Rocksmith2014.Common.Manifest
open Avalonia.FuncUI.Types

let view state dispatch (tones: Tone array) =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            TextBlock.create [
                TextBlock.fontSize 16.
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "selectImportTone")
            ]
            ListBox.create [
                ListBox.name "tonesListBox"
                ListBox.dataItems tones
                // Multiple selection mode is broken in Avalonia 0.9
                // https://github.com/AvaloniaUI/Avalonia/issues/3497
                ListBox.selectionMode SelectionMode.Single
                ListBox.maxHeight 300.
                ListBox.onSelectedItemChanged (ImportTonesChanged >> dispatch)
            ]
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.spacing 8.
                StackPanel.children [
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (50., 10.)
                        Button.horizontalAlignment HorizontalAlignment.Center
                        Button.content (state.Localization.GetString "import")
                        Button.onClick (fun _ -> dispatch ImportSelectedTones)
                        Button.isDefault true
                    ]
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (50., 10.)
                        Button.horizontalAlignment HorizontalAlignment.Center
                        Button.content (state.Localization.GetString "cancel")
                        Button.onClick (fun _ -> dispatch CloseOverlay)
                    ]
                ]
            ]
        ]
    ] :> IView
