module DLCBuilder.Views.ToneEditor

open Avalonia.Layout
open Avalonia.Media
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open System
open Rocksmith2014.Common.Manifest
open Rocksmith2014.Common
open DLCBuilder
open Tones

let private allGear = Tones.loadPedalData() |> Async.RunSynchronously

let private filterSort type' sortBy = allGear |> Array.filter (fun x -> x.Type = type') |> Array.sortBy sortBy
let private toDict = Array.map (fun x -> x.Key, x) >> readOnlyDict

let private pedals = filterSort "Pedals" (fun x -> x.Category, x.Name)
let private amps = filterSort "Amps" (fun x -> x.Name)
let private cabinets = filterSort "Cabinets" (fun x -> x.Name)
let private racks = filterSort "Racks" (fun x -> x.Category, x.Name)

let private pedalDict = toDict pedals
let private ampDict = toDict amps
let private cabinetDict = toDict cabinets
let private rackDict = toDict racks

let private cabinetTemplate =
    DataTemplateView<ToneGear>.create (fun gear ->
        TextBlock.create [
            TextBlock.text (gear.Name + " - " + gear.Category.Replace("_", " "))
        ])

let private gearTemplate =
    DataTemplateView<ToneGear>.create (fun gear ->
        match gear.Type with
        | "Amps" ->
            let prefix =
                if gear.Key.StartsWith("bass", StringComparison.OrdinalIgnoreCase) then
                    "(Bass) "
                else
                    String.Empty

            TextBlock.create [
                TextBlock.text (prefix + gear.Name)
            ]
        | _ ->
            TextBlock.create [
                TextBlock.text (gear.Category + ": " + gear.Name)
            ])

let private getPedalAndDict gearType (tone: Tone) =
    let gearList = tone.GearList
    match gearType with
    | Amp -> Some gearList.Amp, ampDict
    | PrePedal index -> gearList.PrePedals.[index], pedalDict
    | PostPedal index -> gearList.PostPedals.[index], pedalDict
    | Rack index -> gearList.Racks.[index], rackDict

let private gearTypeSelector state dispatch (tone: Tone) =
    let amp = ampDict.[tone.GearList.Amp.Key]
    let selectedGearType = state.SelectedGearType
    let selectedGearKey =
        match state.SelectedGear with
        | Some gear -> gear.Key
        | None when selectedGearType = Amp -> amp.Key
        | _ -> String.Empty

    StackPanel.create [
        StackPanel.children [
            StackPanel.create [
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text (translate "amp")
                    ]
                    ToggleButton.create [
                        ToggleButton.minHeight 27.
                        ToggleButton.content amp.Name
                        ToggleButton.isChecked (amp.Key = selectedGearKey)
                        ToggleButton.onChecked (fun _ -> Amp |> SetSelectedGearType |> dispatch)
                    ]

                    TextBlock.create [
                        TextBlock.text (translate "cabinet")
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
                        TextBlock.text (translate "prePedals")
                    ]
                    yield! [ 0..3 ]
                    |> List.map (fun index ->
                        ToggleButton.create [
                            ToggleButton.margin (0., 2.)
                            ToggleButton.minHeight 27.
                            ToggleButton.content (
                                match getPedalAndDict (PrePedal index) tone with
                                | Some pedal, dict -> dict.[pedal.Key].Name
                                | None, _ -> String.Empty)
                            ToggleButton.isChecked (PrePedal index = selectedGearType)
                            ToggleButton.onChecked (fun _ -> PrePedal index |> SetSelectedGearType |> dispatch)
                        ] |> generalize)
                ]
            ]

            StackPanel.create [
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text (translate "loopPedals")
                    ]
                    yield! [ 0..3 ]
                    |> List.map (fun index ->
                        ToggleButton.create [
                            ToggleButton.margin (0., 2.)
                            ToggleButton.minHeight 27.
                            ToggleButton.content (
                                match getPedalAndDict (PostPedal index) tone with
                                | Some pedal, dict -> dict.[pedal.Key].Name
                                | None, _ -> String.Empty)
                            ToggleButton.isChecked (PostPedal index = selectedGearType)
                            ToggleButton.onChecked (fun _ -> PostPedal index |> SetSelectedGearType |> dispatch)
                        ] |> generalize)
                ]
            ]

            StackPanel.create [
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text (translate "rack")
                    ]
                    yield! [ 0..3 ]
                    |> List.map (fun index ->
                        ToggleButton.create [
                            ToggleButton.margin (0., 2.)
                            ToggleButton.minHeight 27.
                            ToggleButton.content (
                                match getPedalAndDict (Rack index) tone with
                                | Some pedal, dict -> dict.[pedal.Key].Name
                                | None, _ -> String.Empty)
                            ToggleButton.isChecked (Rack index = selectedGearType)
                            ToggleButton.onChecked (fun _ -> Rack index |> SetSelectedGearType |> dispatch)
                        ] |> generalize)
                ]
            ]
        ]
    ]

let private gearSelector dispatch (tone: Tone) gearType =
    let pedal, dict = getPedalAndDict gearType tone

    ComboBox.create [
        ComboBox.dataItems (
            match gearType with
            | Amp -> amps
            | PrePedal _ | PostPedal _ -> pedals
            | Rack _ -> racks)
        ComboBox.itemTemplate gearTemplate
        ComboBox.selectedItem (
            match pedal with
            | Some pedal -> dict.[pedal.Key]
            | None -> Unchecked.defaultof<ToneGear>)
        ComboBox.onSelectedItemChanged (fun item ->
            match item with
            | :? ToneGear as gear -> Some gear
            | _ -> None
            |> SetSelectedGear |> dispatch)
    ]

let private knobSliders dispatch (tone: Tone) gearType knobs =
    knobs
    |> Array.mapi (fun i knob ->
        let currentValue =
            match Tones.getKnobValuesForGear gearType tone with
            | Some currentValues ->
                let mutable value = knob.DefaultValue
                currentValues.TryGetValue(knob.Key, &value) |> ignore
                value
            | None ->
                knob.DefaultValue

        let bg = if i % 2 = 0 then SolidColorBrush.Parse "#303030" else SolidColorBrush.Parse "#383838"

        StackPanel.create [
            StackPanel.background bg
            StackPanel.children [
                TextBlock.create [
                    TextBlock.text knob.Name
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                ]

                match knob.EnumValues with
                | Some enums ->
                    ComboBox.create [
                        ComboBox.dataItems enums
                        ComboBox.selectedIndex (int currentValue)
                        ComboBox.onSelectedIndexChanged ((fun index ->
                            SetKnobValue (knob.Key, float32 index) |> EditTone |> dispatch),
                            SubPatchOptions.Always
                        )
                    ]
                | None ->
                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.horizontalAlignment HorizontalAlignment.Center
                        StackPanel.children [
                            TextBlock.create [
                                TextBlock.text (string currentValue)
                            ]

                            if knob.UnitType <> "number" then
                                TextBlock.create [
                                    TextBlock.text knob.UnitType
                                    TextBlock.margin (4., 0., 0., 0.)
                                ]
                        ]
                    ]

                    DockPanel.create [
                        DockPanel.margin (6., 0., 6., 4.)
                        DockPanel.children [
                            TextBlock.create [
                                DockPanel.dock Dock.Left
                                TextBlock.text (string knob.MinValue)
                            ]
                            TextBlock.create [
                                DockPanel.dock Dock.Right
                                TextBlock.text (string knob.MaxValue)
                            ]
                            Slider.create [
                                Slider.isSnapToTickEnabled true
                                Slider.tickFrequency (float knob.ValueStep)
                                Slider.smallChange (float knob.ValueStep)
                                Slider.maximum (float knob.MaxValue)
                                Slider.minimum (float knob.MinValue)
                                Slider.value (float currentValue)
                                Slider.onValueChanged ((fun value ->
                                    SetKnobValue (knob.Key, float32 value) |> EditTone |> dispatch),
                                    SubPatchOptions.Always
                                )
                            ]
                        ]
                    ]
            ]
        ] |> generalize)

let view state dispatch tone =
    DockPanel.create [
        DockPanel.children [
            Button.create [
                DockPanel.dock Dock.Bottom
                Button.margin 4.
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.content (translate "close")
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.onClick (fun _ -> CloseOverlay |> dispatch)
            ]

            Grid.create [
                Grid.width 550.
                Grid.minHeight 550.
                Grid.columnDefinitions "*,*"
                Grid.children [
                    gearTypeSelector state dispatch tone

                    StackPanel.create [
                        Grid.column 1
                        StackPanel.margin (16., 0., 0., 0.)
                        StackPanel.children [
                            yield gearSelector dispatch tone state.SelectedGearType

                            match state.SelectedGear with
                            | Some { Knobs = Some knobs } ->
                                yield (
                                    Button.create [
                                        Button.margin (0., 2.)
                                        Button.content (translate "remove")
                                        Button.isVisible (state.SelectedGearType <> Amp)
                                        Button.onClick (fun _ -> RemovePedal |> EditTone |> dispatch)
                                    ])
                                yield! knobSliders dispatch tone state.SelectedGearType knobs
                            | _ ->
                                ()
                        ]
                    ]
                ]
            ]
        ]
    ] |> generalize
