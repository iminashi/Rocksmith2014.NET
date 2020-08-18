module DLCBuilder.ToneDetails

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI
open Avalonia.Layout
open Rocksmith2014.Common.Manifest

let view state dispatch (tone: Tone) =
    Grid.create [
        Grid.columnDefinitions "*,3*"
        Grid.rowDefinitions "*,*,*,*"
        Grid.margin (0.0, 4.0)
        Grid.children [
            TextBlock.create [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text "Name:"
            ]
            TextBox.create [
                Grid.column 1
                TextBox.text tone.Name
                TextBox.onTextChanged (fun name -> (fun (t:Tone) -> { t with Name = name }) |> EditTone |> dispatch)
            ]

            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text "Description:"
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 1
                StackPanel.children [
                    UniformGrid.create [
                        UniformGrid.columns tone.ToneDescriptors.Length
                        UniformGrid.children [
                            for i = 0 to tone.ToneDescriptors.Length - 1 do
                                yield ComboBox.create [
                                    ComboBox.margin 4.
                                    ComboBox.dataItems ToneDescriptor.all
                                    ComboBox.selectedItem (ToneDescriptor.uiNameToDesc.[tone.ToneDescriptors.[i]])
                                    ComboBox.onSelectedItemChanged (fun item ->
                                        match item with
                                        | :? ToneDescriptor as td ->
                                            fun t ->
                                                let updated =
                                                    t.ToneDescriptors
                                                    |> Array.mapi (fun j x -> if j = i then td.UIName else x)
                                                { t with ToneDescriptors = updated }
                                            |> EditTone |> dispatch
                                        | _ -> ()
                                    )
                                ]
                        ]
                    ]
                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            Button.create [
                                Button.isEnabled (tone.ToneDescriptors.Length < 3)
                                Button.margin 4.
                                Button.content "+"
                                Button.onClick (fun _ -> (fun t -> { t with ToneDescriptors = t.ToneDescriptors |> Array.append [| ToneDescriptor.all.[0].UIName |] }) |> EditTone |> dispatch)
                                ToolTip.tip "Click to add a description part."
                            ]
                            Button.create [
                                Button.isEnabled (tone.ToneDescriptors.Length > 1)
                                Button.margin 4.
                                Button.content "-"
                                Button.onClick (fun _ -> (fun t -> { t with ToneDescriptors = t.ToneDescriptors.[1..] }) |> EditTone |> dispatch)
                                ToolTip.tip "Click to remove a description part."
                            ]
                        ]
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 2
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text "Volume:"
            ]
            NumericUpDown.create [
                Grid.column 1
                Grid.row 2
                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                NumericUpDown.width 75.
                NumericUpDown.value (float tone.Volume)
                NumericUpDown.minimum -45.
                NumericUpDown.maximum 45.
                NumericUpDown.increment 0.1
                NumericUpDown.formatString "F1"
                NumericUpDown.onValueChanged (fun value -> (fun (t:Tone) -> { t with Volume = sprintf "%.3f" value }) |> EditTone |> dispatch)
            ]
        ]
    ]
