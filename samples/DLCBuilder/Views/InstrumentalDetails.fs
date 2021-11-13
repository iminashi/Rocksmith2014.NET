module DLCBuilder.Views.InstrumentalDetails

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open DLCBuilder

let private enablePriorityChange arrangements priority inst =
    // Disable the main option if a main arrangement of the type already exists
    inst.Priority = priority
    || not (
        priority = ArrangementPriority.Main
        && arrangements
           |> List.exists (function
               | Instrumental other ->
                   inst.RouteMask = other.RouteMask
                   && other.Priority = ArrangementPriority.Main
               | _ ->
                   false)
    )

let private isNumberGreaterThanZero (input: string) =
    let parsed, number = Int32.TryParse(input)
    parsed && number > 0

let private tuningTextBox dispatch (tuning: int16 array) stringIndex =
    FixedTextBox.create [
        TextBox.margin (2., 0.)
        TextBox.minWidth 40.
        TextBox.width 40.
        FixedTextBox.text (string tuning[stringIndex])
        FixedTextBox.onTextChanged (fun text ->
            match Int16.TryParse(text) with
            | true, newTuning ->
                SetTuning(stringIndex, newTuning)
                |> EditInstrumental
                |> dispatch
            | false, _ ->
                ())
        TextBox.onKeyUp (fun arg ->
            match arg.Key with
            | Key.Down
            | Key.Up ->
                arg.Handled <- true
                let dir = if arg.Key = Key.Down then Down else Up
                ChangeTuning(stringIndex, dir)
                |> EditInstrumental
                |> dispatch
            | _ ->
                ())
    ] |> generalize

let private tuningChangeRepeatButton dispatch direction =
    RepeatButton.create [
        if direction = Down then Grid.row 1
        RepeatButton.classes [ "updown-btn" ]
        RepeatButton.verticalAlignment VerticalAlignment.Stretch
        RepeatButton.content (
            PathIcon.create [
                PathIcon.width 12.
                PathIcon.height 12.
                PathIcon.data (if direction = Up then Media.Icons.chevronUp else Media.Icons.chevronDown)
            ])
        RepeatButton.onClick (fun _ ->
            ChangeTuningAll direction
            |> EditInstrumental
            |> dispatch
        )
    ]

let view state dispatch (inst: Instrumental) =
    Grid.create [
        Grid.margin 6.
        Grid.columnDefinitions "auto,*"
        Grid.rowDefinitions "*,*,*,*,*,*,*,*,*,*,*,*,*"
        Grid.children [
            // Arrangement name (for non-bass arrangements)
            locText "Name" [
                TextBlock.isVisible (inst.Name <> ArrangementName.Bass)
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.verticalAlignment VerticalAlignment.Center
            ]
            FixedComboBox.create [
                Grid.column 1
                ComboBox.isVisible (inst.Name <> ArrangementName.Bass)
                ComboBox.horizontalAlignment HorizontalAlignment.Left
                ComboBox.margin (4., 0.)
                ComboBox.width 140.
                ComboBox.dataItems [ ArrangementName.Lead; ArrangementName.Rhythm; ArrangementName.Combo ]
                ComboBox.itemTemplate Templates.arrangementName
                FixedComboBox.selectedItem inst.Name
                FixedComboBox.onSelectedItemChanged (function
                    | :? ArrangementName as name ->
                        name |> SetArrangementName |> EditInstrumental |> dispatch
                    | _ ->
                        ())
            ]

            // Priority
            locText "Priority"  [
                Grid.row 1
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 1
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    for priority in [ ArrangementPriority.Main; ArrangementPriority.Alternative; ArrangementPriority.Bonus ] ->
                        RadioButton.create [
                            RadioButton.margin (4.0, 0.0)
                            RadioButton.minWidth 0.
                            RadioButton.groupName "Priority"
                            RadioButton.content (translate(string priority))
                            RadioButton.isChecked (inst.Priority = priority)
                            RadioButton.onClick (fun _ -> priority |> SetPriority |> EditInstrumental |> dispatch)
                            RadioButton.isEnabled (enablePriorityChange state.Project.Arrangements priority inst)
                        ]
                ]
            ]

            // Path (only for combo arrangements)
            locText "Path" [
                Grid.row 2
                TextBlock.isVisible (inst.Name = ArrangementName.Combo)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 2
                StackPanel.margin (4.0, 0.0)
                StackPanel.orientation Orientation.Horizontal
                StackPanel.isVisible (inst.Name = ArrangementName.Combo)
                if inst.Name = ArrangementName.Combo then
                    StackPanel.children [
                        for mask in [ RouteMask.Lead; RouteMask.Rhythm ] ->
                            RadioButton.create [
                                RadioButton.margin (2.0, 0.0)
                                RadioButton.groupName "RouteMask"
                                RadioButton.content (string mask)
                                RadioButton.isChecked (inst.RouteMask = mask)
                                RadioButton.onClick (fun _ -> mask |> SetRouteMask |> EditInstrumental |> dispatch)
                            ]
                    ]
            ]

            // Bass pick
            locText "Picked" [
                Grid.row 3
                TextBlock.isVisible (inst.Name = ArrangementName.Bass)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            CheckBox.create [
                Grid.column 1
                Grid.row 3
                CheckBox.margin (4.0, 0.0)
                CheckBox.isVisible (inst.Name = ArrangementName.Bass)
                CheckBox.isChecked inst.BassPicked
                CheckBox.onChecked (fun _ -> true |> SetBassPicked |> EditInstrumental |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetBassPicked |> EditInstrumental |> dispatch)
            ]

            // Tuning strings
            locText "Tuning" [
                Grid.row 4
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 4
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    UniformGrid.create [
                        UniformGrid.margin (2.0, 0.0)
                        UniformGrid.columns 6
                        UniformGrid.horizontalAlignment HorizontalAlignment.Left
                        UniformGrid.children (List.init 6 (tuningTextBox dispatch inst.Tuning))
                    ]

                    Grid.create [
                        Grid.rowDefinitions "15,15"
                        Grid.children [
                            tuningChangeRepeatButton dispatch Up
                            tuningChangeRepeatButton dispatch Down
                        ]
                    ]
                ]
            ]

            // Tuning Pitch
            locText "TuningPitch" [
                Grid.row 5
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            StackPanel.create [
                Grid.column 1
                Grid.row 5
                StackPanel.orientation Orientation.Horizontal
                StackPanel.children [
                    FixedNumericUpDown.create [
                        NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                        NumericUpDown.width 160.
                        NumericUpDown.minimum 0.0
                        NumericUpDown.maximum 50000.0
                        NumericUpDown.increment 1.0
                        NumericUpDown.formatString "F2"
                        FixedNumericUpDown.value inst.TuningPitch
                        FixedNumericUpDown.onValueChanged (SetTuningPitch >> EditInstrumental >> dispatch)
                    ]
                    TextBlock.create [
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.text (sprintf "%+.0f %s" (Utils.tuningPitchToCents inst.TuningPitch) (translate "cents"))
                    ]
                ]
            ]

            // Base Tone
            locText "BaseToneKey" [
                Grid.row 6
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            FixedAutoCompleteBox.create [
                Grid.column 1
                Grid.row 6
                AutoCompleteBox.margin (4.0, 0.0)
                AutoCompleteBox.dataItems (
                    state.Project.Tones
                    |> List.choose (fun t -> Option.ofString t.Key)
                    |> List.distinct)
                AutoCompleteBox.horizontalAlignment HorizontalAlignment.Stretch
                FixedAutoCompleteBox.validationErrorMessage (translate "EnterBaseToneKey")
                FixedAutoCompleteBox.validation String.notEmpty
                FixedAutoCompleteBox.text inst.BaseTone
                FixedAutoCompleteBox.onTextChanged (StringValidator.toneName >> SetBaseTone >> EditInstrumental >> dispatch)
                ToolTip.tip (translate "BaseToneKeyToolTip")
            ]

            // Tone key list
            locText "ToneKeys" [
                Grid.row 7
                TextBlock.isVisible (inst.Tones.Length > 0)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            TextBlock.create [
                Grid.column 1
                Grid.row 7
                TextBlock.margin 4.
                TextBlock.textWrapping TextWrapping.Wrap
                TextBlock.isVisible (inst.Tones.Length > 0)
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Left
                TextBlock.text (String.Join(", ", inst.Tones))
            ]

            // Scroll speed
            locText "ScrollSpeed" [
                Grid.row 8
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            FixedNumericUpDown.create [
                Grid.column 1
                Grid.row 8
                ToolTip.tip (translate "ScrollSpeedToolTip")
                NumericUpDown.isVisible state.Config.ShowAdvanced
                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                NumericUpDown.increment 0.1
                NumericUpDown.maximum 5.0
                NumericUpDown.minimum 0.5
                NumericUpDown.formatString "F1"
                FixedNumericUpDown.value inst.ScrollSpeed
                FixedNumericUpDown.onValueChanged (SetScrollSpeed >> EditInstrumental >> dispatch)
            ]

            // Master ID
            locText "MasterID" [
                Grid.row 9
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            FixedTextBox.create [
                Grid.column 1
                Grid.row 9
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                FixedTextBox.text (string inst.MasterID)
                FixedTextBox.validationErrorMessage (translate "EnterNumberLargerThanZero")
                FixedTextBox.validation isNumberGreaterThanZero
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    match Int32.TryParse(txtBox.Text) with
                    | true, masterId when masterId > 0 ->
                        SetMasterId masterId |> EditInstrumental |> dispatch
                    | _ ->
                        ()
                )
            ]

            // Persistent ID
            locText "PersistentID" [
                Grid.row 10
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            FixedTextBox.create [
                Grid.column 1
                Grid.row 10
                TextBox.isVisible state.Config.ShowAdvanced
                TextBox.horizontalAlignment HorizontalAlignment.Stretch
                FixedTextBox.text (inst.PersistentID.ToString("N"))
                FixedTextBox.validationErrorMessage (translate "EnterAValidGUID")
                FixedTextBox.validation (Guid.TryParse >> fst)
                TextBox.onLostFocus (fun arg ->
                    let txtBox = arg.Source :?> TextBox
                    match Guid.TryParse(txtBox.Text) with
                    | true, id ->
                        SetPersistentId id |> EditInstrumental |> dispatch
                    | false, _ ->
                        ()
                )
            ]

            // Custom audio file
            locText "CustomAudioFile" [
                Grid.row 11
                TextBlock.isVisible state.Config.ShowAdvanced
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
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
                        Button.onClick (fun _ -> Dialog.AudioFile true |> ShowDialog |> dispatch)
                    ]

                    // Remove audio file
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0., 2., 2., 0.)
                        Button.content "X"
                        Button.isVisible inst.CustomAudio.IsSome
                        Button.onClick (fun _ -> None |> SetCustomAudioPath |> EditInstrumental |> dispatch)
                        ToolTip.tip (translate "RemoveCustomAudioToolTip")
                    ]

                    // Convert to wem
                    Button.create [
                        DockPanel.dock Dock.Right
                        Button.margin (0., 2., 2., 0.)
                        Button.content "W"
                        Button.isEnabled (not <| state.RunningTasks.Contains(WemConversion))
                        Button.isVisible (
                            match inst.CustomAudio with
                            | Some audio when not <| String.endsWith ".wem" audio.Path ->
                                true
                            | _ ->
                                false)
                        Button.onClick (fun _ -> ConvertToWemCustom |> dispatch)
                        ToolTip.tip (translate "ConvertToWemToolTip")
                    ]

                    // Audio file path
                    TextBlock.create [
                        TextBlock.margin (4., 2., 0., 0.)
                        TextBlock.text (
                            Option.map (fun x -> IO.Path.GetFileName(x.Path)) inst.CustomAudio
                            |> Option.defaultValue (translate "NoAudioFile")
                        )
                    ]
                ]
            ]

            // Custom audio volume
            if state.Config.ShowAdvanced && inst.CustomAudio.IsSome then
                locText "Volume" [
                    Grid.row 12
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                ]
                FixedNumericUpDown.create [
                    Grid.column 1
                    Grid.row 12
                    NumericUpDown.isEnabled (
                        match inst.CustomAudio with
                        | Some audio when state.RunningTasks.Contains(VolumeCalculation(CustomAudio(audio.Path, inst.PersistentID))) ->
                            false
                        | _ ->
                            true)
                    NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                    NumericUpDown.minimum -45.
                    NumericUpDown.maximum 45.
                    NumericUpDown.increment 0.5
                    NumericUpDown.formatString "+0.0;-0.0;0.0"
                    FixedNumericUpDown.value (
                        inst.CustomAudio
                        |> Option.map (fun x -> x.Volume)
                        |> Option.defaultValue state.Project.AudioFile.Volume)
                    FixedNumericUpDown.onValueChanged (SetCustomAudioVolume >> EditInstrumental >> dispatch)
                    ToolTip.tip (translate "AudioVolumeToolTip")
                ]
        ]
    ]
