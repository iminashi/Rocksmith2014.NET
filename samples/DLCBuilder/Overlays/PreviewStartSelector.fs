module DLCBuilder.Views.PreviewStartSelector

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open System
open DLCBuilder

let view state dispatch (data: PreviewAudioCreationData) =
    let previewStart =
        state.Project.AudioPreviewStartTime
        |> Option.defaultValue (TimeSpan())

    let maxSecs = if previewStart.Minutes = data.MaxPreviewStart.Minutes then data.MaxPreviewStart.Seconds else 59
    let maxMs = if previewStart.Seconds = data.MaxPreviewStart.Seconds then data.MaxPreviewStart.Milliseconds else 999

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
                StackPanel.isVisible (data.MaxPreviewStart.TotalSeconds > 0.)
                StackPanel.children [
                    // Start time
                    locText "StartTime" [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                    ]

                    // Minutes
                    FixedNumericUpDown.create [
                        NumericUpDown.width 60.
                        NumericUpDown.minimum 0.
                        NumericUpDown.maximum data.MaxPreviewStart.Minutes
                        NumericUpDown.formatString "0"
                        NumericUpDown.showButtonSpinner false
                        FixedNumericUpDown.value previewStart.Minutes
                        FixedNumericUpDown.onValueChanged (Minutes >> SetPreviewStartTime >> EditProject >> dispatch)
                    ]

                    TextBlock.create [ TextBlock.text "m"; TextBlock.verticalAlignment VerticalAlignment.Center ]

                    // Seconds
                    FixedNumericUpDown.create [
                        NumericUpDown.width 60.
                        NumericUpDown.minimum 0.
                        NumericUpDown.maximum (float maxSecs)
                        NumericUpDown.formatString "00"
                        NumericUpDown.showButtonSpinner false
                        FixedNumericUpDown.value previewStart.Seconds
                        FixedNumericUpDown.onValueChanged (Seconds >> SetPreviewStartTime >> EditProject >> dispatch)
                    ]

                    TextBlock.create [ TextBlock.text "."; TextBlock.verticalAlignment VerticalAlignment.Center ]

                    // Milliseconds
                    FixedNumericUpDown.create [
                        NumericUpDown.width 80.
                        NumericUpDown.minimum 0.
                        NumericUpDown.maximum (float maxMs)
                        NumericUpDown.formatString "000"
                        NumericUpDown.showButtonSpinner false
                        FixedNumericUpDown.value previewStart.Milliseconds
                        FixedNumericUpDown.onValueChanged (Milliseconds >> SetPreviewStartTime >> EditProject >> dispatch)
                    ]

                    TextBlock.create [ TextBlock.text "s"; TextBlock.verticalAlignment VerticalAlignment.Center ]
                ]
            ]

            if data.MaxPreviewStart.TotalSeconds <= 0. then
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
