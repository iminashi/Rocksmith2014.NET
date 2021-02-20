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
open ToneGear

let private allGear = loadGearData() |> Async.RunSynchronously

let private filterSort type' sortBy = allGear |> Array.filter (fun x -> x.Type = type') |> Array.sortBy sortBy
let private toDict = Array.map (fun x -> x.Key, x) >> readOnlyDict

let private amps = filterSort "Amps" (fun x -> x.Name)
let private cabinets = filterSort "Cabinets" (fun x -> x.Name)
let private pedals = filterSort "Pedals" (fun x -> x.Category, x.Name)
let private racks = filterSort "Racks" (fun x -> x.Category, x.Name)

let private ampDict = toDict amps
let private cabinetDict = toDict cabinets
let private pedalDict = toDict pedals
let private rackDict = toDict racks

let private getPedalAndDict gearType (tone: Tone) =
    let gearList = tone.GearList
    match gearType with
    | Amp -> Some gearList.Amp, ampDict
    | PrePedal index -> gearList.PrePedals.[index], pedalDict
    | PostPedal index -> gearList.PostPedals.[index], pedalDict
    | Rack index -> gearList.Racks.[index], rackDict

let private cabinetTemplate =
    DataTemplateView<GearData>.create (fun gear ->
        TextBlock.create [
            let category = gear.Category.Replace("_", " ")
            TextBlock.text $"{gear.Name} - {category}"
        ])

let private gearTemplate =
    DataTemplateView<GearData>.create (fun gear ->
        match gear.Type with
        | "Amps" ->
            let prefix = if String.startsWith "bass" gear.Key then "(Bass) " else String.Empty
            TextBlock.create [ TextBlock.text $"{prefix}{gear.Name}" ]
        | _ ->
            TextBlock.create [ TextBlock.text $"{gear.Category}: {gear.Name}" ])

let private gearTypeHeader locName =
    TextBlock.create [
        TextBlock.fontSize 14.
        TextBlock.margin (0., 2.)
        TextBlock.text (translate locName)
      ] |> generalize

let private pedalSelectors dispatch selectedGearType tone (locName, pedalFunc) =
    [ gearTypeHeader locName

      for index in 0..3 do
          ToggleButton.create [
              ToggleButton.margin (0., 2.)
              ToggleButton.minHeight 30.
              ToggleButton.fontSize 14.
              ToggleButton.content (
                  match getPedalAndDict (pedalFunc index) tone with
                  | Some pedal, dict -> dict.[pedal.Key].Name
                  | None, _ -> String.Empty)
              ToggleButton.isChecked (pedalFunc index = selectedGearType)
              ToggleButton.onChecked (fun _ -> pedalFunc index |> SetSelectedGearType |> dispatch)
          ] |> generalize ]

let private gearTypeSelector state dispatch (tone: Tone) =
    StackPanel.create [
        StackPanel.children [
            gearTypeHeader "amp"
            ToggleButton.create [
                ToggleButton.minHeight 30.
                ToggleButton.fontSize 14.
                ToggleButton.content ampDict.[tone.GearList.Amp.Key].Name
                ToggleButton.isChecked (state.SelectedGearType = Amp)
                ToggleButton.onChecked (fun _ -> Amp |> SetSelectedGearType |> dispatch)
            ]

            gearTypeHeader "cabinet"
            ComboBox.create [
                ComboBox.dataItems cabinets
                ComboBox.itemTemplate cabinetTemplate
                ComboBox.selectedItem (cabinetDict.[tone.GearList.Cabinet.Key])
                ComboBox.onSelectedItemChanged (fun item ->
                    match item with
                    | :? GearData as gear ->
                        gear |> SetCabinet |> EditTone |> dispatch
                    | _ ->
                        ()
                )
            ]

            yield! [ ("prePedals", PrePedal); ("loopPedals", PostPedal); ("rack", Rack) ]
            |> List.collect (pedalSelectors dispatch state.SelectedGearType tone)
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
            | None -> Unchecked.defaultof<GearData>)
        ComboBox.onSelectedItemChanged (fun item ->
            match item with
            | :? GearData as gear -> Some gear
            | _ -> None
            |> SetSelectedGear |> dispatch)
    ]

let private knobSliders dispatch (tone: Tone) gearType (knobs: GearKnob array) =
    knobs
    |> Array.mapi (fun i knob ->
        let currentValue =
            getKnobValuesForGear gearType tone
            |> Option.bind (Map.tryFind knob.Key)
            |> Option.defaultValue knob.DefaultValue

        let bg = if i % 2 = 0 then SolidColorBrush.Parse "#303030" else SolidColorBrush.Parse "#383838"
        let formatValue v (step: float) =
            if Math.Ceiling step > step
            then sprintf "%.1f" v
            else sprintf "%.0f" v

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
                                TextBlock.text (formatValue currentValue (float knob.ValueStep))
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
    Grid.create [
        Grid.width 550.
        Grid.minHeight 660.
        Grid.columnDefinitions "*,*"
        Grid.rowDefinitions "*,auto"
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

            Button.create [
                Grid.row 1
                Grid.columnSpan 2
                Button.margin 4.
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.content (translate "close")
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.onClick (fun _ -> CloseOverlay |> dispatch)
            ]
        ]
    ] |> generalize
