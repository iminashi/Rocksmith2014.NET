module DLCBuilder.Views.PitchShifter

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Rocksmith2014.DLCProject
open DLCBuilder

let view dispatch state =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            // Explanation
            locText "PitchShifterExplanation" [
                TextBlock.fontSize 16.
                TextBlock.margin 10.0
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    // Shift tuning by
                    locText "ShiftTuningBy" [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                    ]
                    FixedNumericUpDown.create [
                        NumericUpDown.minimum -24.
                        NumericUpDown.maximum 24.
                        NumericUpDown.increment 1.
                        NumericUpDown.formatString "+0;-0;0"
                        FixedNumericUpDown.value (state.Project.PitchShift |> Option.defaultValue 0s |> float)
                        FixedNumericUpDown.onValueChanged (int16 >> SetPitchShift >> EditProject >> dispatch)
                    ]
                ]
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.children [
                    // Create button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "Create")
                        Button.isEnabled (state.Project.PitchShift |> Option.exists ((<>) 0s))
                        Button.onClick (fun _ ->
                            dispatch (CloseOverlay OverlayCloseMethod.OverlayButton)
                            dispatch (Build PitchShifted))
                    ]

                    // Cancel button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "Cancel")
                        Button.onClick (fun _ -> dispatch (CloseOverlay OverlayCloseMethod.OverlayButton))
                    ]
                ]
            ]
        ]
    ] |> generalize
