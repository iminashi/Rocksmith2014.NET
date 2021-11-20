module DLCBuilder.Views.PreviewStartSelector

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open System
open DLCBuilder

let view state dispatch (data: PreviewAudioCreationData) =
    // Remove the length of the preview from the total length
    let length = data.AudioLength - TimeSpan.FromSeconds(28.)

    let previewStart =
        state.Project.AudioPreviewStartTime
        |> Option.defaultValue 0.

    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            // Title
            locText "PreviewAudio" [
                TextBlock.fontSize 18.
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            // Explanation
            locText "PreviewCreationExplanation" [
                TextBlock.fontSize 14.
                TextBlock.margin (0., 5.)
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            StackPanel.create [
                StackPanel.margin (0., 5.)
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.isVisible (length.TotalSeconds > 0.)
                StackPanel.children [
                    // Start time
                    locText "StartTime" [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                    ]
                    FixedNumericUpDown.create [
                        NumericUpDown.width 180.
                        NumericUpDown.minimum 0.
                        NumericUpDown.maximum (max 0. length.TotalSeconds)
                        NumericUpDown.formatString "F3"
                        FixedNumericUpDown.value previewStart
                        FixedNumericUpDown.onValueChanged (SetPreviewStartTime >> EditProject >> dispatch)
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

            if length.TotalSeconds <= 0. then
                locText "PreviewAudioLengthNotification" []

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
                        Button.content (translate "Create")
                        Button.onClick (fun _ -> CreatePreviewAudio (CreateFile data) |> dispatch)
                    ]

                    // Cancel
                    Button.create [
                        Button.fontSize 16.
                        Button.padding (50., 10.)
                        Button.content (translate "Cancel")
                        Button.onClick (fun _ -> dispatch (CloseOverlay OverlayCloseMethod.OverlayButton))
                    ]
                ]
            ]
        ]
    ] |> generalize
