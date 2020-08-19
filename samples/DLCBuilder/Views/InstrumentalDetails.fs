module DLCBuilder.InstrumentalDetails

open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Controls
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System

let view (state: State) dispatch (i: Instrumental) =
    Grid.create [
        //Grid.showGridLines true
        Grid.margin (0.0, 4.0)
        Grid.columnDefinitions "*,3*"
        Grid.rowDefinitions "*,*,*,*,*,*,*,*,*,*,*,*,*"
        Grid.children [
            TextBlock.create [
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Name:"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            ComboBox.create [
                Grid.column 1
                ComboBox.horizontalAlignment HorizontalAlignment.Left
                ComboBox.margin 4.
                ComboBox.width 100.
                ComboBox.dataItems (Enum.GetValues(typeof<ArrangementName>))
                ComboBox.selectedItem i.Name
                ComboBox.onSelectedItemChanged (fun item ->
                    match item with
                    | :? ArrangementName as item ->
                        fun (a:Instrumental) ->
                            let routeMask =
                                match item with
                                | ArrangementName.Lead -> RouteMask.Lead
                                | ArrangementName.Rhythm | ArrangementName.Combo -> RouteMask.Rhythm
                                | ArrangementName.Bass -> RouteMask.Bass
                                | _ -> failwith "Unlikely failure."
                            { a with Name = item; RouteMask = routeMask }
                        |> EditInstrumental |> dispatch
                    | _ -> ()
                )
            ]

            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Priority:"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 1
                StackPanel.orientation Orientation.Horizontal
                StackPanel.margin 4.
                StackPanel.children [
                    for priority in [ ArrangementPriority.Main; ArrangementPriority.Alternative; ArrangementPriority.Bonus ] ->
                        RadioButton.create [
                            RadioButton.margin (2.0, 0.0)
                            RadioButton.groupName "Priority"
                            RadioButton.content (string priority)
                            RadioButton.isChecked (i.Priority = priority)
                            RadioButton.onChecked (fun _ -> (fun a -> { a with Priority = priority }) |> EditInstrumental |> dispatch)
                        ]
                ]
            ]

            TextBlock.create [
                Grid.row 2
                TextBlock.isVisible (i.Name = ArrangementName.Combo)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Path:"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 2
                StackPanel.margin 4.
                StackPanel.orientation Orientation.Horizontal
                StackPanel.isVisible (i.Name = ArrangementName.Combo)
                StackPanel.children [
                    for mask in [ RouteMask.Lead; RouteMask.Rhythm ] ->
                        RadioButton.create [
                            RadioButton.margin (2.0, 0.0)
                            RadioButton.groupName "RouteMask"
                            RadioButton.content (string mask)
                            RadioButton.isChecked (i.RouteMask = mask)
                            RadioButton.onChecked (fun _ -> (fun a -> { a with RouteMask = mask }) |> EditInstrumental |> dispatch)
                        ]
                ]
            ]

            TextBlock.create [
                Grid.row 3
                TextBlock.isVisible (i.Name = ArrangementName.Bass)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Picked:"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            CheckBox.create [
                Grid.column 1
                Grid.row 3
                CheckBox.margin 4.
                CheckBox.isVisible (i.Name = ArrangementName.Bass)
                CheckBox.isChecked i.BassPicked
                CheckBox.onChecked (fun _ -> (fun a -> { a with BassPicked = true }) |> EditInstrumental |> dispatch)
                CheckBox.onUnchecked (fun _ -> (fun a -> { a with BassPicked = false }) |> EditInstrumental |> dispatch)
            ]

            TextBlock.create [
                Grid.row 4
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Tuning:"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 4
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    for str in 0..5 ->
                        TextBox.create [
                            TextBox.width 30.
                            TextBox.text (string i.Tuning.[str])
                            TextBox.onLostFocus (fun arg ->
                                let txtBox = arg.Source :?> TextBox
                                let success, newTuning = Int16.TryParse(txtBox.Text)
                                if success then
                                    fun a ->
                                        let tuning =
                                            a.Tuning
                                            |> Array.mapi (fun i old -> if i = str then newTuning else old)
                                        { a with Tuning = tuning }
                                    |> EditInstrumental |> dispatch
                            )
                        ]
                ]
            ]

            TextBlock.create [
                Grid.row 5
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Cent Offset:"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            NumericUpDown.create [
                Grid.column 1
                Grid.row 5
                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                NumericUpDown.width 65.
                NumericUpDown.value (float i.CentOffset)
                NumericUpDown.minimum -5000.0
                NumericUpDown.maximum 5000.0
                NumericUpDown.increment 1.0
                NumericUpDown.formatString "F0"
                NumericUpDown.onValueChanged (fun value -> (fun a -> { a with CentOffset = int value }) |> EditInstrumental |> dispatch)
            ]

            TextBlock.create [
                Grid.row 6
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Base Tone:"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            TextBox.create [
                Grid.column 1
                Grid.row 6
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text i.BaseTone
                TextBox.onTextChanged (fun text -> (fun a -> { a with BaseTone = text }) |> EditInstrumental |> dispatch)
            ]

            TextBlock.create [
                Grid.row 7
                TextBlock.isVisible (i.Tones.Length > 0)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Tones:"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            TextBlock.create [
                Grid.column 1
                Grid.row 7
                TextBlock.isVisible (i.Tones.Length > 0)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (String.Join(", ", i.Tones))
                TextBlock.horizontalAlignment HorizontalAlignment.Left
            ]

            TextBlock.create [
                Grid.row 8
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Scroll Speed:"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            NumericUpDown.create [
                Grid.column 1
                Grid.row 8
                NumericUpDown.isVisible state.Config.ShowAdvanced
                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                NumericUpDown.increment 0.1
                NumericUpDown.width 65.
                NumericUpDown.maximum 5.0
                NumericUpDown.minimum 0.5
                NumericUpDown.formatString "F1"
                NumericUpDown.value i.ScrollSpeed
                NumericUpDown.onValueChanged (fun value -> (fun a -> { a with ScrollSpeed = value }) |> EditInstrumental |> dispatch)
            ]

            TextBlock.create [
                Grid.row 9
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Master ID:"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            TextBox.create [
                Grid.column 1
                Grid.row 9
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text (string i.MasterID)
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    let success, masterID = Int32.TryParse(txtBox.Text)
                    if success then
                        (fun (a:Instrumental) -> { a with MasterID = masterID }) |> EditInstrumental |> dispatch
                )
            ]

            TextBlock.create [
                Grid.row 10
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text "Persistent ID:"
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            TextBox.create [
                Grid.column 1
                Grid.row 10
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text (i.PersistentID.ToString("N"))
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    let success, perID = Guid.TryParse(txtBox.Text)
                    if success then
                        (fun (a:Instrumental) -> { a with PersistentID = perID }) |> EditInstrumental |> dispatch
                )
            ]

            Button.create [
                Grid.columnSpan 2
                Grid.row 11
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.isVisible state.Config.ShowAdvanced
                Button.content "Generate New Arrangement Identification"
                Button.onClick (fun _ -> 
                    fun (a: Instrumental) ->
                        { a with MasterID = RandomGenerator.next()
                                 PersistentID = Guid.NewGuid() }
                    |> EditInstrumental |> dispatch
                )
                ToolTip.tip "Generates new identification IDs for this arrangement.\nThe in-game stats for the arrangement will be reset."
            ]
        ]
    ]