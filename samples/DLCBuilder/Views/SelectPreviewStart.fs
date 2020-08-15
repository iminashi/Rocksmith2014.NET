module DLCBuilder.SelectPreviewStart

open System
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout

let view state dispatch (audioLength: TimeSpan) =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            TextBlock.create [
                TextBlock.fontSize 18.
                TextBlock.text "Preview Audio"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.spacing 8.
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text "Start Time: "
                        Button.verticalAlignment VerticalAlignment.Center
                    ]
                    NumericUpDown.create [
                        NumericUpDown.width 100.
                        NumericUpDown.minimum 0.
                        NumericUpDown.maximum audioLength.TotalSeconds
                        NumericUpDown.value (state.PreviewStartTime.TotalSeconds)
                        NumericUpDown.formatString "F3"
                        NumericUpDown.onValueChanged (PreviewAudioStartChanged >> dispatch)
                    ]
                ]
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.spacing 8.
                StackPanel.children [
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (50., 10.)
                        Button.horizontalAlignment HorizontalAlignment.Center
                        Button.content "Create"
                        Button.onClick (fun _ -> (CreatePreviewAudio CreateFile) |> dispatch)
                    ]
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (50., 10.)
                        Button.horizontalAlignment HorizontalAlignment.Center
                        Button.content "Cancel"
                        Button.onClick (fun _ -> CloseOverlay |> dispatch)
                    ]
                ]
            ]
        ]
    ]
