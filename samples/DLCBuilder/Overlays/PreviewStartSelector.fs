module DLCBuilder.Views.PreviewStartSelector

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open System
open DLCBuilder

let view state dispatch (audioLength: TimeSpan) =
    let previewStart =
        state.Project.AudioPreviewStartTime
        |> Option.defaultValue 0.

    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            // Title
            locText "previewAudio" [
                TextBlock.fontSize 18.
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            // Explanation
            locText "previewExplanation" [
                TextBlock.fontSize 14.
                TextBlock.margin (0., 5.)
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            StackPanel.create [
                StackPanel.margin (0., 5.)
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.children [
                    // Start time
                    locText "startTime" [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                    ]
                    NumericUpDown.create [
                        NumericUpDown.width 180.
                        NumericUpDown.minimum 0.
                        NumericUpDown.maximum audioLength.TotalSeconds
                        NumericUpDown.value previewStart
                        NumericUpDown.formatString "F3"
                        NumericUpDown.onValueChanged (SetPreviewStartTime >> EditProject >> dispatch)
                    ]
                    TextBlock.create [
                        let minutes = previewStart / 60. |> floor
                        let seconds = previewStart - minutes * 60.
                        TextBlock.width 100.
                        TextBlock.text $"(%02i{int minutes}:%06.3f{seconds})"
                        TextBlock.verticalAlignment VerticalAlignment.Center
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
                        Button.onClick (fun _ -> CreatePreviewAudio CreateFile |> dispatch)
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
    ] |> generalize
