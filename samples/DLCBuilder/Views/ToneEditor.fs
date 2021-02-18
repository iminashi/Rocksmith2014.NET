module DLCBuilder.Views.ToneEditor

open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI
open Avalonia.FuncUI.Components
open System
open Rocksmith2014.Common.Manifest
open Rocksmith2014.Common
open DLCBuilder
open Tones

let private allTones =
    Tones.loadPedalData()
    |> Async.RunSynchronously

let private pedals =
    allTones
    |> Array.filter (fun x -> x.Type = "Pedals")
    |> Array.sortBy (fun x -> x.Category, x.Name)

let private pedalDict =
    pedals
    |> Array.map (fun x -> x.Key, x)
    |> readOnlyDict

let private amps =
    allTones
    |> Array.filter (fun x -> x.Type = "Amps")
    |> Array.sortBy (fun x -> x.Name)

let private ampDict =
    amps
    |> Array.map (fun x -> x.Key, x)
    |> readOnlyDict

let private cabinets =
    allTones
    |> Array.filter (fun x -> x.Type = "Cabinets")
    |> Array.sortBy (fun x -> x.Name)

let private cabinetDict =
    cabinets
    |> Array.map (fun x -> x.Key, x)
    |> readOnlyDict

let private racks =
    allTones
    |> Array.filter (fun x -> x.Type = "Racks")
    |> Array.sortBy (fun x -> x.Category, x.Name)

let private rackDict =
    racks
    |> Array.map (fun x -> x.Key, x)
    |> readOnlyDict

let private ampTemplate =
    DataTemplateView<ToneGear>.create (fun t ->
        let prefix =
            if t.Key.StartsWith("bass", StringComparison.OrdinalIgnoreCase) then
                "(Bass) "
            else
                String.Empty

        TextBlock.create [
            TextBlock.text (prefix + t.Name)
        ])

let private cabinetTemplate =
    DataTemplateView<ToneGear>.create (fun t ->
        TextBlock.create [
            TextBlock.text (t.Name + " - " + t.Category.Replace("_", " "))
        ])

let private pedalTemplate =
    DataTemplateView<ToneGear>.create (fun t ->
        TextBlock.create [
            TextBlock.text (t.Category + ": " + t.Name)
        ])

let private getPedalAndDict gearType (tone: Tone) =
    match gearType with
    | Amp ->
        Some tone.GearList.Amp, ampDict
    | PrePedal index ->
        tone.GearList.PrePedals.[index], pedalDict
    | PostPedal index ->
        tone.GearList.PostPedals.[index], pedalDict
    | Rack index ->
        tone.GearList.Racks.[index], rackDict

let editor state dispatch (tone: Tone) =
    let amp = ampDict.[tone.GearList.Amp.Key]
    let selectedGearKey =
        match state.SelectedGear with
        | (Some gear, _) -> gear.Key
        | (None, Amp) -> amp.Key
        | _ -> String.Empty

    let selectedGearType = state.SelectedGear |> snd

    StackPanel.create [
        StackPanel.children [
            StackPanel.create [
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text "Amp"
                    ]
                    ToggleButton.create [
                        ToggleButton.content amp.Name
                        ToggleButton.isChecked (amp.Key = selectedGearKey)
                        ToggleButton.onIsPressedChanged ((fun isPressed ->
                            if isPressed then
                                (Some amp, Amp) |> SetSelectedGear |> dispatch),
                            SubPatchOptions.OnChangeOf amp)
                    ]

                    TextBlock.create [
                        TextBlock.text "Cabinet"
                    ]
                    ComboBox.create [
                        ComboBox.dataItems cabinets
                        ComboBox.itemTemplate cabinetTemplate
                        ComboBox.selectedItem (cabinetDict.[tone.GearList.Cabinet.Key])
                        ComboBox.onSelectedItemChanged (fun item ->
                            match item with
                            | :? ToneGear as gear ->
                                gear |> SetCabinet |> EditTone |> dispatch
                            | _ ->
                                ()
                        )
                    ]
                ]
            ]
            StackPanel.create [
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text "Pre-pedals"
                    ]
                    yield! [ 0..3 ]
                    |> List.map (fun index ->
                        match getPedalAndDict (PrePedal index) tone with
                        | Some pedal, _ ->
                            ToggleButton.create [
                                ToggleButton.margin (0., 2.)
                                ToggleButton.minHeight 25.
                                ToggleButton.content pedalDict.[pedal.Key].Name
                                ToggleButton.isChecked (PrePedal index = selectedGearType)
                                ToggleButton.onIsPressedChanged ((fun isPressed ->
                                    if isPressed then
                                        (Some pedalDict.[pedal.Key], PrePedal index) |> SetSelectedGear |> dispatch),
                                    SubPatchOptions.OnChangeOf pedal)
                            ] |> Helpers.generalize
                        | None, _ ->
                            ToggleButton.create [
                                ToggleButton.margin (0., 2.)
                                ToggleButton.minHeight 25.
                                ToggleButton.isChecked (PrePedal index = selectedGearType)
                                ToggleButton.onChecked (fun _ ->
                                    (None, PrePedal index) |> SetSelectedGear |> dispatch)
                            ] |> Helpers.generalize)
                ]
            ]

            StackPanel.create [
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text "Loop Pedals"
                    ]
                    yield! [ 0..3 ]
                    |> List.map (fun index ->
                        match getPedalAndDict (PostPedal index) tone with
                        | Some pedal, _ ->
                            ToggleButton.create [
                                ToggleButton.margin (0., 2.)
                                ToggleButton.minHeight 25.
                                ToggleButton.content pedalDict.[pedal.Key].Name
                                ToggleButton.isChecked (PostPedal index = selectedGearType)
                                ToggleButton.onChecked ((fun _ ->
                                    (Some pedalDict.[pedal.Key], PostPedal index) |> SetSelectedGear |> dispatch),
                                    SubPatchOptions.OnChangeOf pedal)
                            ] |> Helpers.generalize
                        | None, _ ->
                            ToggleButton.create [
                                ToggleButton.margin (0., 2.)
                                ToggleButton.minHeight 25.
                                ToggleButton.isChecked (PostPedal index = selectedGearType)
                                ToggleButton.onChecked (fun _ ->
                                    (None, PostPedal index) |> SetSelectedGear |> dispatch)
                            ] |> Helpers.generalize)
                ]
            ]

            StackPanel.create [
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text "Rack"
                    ]
                    yield! [ 0..3 ]
                    |> List.map (fun index ->
                        match getPedalAndDict (Rack index) tone with
                        | Some pedal, _ ->
                            ToggleButton.create [
                                ToggleButton.margin (0., 2.)
                                ToggleButton.minHeight 25.
                                ToggleButton.content rackDict.[pedal.Key].Name
                                ToggleButton.isChecked (Rack index = selectedGearType)
                                //ToggleButton.onChecked ((fun _ ->
                                //    (Some rackDict.[pedal.Key], Rack index) |> SetSelectedGear |> dispatch),
                                //    SubPatchOptions.OnChangeOf pedal)
                            ] |> Helpers.generalize
                        | None, _ ->
                            ToggleButton.create [
                                ToggleButton.margin (0., 2.)
                                ToggleButton.minHeight 25.
                                ToggleButton.isChecked (Rack index = selectedGearType)
                                ToggleButton.onChecked (fun _ ->
                                    (None, Rack index) |> SetSelectedGear |> dispatch)
                            ] |> Helpers.generalize)
                ]
            ]
        ]
    ]

let gearSelector dispatch (tone: Tone) gearType =
    let pedal, dict = getPedalAndDict gearType tone

    ComboBox.create [
        ComboBox.dataItems (
            match gearType with
            | Amp -> amps
            | PrePedal _ | PostPedal _ -> pedals
            | Rack _ -> racks)
        ComboBox.itemTemplate (match gearType with | Amp -> ampTemplate | _ -> pedalTemplate)
        match pedal with
        | Some pedal -> ComboBox.selectedItem (dict.[pedal.Key])
        | None -> ()
        ComboBox.onSelectedItemChanged ((fun item ->
            match item with
            | :? ToneGear as gear ->
                (Some gear, gearType) |> SetSelectedGear |> dispatch
                //gear |> SetPedal |> EditTone |> dispatch
            | _ -> ()),
            SubPatchOptions.OnChangeOf gearType)
    ]

let knobSliders dispatch (tone: Tone) gearType knobs =
    knobs
    |> Array.map (fun knob ->
        let currentValue =
            match Tones.getKnobValuesForGear gearType tone with
            | Some currentValues ->
                let mutable value = float32 knob.DefaultValue
                currentValues.TryGetValue(knob.Key, &value) |> ignore
                value
            | None ->
                float32 knob.DefaultValue

        match knob.EnumValues with
        | Some enums ->
            StackPanel.create [
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text knob.Name
                    ]
                    ComboBox.create [
                        ComboBox.dataItems enums
                        ComboBox.selectedIndex (int currentValue)
                        //ComboBox.onSelectedIndexChanged ((fun index ->
                        //    SetKnobValue (knob.Key, float index) |> EditTone |> dispatch),
                        //    SubPatchOptions.OnChangeOf knob
                        //)
                    ]
                ]
            ] |> Helpers.generalize
        | None ->
            StackPanel.create [
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text knob.Name
                    ]

                    TextBlock.create [
                        TextBlock.text (string currentValue)
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                    ]

                    DockPanel.create [
                        DockPanel.children [
                            TextBlock.create [
                                DockPanel.dock Dock.Left
                                TextBlock.text (string knob.MinValue)
                            ]
                            if knob.UnitType <> "number" then
                                TextBlock.create [
                                    DockPanel.dock Dock.Right
                                    TextBlock.text knob.UnitType
                                    TextBlock.margin (4.0, 0.0)
                                ]
                            TextBlock.create [
                                DockPanel.dock Dock.Right
                                TextBlock.text (string knob.MaxValue)
                            ]
                            Slider.create [
                                Slider.isSnapToTickEnabled true
                                Slider.tickFrequency knob.ValueStep
                                Slider.smallChange knob.ValueStep
                                Slider.value (float currentValue)
                                Slider.maximum knob.MaxValue
                                Slider.minimum knob.MinValue
                                //Slider.onValueChanged ((fun value ->
                                //    if value <> float currentValue then
                                //        SetKnobValue (knob.Key, value) |> EditTone |> dispatch),
                                //    SubPatchOptions.Always
                                //)
                            ]
                        ]
                    ]
                ]
            ] |> Helpers.generalize)

let view state dispatch tone =
    Grid.create [
        Grid.width 500.
        Grid.columnDefinitions "*,*"
        DockPanel.children [
            editor state dispatch tone

            StackPanel.create [
                Grid.column 1
                StackPanel.margin (16., 0., 0., 0.)
                StackPanel.children [
                    match state.SelectedGear with
                    | Some { Knobs = Some knobs }, gearType ->
                        yield gearSelector dispatch tone gearType

                        yield! knobSliders dispatch tone gearType knobs
                    | _, gearType ->
                        yield gearSelector dispatch tone gearType
                ]
            ]
        ]
    ] |> Helpers.generalize
