module DLCBuilder.Views.InstrumentalDetails

open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Media
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open DLCBuilder

let view state dispatch (i: Instrumental) =
    Grid.create [
        //Grid.showGridLines true
        Grid.margin 4.
        Grid.columnDefinitions "auto,*"
        Grid.rowDefinitions "*,*,*,*,*,*,*,*,*,*,*,*,*"
        Grid.children [
            // Arrangement name (for non-bass arrangements)
            TextBlock.create [
                TextBlock.isVisible (i.Name <> ArrangementName.Bass)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.text (translate "name")
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            ComboBox.create [
                Grid.column 1
                ComboBox.isVisible (i.Name <> ArrangementName.Bass)
                ComboBox.horizontalAlignment HorizontalAlignment.Left
                ComboBox.margin 4.
                ComboBox.width 100.
                ComboBox.dataItems [ ArrangementName.Lead; ArrangementName.Rhythm; ArrangementName.Combo ]
                ComboBox.selectedItem i.Name
                ComboBox.onSelectedItemChanged (function
                    | :? ArrangementName as name -> name |> SetArrangementName |> EditInstrumental |> dispatch
                    | _ -> ())
            ]

            // Priority
            TextBlock.create [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "priority")
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 1
                StackPanel.orientation Orientation.Horizontal
                StackPanel.margin 4.
                StackPanel.children [
                    for priority in [ ArrangementPriority.Main; ArrangementPriority.Alternative; ArrangementPriority.Bonus ] ->
                        RadioButton.create [
                            RadioButton.margin (4.0, 0.0)
                            RadioButton.minWidth 0.
                            RadioButton.groupName "Priority"
                            RadioButton.content (translate(string priority))
                            RadioButton.isChecked (i.Priority = priority)
                            RadioButton.onChecked (fun _ -> priority |> SetPriority |> EditInstrumental |> dispatch)
                            RadioButton.isEnabled (
                                // Disable the main option if a main arrangement of the type already exists
                                i.Priority = priority
                                ||
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

            // Path (only for combo arrangements)
            TextBlock.create [
                Grid.row 2
                TextBlock.isVisible (i.Name = ArrangementName.Combo)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "path")
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
                                RadioButton.onChecked (fun _ -> mask |> SetRouteMask |> EditInstrumental |> dispatch)
                            ]
                    ]
            ]

            // Bass pick
            TextBlock.create [
                Grid.row 3
                TextBlock.isVisible (i.Name = ArrangementName.Bass)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "picked")
            ]
            CheckBox.create [
                Grid.column 1
                Grid.row 3
                CheckBox.margin 4.
                CheckBox.isVisible (i.Name = ArrangementName.Bass)
                CheckBox.isChecked i.BassPicked
                CheckBox.onChecked (fun _ -> true |> SetBassPicked |> EditInstrumental |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetBassPicked |> EditInstrumental |> dispatch)
            ]

            // Tuning strings
            TextBlock.create [
                Grid.row 4
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "tuning")
            ]
            UniformGrid.create [
                Grid.column 1
                Grid.row 4
                UniformGrid.columns 6
                UniformGrid.horizontalAlignment HorizontalAlignment.Left
                UniformGrid.children [
                    for str in 0..5 ->
                        TextBox.create [
                            TextBox.margin 2.
                            TextBox.minWidth 40.
                            TextBox.width 40.
                            TextBox.text (string i.Tuning.[str])
                            TextBox.onLostFocus (fun arg ->
                                let txtBox = arg.Source :?> TextBox
                                match Int16.TryParse txtBox.Text with
                                | true, newTuning -> SetTuning (str, newTuning) |> EditInstrumental |> dispatch
                                | false, _ -> ())
                        ]
                ]
            ]

            // Tuning Pitch
            TextBlock.create [
                Grid.row 5
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "tuningPitch")
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 5
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    NumericUpDown.create [
                        NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                        NumericUpDown.width 160.
                        NumericUpDown.value i.TuningPitch
                        NumericUpDown.minimum 0.0
                        NumericUpDown.maximum 50000.0
                        NumericUpDown.increment 1.0
                        NumericUpDown.formatString "F2"
                        NumericUpDown.onValueChanged (SetTuningPitch >> EditInstrumental >> dispatch)
                    ]
                    TextBlock.create [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.text (sprintf "%+.0f %s" (Utils.tuningPitchToCents i.TuningPitch) (translate "cents"))
                    ]
                ]
            ]

            // Base Tone
            TextBlock.create [
                Grid.row 6
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "baseTone")
            ]
            TextBox.create [
                Grid.column 1
                Grid.row 6
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text i.BaseTone
                TextBox.onTextChanged (StringValidator.toneName >> SetBaseTone >> EditInstrumental >> dispatch)
            ]

            // Tone key list
            TextBlock.create [
                Grid.row 7
                TextBlock.isVisible (i.Tones.Length > 0)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "tones")
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

            // Scroll speed
            TextBlock.create [
                Grid.row 8
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "scrollSpeed")
            ]
            NumericUpDown.create [
                Grid.column 1
                Grid.row 8
                ToolTip.tip (translate "scrollSpeedTooltip")
                NumericUpDown.isVisible state.Config.ShowAdvanced
                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                NumericUpDown.increment 0.1
                NumericUpDown.maximum 5.0
                NumericUpDown.minimum 0.5
                NumericUpDown.formatString "F1"
                NumericUpDown.value i.ScrollSpeed
                NumericUpDown.onValueChanged (SetScrollSpeed >> EditInstrumental >> dispatch)
            ]

            // Master ID
            TextBlock.create [
                Grid.row 9
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "masterID")
            ]
            TextBox.create [
                Grid.column 1
                Grid.row 9
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text (string i.MasterID)
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    match Int32.TryParse txtBox.Text with
                    | true, masterId -> SetMasterId masterId |> EditInstrumental |> dispatch
                    | false, _ -> ()
                )
            ]

            // Persistent ID
            TextBlock.create [
                Grid.row 10
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "persistentID")
            ]
            TextBox.create [
                Grid.column 1
                Grid.row 10
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                TextBox.text (i.PersistentID.ToString("N"))
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    match Guid.TryParse txtBox.Text with
                    | true, id -> SetPersistentId id |> EditInstrumental |> dispatch
                    | false, _ -> ()
                )
            ]

            // Custom audio file
            TextBlock.create [
                Grid.row 11
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (translate "customAudioFile")
            ]
            DockPanel.create [
                Grid.column 1
                Grid.row 11
                DockPanel.isVisible state.Config.ShowAdvanced
                DockPanel.children [
                    // Select audio file
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.content "..."
                        Button.margin (0., 2., 0., 0.)
                        Button.onClick (fun _ ->
                            Msg.OpenFileDialog("selectAudioFile", Dialogs.audioFileFilters, Some >> SetCustomAudioPath >> EditInstrumental)
                            |> dispatch)
                    ]

                    // Remove audio file
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0., 2., 2., 0.)
                        Button.content "X"
                        Button.isVisible i.CustomAudio.IsSome
                        Button.onClick (fun _ -> None |> SetCustomAudioPath |> EditInstrumental |> dispatch)
                        ToolTip.tip (translate "removeCustomAudioTooltip")
                    ]

                    // Convert to wem
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0., 2., 2., 0.)
                        Button.content "W"
                        Button.isEnabled (not <| state.RunningTasks.Contains WemConversion)
                        Button.isVisible (
                            match i.CustomAudio with
                            | Some audio when not <| String.endsWith ".wem" audio.Path -> true
                            | _ -> false)
                        Button.onClick (fun _ -> ConvertToWemCustom |> dispatch)
                        ToolTip.tip (translate "convertToWemTooltip")
                    ]

                    // Audio file path
                    TextBlock.create [
                        TextBlock.margin (4., 2., 0., 0.)
                        TextBlock.text (
                            Option.map (fun x -> IO.Path.GetFileName x.Path) i.CustomAudio
                            |> Option.defaultValue (translate "noAudioFile")
                        )
                    ]
                ]
            ]

            // Custom audio volume
            if state.Config.ShowAdvanced && i.CustomAudio.IsSome then
                TextBlock.create [
                    Grid.row 12
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.text (translate "volume")
                ]
                NumericUpDown.create [
                    Grid.column 1
                    Grid.row 12
                    NumericUpDown.isEnabled (
                        match i.CustomAudio with
                        | Some audio when state.RunningTasks.Contains(VolumeCalculation(CustomAudio(audio.Path))) ->
                            false
                        | _ ->
                            true)
                    NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                    NumericUpDown.minimum -45.
                    NumericUpDown.maximum 45.
                    NumericUpDown.increment 0.5
                    NumericUpDown.value (
                        i.CustomAudio
                        |> Option.map (fun x -> x.Volume)
                        |> Option.defaultValue state.Project.AudioFile.Volume)
                    NumericUpDown.formatString "F1"
                    NumericUpDown.onValueChanged (SetCustomAudioVolume >> EditInstrumental >> dispatch)
                    ToolTip.tip (translate "audioVolumeToolTip")
                ]
        ]
    ]
