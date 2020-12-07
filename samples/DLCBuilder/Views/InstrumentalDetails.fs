module DLCBuilder.InstrumentalDetails

open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Controls
open Avalonia.Media
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System

let private fixPriority state routeMask arr =
    if arr.Priority = ArrangementPriority.Main
       && state.Project.Arrangements |> List.exists (function
            | Instrumental inst when inst <> arr ->
                inst.RouteMask = routeMask && inst.Priority = ArrangementPriority.Main
            | _ -> false) then
        ArrangementPriority.Alternative
    else
        arr.Priority

let view state dispatch (i: Instrumental) =
    Grid.create [
        //Grid.showGridLines true
        Grid.margin (0.0, 4.0)
        Grid.columnDefinitions "*,3*"
        Grid.rowDefinitions "*,*,*,*,*,*,*,*,*,*,*,*,*"
        Grid.children [
            TextBlock.create [
                TextBlock.isVisible (i.Name <> ArrangementName.Bass)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (state.Localization.GetString "name")
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]

            if i.Name <> ArrangementName.Bass then
                ComboBox.create [
                    Grid.column 1
                    ComboBox.horizontalAlignment HorizontalAlignment.Left
                    ComboBox.margin 4.
                    ComboBox.width 100.
                    ComboBox.dataItems [ ArrangementName.Lead; ArrangementName.Rhythm; ArrangementName.Combo ]
                    ComboBox.selectedItem i.Name
                    ComboBox.onSelectedItemChanged (fun item ->
                        let name = item :?> ArrangementName
                        fun state (a:Instrumental) ->
                            let routeMask =
                                match name with
                                | ArrangementName.Lead -> RouteMask.Lead
                                | ArrangementName.Rhythm -> RouteMask.Rhythm
                                | ArrangementName.Combo ->
                                    if a.RouteMask = RouteMask.Bass then RouteMask.Rhythm else a.RouteMask
                                | ArrangementName.Bass -> RouteMask.Bass
                                | _ -> failwith "Impossible failure."
                            let priority = fixPriority state routeMask a
                            { a with Name = name; RouteMask = routeMask; Priority = priority }
                        |> EditInstrumental |> dispatch)
                ]

            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "priority")
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
                            RadioButton.content (state.Localization.GetString(string priority))
                            RadioButton.isChecked (i.Priority = priority)
                            RadioButton.onChecked (fun _ ->
                                fun _ a -> { a with Priority = priority }
                                |> EditInstrumental
                                |> dispatch)
                            RadioButton.isEnabled (
                                // Disable the main option if a main arrangement of the type already exists
                                not (priority = ArrangementPriority.Main
                                     &&
                                     state.Project.Arrangements
                                     |> List.exists (function
                                         | Instrumental other -> i.RouteMask = other.RouteMask && other.Priority = ArrangementPriority.Main
                                         | _ -> false))
                            )
                        ]
                ]
            ]

            TextBlock.create [
                Grid.row 2
                TextBlock.isVisible (i.Name = ArrangementName.Combo)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "path")
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 2
                StackPanel.margin 4.
                StackPanel.orientation Orientation.Horizontal
                StackPanel.isVisible (i.Name = ArrangementName.Combo)
                if i.Name = ArrangementName.Combo then
                    StackPanel.children [
                        for mask in [ RouteMask.Lead; RouteMask.Rhythm ] ->
                            RadioButton.create [
                                RadioButton.margin (2.0, 0.0)
                                RadioButton.groupName "RouteMask"
                                RadioButton.content (string mask)
                                RadioButton.isChecked (i.RouteMask = mask)
                                RadioButton.onChecked (fun _ ->
                                    fun state a ->
                                        let priority = fixPriority state mask a
                                        { a with RouteMask = mask; Priority = priority }
                                    |> EditInstrumental |> dispatch)
                            ]
                    ]
            ]

            TextBlock.create [
                Grid.row 3
                TextBlock.isVisible (i.Name = ArrangementName.Bass)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "picked")
            ]

            CheckBox.create [
                Grid.column 1
                Grid.row 3
                CheckBox.margin 4.
                CheckBox.isVisible (i.Name = ArrangementName.Bass)
                CheckBox.isChecked i.BassPicked
                CheckBox.onChecked (fun _ ->
                    fun _ a -> { a with BassPicked = true }
                    |> EditInstrumental
                    |> dispatch)
                CheckBox.onUnchecked (fun _ ->
                    fun _ a -> { a with BassPicked = false }
                    |> EditInstrumental
                    |> dispatch)
            ]

            TextBlock.create [
                Grid.row 4
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "tuning")
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
                                    fun _ a ->
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
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "tuningPitch")
            ]

            StackPanel.create [
                Grid.column 1
                Grid.row 5
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    NumericUpDown.create [
                        NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                        NumericUpDown.width 90.
                        NumericUpDown.value i.TuningPitch
                        NumericUpDown.minimum 0.0
                        NumericUpDown.maximum 50000.0
                        NumericUpDown.increment 1.0
                        NumericUpDown.formatString "F2"
                        NumericUpDown.onValueChanged (fun value ->
                            fun _ a -> { a with TuningPitch = value }
                            |> EditInstrumental
                            |> dispatch)
                    ]
                    TextBlock.create [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.text (sprintf "%+.0f %s" (Utils.tuningPitchToCents i.TuningPitch) (state.Localization.GetString "cents"))
                    ]
                ]
            ]

            TextBlock.create [
                Grid.row 6
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "baseTone")
            ]

            TextBox.create [
                Grid.column 1
                Grid.row 6
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text i.BaseTone
                TextBox.onTextChanged (fun text ->
                    fun _ a -> { a with BaseTone = StringValidator.toneName text }
                    |> EditInstrumental
                    |> dispatch)
            ]

            TextBlock.create [
                Grid.row 7
                TextBlock.isVisible (i.Tones.Length > 0)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "tones")
            ]

            TextBlock.create [
                Grid.column 1
                Grid.row 7
                TextBlock.textWrapping TextWrapping.Wrap
                TextBlock.isVisible (i.Tones.Length > 0)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (String.Join(", ", i.Tones))
                TextBlock.horizontalAlignment HorizontalAlignment.Left
            ]

            Button.create [
                Grid.columnSpan 2
                Grid.row 8
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (state.Localization.GetString "updateToneNames")
                Button.onClick (fun _ ->
                    fun _ arr -> Arrangement.updateToneInfo arr true
                    |> EditInstrumental
                    |> dispatch)
                ToolTip.tip (state.Localization.GetString "updateToneNamesTooltip")
            ]

            TextBlock.create [
                Grid.row 9
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "scrollSpeed")
            ]

            NumericUpDown.create [
                Grid.column 1
                Grid.row 9
                ToolTip.tip (state.Localization.GetString "scrollSpeedTooltip")
                NumericUpDown.isVisible state.Config.ShowAdvanced
                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                NumericUpDown.increment 0.1
                NumericUpDown.width 65.
                NumericUpDown.maximum 5.0
                NumericUpDown.minimum 0.5
                NumericUpDown.formatString "F1"
                NumericUpDown.value i.ScrollSpeed
                NumericUpDown.onValueChanged (fun value ->
                    fun _ a -> { a with ScrollSpeed = value }
                    |> EditInstrumental
                    |> dispatch)
            ]

            TextBlock.create [
                Grid.row 10
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "masterID")
            ]

            TextBox.create [
                Grid.column 1
                Grid.row 10
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text (string i.MasterID)
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    let success, masterID = Int32.TryParse(txtBox.Text)
                    if success then
                        fun _ (a:Instrumental) -> { a with MasterID = masterID }
                        |> EditInstrumental
                        |> dispatch
                )
            ]

            TextBlock.create [
                Grid.row 11
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (state.Localization.GetString "persistentID")
            ]

            TextBox.create [
                Grid.column 1
                Grid.row 11
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text (i.PersistentID.ToString("N"))
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    let success, perID = Guid.TryParse(txtBox.Text)
                    if success then
                        fun _ (a:Instrumental) -> { a with PersistentID = perID }
                        |> EditInstrumental
                        |> dispatch
                )
            ]

            Button.create [
                Grid.columnSpan 2
                Grid.row 12
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.isVisible state.Config.ShowAdvanced
                Button.content (state.Localization.GetString "generateNewArrIDs")
                Button.onClick (fun _ -> 
                    fun _ (a: Instrumental) ->
                        { a with MasterID = RandomGenerator.next()
                                 PersistentID = Guid.NewGuid() }
                    |> EditInstrumental |> dispatch
                )
                ToolTip.tip (state.Localization.GetString "generateNewArrIDsToolTip")
            ]
        ]
    ]