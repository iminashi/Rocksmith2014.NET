module DLCBuilder.Views.PitchShifter

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder
open Rocksmith2014.DLCProject

let view dispatch state =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [          
            // Explanation
            TextBlock.create [
                TextBlock.fontSize 16.
                TextBlock.text (translate "pitchShifterExplanation")
                TextBlock.margin 10.0
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.text (translate "shiftTuningBy")
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
                            dispatch BuildPitchShifted)
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
