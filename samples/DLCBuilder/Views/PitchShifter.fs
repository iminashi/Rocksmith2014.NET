module DLCBuilder.Views.PitchShifter

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Layout
open DLCBuilder
open Rocksmith2014.DLCProject

let view dispatch state =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            // Explanation
            locText "pitchShifterExplanation" [
                TextBlock.fontSize 16.
                TextBlock.margin 10.0
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    // Shift tuning by
                    locText "shiftTuningBy" [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                    ]
                    NumericUpDown.create [
                        NumericUpDown.minimum -24.
                        NumericUpDown.maximum 24.
                        NumericUpDown.increment 1.
                        NumericUpDown.formatString "+0;-0;0"
                        NumericUpDown.value (state.Project.PitchShift |> Option.defaultValue 0s |> float)
                        NumericUpDown.onValueChanged (int16 >> SetPitchShift >> EditProject >> dispatch)
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
                        Button.content (translate "create")
                        Button.isEnabled (state.Project.PitchShift |> Option.exists ((<>) 0s))
                        Button.onClick (fun _ ->
                            dispatch CloseOverlay
                            dispatch (Build PitchShifted))
                    ]

                    // Cancel button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "cancel")
                        Button.onClick (fun _ -> dispatch CloseOverlay)
                    ]
                ]
            ]
        ]
    ] :> IView
