module DLCBuilder.Views.SelectPreviewStart

open System
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.FuncUI.Types
open DLCBuilder

let view state dispatch (audioLength: TimeSpan) =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            // Title
            TextBlock.create [
                TextBlock.fontSize 18.
                TextBlock.text (translate "previewAudio")
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            // Explanation
            TextBlock.create [
                TextBlock.fontSize 14.
                TextBlock.margin (0., 5.)
                TextBlock.text (translate "previewExplanation")
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            StackPanel.create [
                StackPanel.margin (0., 5.)
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.children [
                    // Start time
                    TextBlock.create [
                        TextBlock.text (translate "startTime")
                        Button.verticalAlignment VerticalAlignment.Center
                    ]
                    NumericUpDown.create [
                        NumericUpDown.width 180.
                        NumericUpDown.minimum 0.
                        NumericUpDown.maximum audioLength.TotalSeconds
                        NumericUpDown.value (state.PreviewStartTime.TotalSeconds)
                        NumericUpDown.formatString "F3"
                        NumericUpDown.onValueChanged (PreviewAudioStartChanged >> dispatch)
                    ]
                ]
            ]

            // Buttons
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.children [
                    // Create
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (50., 10.)
                        Button.content (translate "create")
                        Button.onClick (fun _ -> (CreatePreviewAudio CreateFile) |> dispatch)
                    ]

                    // Cancel
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (50., 10.)
                        Button.content (translate "cancel")
                        Button.onClick (fun _ -> dispatch CloseOverlay)
                    ]
                ]
            ]
        ]
    ] :> IView
