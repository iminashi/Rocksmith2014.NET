module DLCBuilder.Views.ToneDetails

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open DLCBuilder

let createDescriptors dispatch (tone: Tone) =
    UniformGrid.create [
        UniformGrid.columns tone.ToneDescriptors.Length
        UniformGrid.children [
            for i = 0 to tone.ToneDescriptors.Length - 1 do
                yield ComboBox.create [
                    ComboBox.margin 4.
                    ComboBox.dataItems ToneDescriptor.all
                    ComboBox.itemTemplate Templates.toneDescriptor
                    ComboBox.selectedItem (ToneDescriptor.uiNameToDesc.[tone.ToneDescriptors.[i]])
                    ComboBox.onSelectedItemChanged (function
                        | :? ToneDescriptor as td -> ChangeDescriptor(i, td) |> EditTone |> dispatch
                        | _ -> ()
                    )
                    ToolTip.tip (translate "toneDescriptorToolTip")
                ]
        ]
    ]

let view state dispatch (tone: Tone) =
    let keys =
        state.Project.Arrangements
        |> List.collect (fun x ->
            [ match x with
              | Instrumental i ->
                  yield i.BaseTone
                  yield! i.Tones
              | _ -> () ])
        |> List.distinct

    Grid.create [
        Grid.columnDefinitions "auto,*"
        Grid.rowDefinitions "*,*,*,*,*"
        Grid.margin (0.0, 4.0)
        Grid.children [
            // Name
            TextBlock.create [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "name")
            ]
            TextBox.create [
                Grid.column 1
                TextBox.text tone.Name
                TextBox.onTextInput (fun arg -> arg.Text <- StringValidator.toneName arg.Text)
                TextBox.onTextChanged (StringValidator.toneName >> SetName >> EditTone >> dispatch)
            ]

            // Key
            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "key")
            ]
            ComboBox.create [
                Grid.row 1
                Grid.column 1
                ComboBox.margin 4.
                ComboBox.minHeight 26.
                ComboBox.dataItems keys
                ComboBox.selectedItem tone.Key
                ComboBox.onSelectedItemChanged (function
                    | :? string as key -> key |> (SetKey >> EditTone >> dispatch)
                    | _ -> ()
                )
            ]

            // Descriptors
            TextBlock.create [
                Grid.row 2
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "description")
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 2
                StackPanel.children [
                    createDescriptors dispatch tone
                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            Button.create [
                                Button.isEnabled (tone.ToneDescriptors.Length < 3)
                                Button.margin 4.
                                Button.content "+"
                                Button.onClick (fun _ -> AddDescriptor |> EditTone |> dispatch)
                                ToolTip.tip (translate "addDescriptionToolTip")
                            ]
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

            // Volume
            TextBlock.create [
                Grid.row 3
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "volume")
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

            // Buttons
            Grid.create [
                Grid.row 4
                Grid.columnSpan 2
                Grid.columnDefinitions "*, *"
                Grid.children [
                    Button.create [
                        Button.content (translate "edit")
                        Button.onClick (fun _ -> ShowToneEditor |> dispatch)
                        Button.margin 2.
                        Button.padding (8., 4.)
                    ]

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
