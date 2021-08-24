module DLCBuilder.Views.ToneDetails

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open System
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
                yield FixedComboBox.create [
                    ComboBox.margin 4.
                    ComboBox.dataItems ToneDescriptor.all
                    ComboBox.itemTemplate Templates.toneDescriptor
                    FixedComboBox.selectedItem (ToneDescriptor.uiNameToDesc.[tone.ToneDescriptors.[i]])
                    FixedComboBox.onSelectedItemChanged (function
                        | :? ToneDescriptor as td ->
                            ChangeDescriptor(i, td)
                            |> EditTone
                            |> dispatch
                        | _ ->
                            ())
                    ToolTip.tip (translate "ToneDescriptorToolTip")
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
        |> List.filter String.notEmpty
        |> List.distinct

    Grid.create [
        Grid.columnDefinitions "auto,*"
        Grid.rowDefinitions "*,*,*,*,*"
        Grid.margin 4.
        Grid.children [
            // Key
            locText "Key" [
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
                        ToolTip.tip (translate "RemoveKeyToolTip")
                    ]

                    FixedComboBox.create [
                        ComboBox.margin 4.
                        ComboBox.minHeight 26.
                        FixedComboBox.dataItems keys
                        FixedComboBox.selectedItem tone.Key
                        FixedComboBox.onSelectedItemChanged (function
                            | :? string as key ->
                                key |> SetKey |> EditTone |> dispatch
                            | _ ->
                                ())
                    ]
                ]
            ]

            // Name
            locText "Name" [
                Grid.row 1
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            FixedTextBox.create [
                Grid.row 1
                Grid.column 1
                FixedTextBox.text tone.Name
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.onTextInput (fun arg -> arg.Text <- StringValidator.toneName arg.Text)
                FixedTextBox.onTextChanged (StringValidator.toneName >> SetName >> EditTone >> dispatch)
                ToolTip.tip (translate "ToneNameToolTip")
            ]

            // Descriptors
            StackPanel.create [
                Grid.row 2
                StackPanel.children [
                    locText "Description" [
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
                                ToolTip.tip (translate "AddDescriptionToolTip")
                            ]
                            // Remove description part
                            Button.create [
                                Button.isEnabled (tone.ToneDescriptors.Length > 1)
                                Button.margin 4.
                                Button.content "-"
                                Button.onClick (fun _ -> RemoveDescriptor |> EditTone |> dispatch)
                                ToolTip.tip (translate "RemoveDescriptionToolTip")
                            ]
                        ]
                    ]
                ]
            ]
            createDescriptors dispatch tone

            // Volume
            locText "Volume" [
                Grid.row 3
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            DockPanel.create [
                Grid.column 1
                Grid.row 3
                DockPanel.children [
                    FixedNumericUpDown.create [
                        DockPanel.dock Dock.Left
                        NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                        NumericUpDown.verticalAlignment VerticalAlignment.Center
                        NumericUpDown.width 140.
                        NumericUpDown.minimum 0.1
                        NumericUpDown.maximum 36.
                        NumericUpDown.increment 0.1
                        NumericUpDown.formatString "F1"
                        FixedNumericUpDown.value (-tone.Volume)
                        FixedNumericUpDown.onValueChanged (SetVolume >> EditTone >> dispatch)
                    ]

                    Grid.create [
                        Grid.columnDefinitions "auto,*,auto"
                        Grid.children [
                            Path.create [
                                Grid.column 0
                                Path.data Media.Icons.volumeLow
                                Path.fill Brushes.Gray
                                Path.verticalAlignment VerticalAlignment.Center
                                Path.margin (4., 0.)
                            ]

                            FixedSlider.create [
                                Grid.column 1
                                Slider.minimum 0.1
                                Slider.maximum 36.
                                FixedSlider.value (-tone.Volume)
                                FixedSlider.onValueChanged (SetVolume >> EditTone >> dispatch)
                            ]

                            Path.create [
                                Grid.column 2
                                Path.data Media.Icons.volumeHigh
                                Path.fill Brushes.Gray
                                Path.verticalAlignment VerticalAlignment.Center
                                Path.margin (4., 0.)
                            ]
                        ]
                    ]
                ]
            ]

            // Buttons (Edit & Export)
            Grid.create [
                Grid.row 4
                Grid.columnSpan 2
                Grid.columnDefinitions "*, *"
                Grid.children [
                    // Edit Button
                    Button.create [
                        Button.content (translate "Edit")
                        Button.onClick (fun _ -> ShowToneEditor |> dispatch)
                        Button.margin 2.
                        Button.padding (8., 4.)
                    ]

                    // Export Button
                    Button.create [
                        Grid.column 1
                        Button.content (translate "Export")
                        Button.onClick (fun _ -> ExportSelectedTone |> dispatch)
                        Button.margin 2.
                        Button.padding (8., 4.)
                    ]
                ]
            ]
        ]
    ]
