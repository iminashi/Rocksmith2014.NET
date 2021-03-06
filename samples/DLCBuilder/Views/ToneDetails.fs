﻿module DLCBuilder.Views.ToneDetails

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open System
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open DLCBuilder

let private createDescriptors dispatch (tone: Tone) =
    UniformGrid.create [
        Grid.column 1
        Grid.row 2
        UniformGrid.columns tone.ToneDescriptors.Length
        UniformGrid.children [
            for i = 0 to tone.ToneDescriptors.Length - 1 do
                yield ComboBox.create [
                    ComboBox.margin 4.
                    ComboBox.dataItems ToneDescriptor.all
                    ComboBox.itemTemplate Templates.toneDescriptor
                    ComboBox.selectedItem (ToneDescriptor.uiNameToDesc.[tone.ToneDescriptors.[i]])
                    ComboBox.onSelectedItemChanged (function
                        | :? ToneDescriptor as td -> 
                            ChangeDescriptor(i, td)
                            |> EditTone
                            |> dispatch
                        | _ ->
                            ())
                    ToolTip.tip (translate "toneDescriptorToolTip")
                ]
        ]
    ]

let view state dispatch (tone: Tone) =
    let keys =
        state.Project.Arrangements
        |> List.collect (function
            | Instrumental i ->
                i.BaseTone::i.Tones
            | _ ->
                List.empty)
        |> List.distinct

    Grid.create [
        Grid.columnDefinitions "auto,*"
        Grid.rowDefinitions "*,*,*,*,*"
        Grid.margin 4.
        Grid.children [
            // Key
            locText "key" [
                Grid.row 0
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            DockPanel.create [
                Grid.row 0
                Grid.column 1
                StackPanel.children [
                    // Remove key
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin 4.
                        Button.content "X"
                        Button.isEnabled (String.notEmpty tone.Key)
                        Button.onClick (fun _ -> String.Empty |> SetKey |> EditTone |> dispatch)
                    ]

                    ComboBox.create [
                        ComboBox.margin 4.
                        ComboBox.minHeight 26.
                        ComboBox.dataItems keys
                        ComboBox.selectedItem tone.Key
                        ComboBox.onSelectedItemChanged (function
                            | :? string as key ->
                                key |> SetKey |> EditTone |> dispatch
                            | _ ->
                                ())
                    ]
                ]
            ]

            // Name
            locText "name" [
                Grid.row 1
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            TextBox.create [
                Grid.row 1
                Grid.column 1
                TextBox.text tone.Name
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.onTextInput (fun arg -> arg.Text <- StringValidator.toneName arg.Text)
                TextBox.onLostFocus (fun arg ->
                    (arg.Source :?> TextBox).Text
                    |> StringValidator.toneName
                    |> SetName
                    |> EditTone
                    |> dispatch)
                ToolTip.tip (translate "toneNameToolTip")
            ]

            // Descriptors
            StackPanel.create [
                Grid.row 2
                StackPanel.children [
                    locText "description" [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                    ]

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.horizontalAlignment HorizontalAlignment.Center
                        StackPanel.children [
                            // Add description part
                            Button.create [
                                Button.isEnabled (tone.ToneDescriptors.Length < 3)
                                Button.margin 4.
                                Button.content "+"
                                Button.onClick (fun _ -> AddDescriptor |> EditTone |> dispatch)
                                ToolTip.tip (translate "addDescriptionToolTip")
                            ]
                            // Remove description part
                            Button.create [
                                Button.isEnabled (tone.ToneDescriptors.Length > 1)
                                Button.margin 4.
                                Button.content "-"
                                Button.onClick (fun _ -> RemoveDescriptor |> EditTone |> dispatch)
                                ToolTip.tip (translate "removeDescriptionToolTip")
                            ]
                        ]
                    ]
                ]
            ]
            createDescriptors dispatch tone

            // Volume
            locText "volume" [
                Grid.row 3
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            NumericUpDown.create [
                Grid.column 1
                Grid.row 3
                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                NumericUpDown.width 140.
                NumericUpDown.value tone.Volume
                NumericUpDown.minimum -45.
                NumericUpDown.maximum 45.
                NumericUpDown.increment 0.1
                NumericUpDown.formatString "F1"
                NumericUpDown.onValueChanged (SetVolume >> EditTone >> dispatch)
                ToolTip.tip (translate "toneVolumeToolTip")
            ]

            // Buttons (Edit & Export)
            Grid.create [
                Grid.row 4
                Grid.columnSpan 2
                Grid.columnDefinitions "*, *"
                Grid.children [
                    // Edit Button
                    Button.create [
                        Button.content (translate "edit")
                        Button.onClick (fun _ -> ShowToneEditor |> dispatch)
                        Button.margin 2.
                        Button.padding (8., 4.)
                    ]

                    // Export Button
                    Button.create [
                        Grid.column 1
                        Button.content (translate "export")
                        Button.onClick (fun _ -> ExportSelectedTone |> dispatch)
                        Button.margin 2.
                        Button.padding (8., 4.)
                    ]
                ]
            ]
        ]
    ]
