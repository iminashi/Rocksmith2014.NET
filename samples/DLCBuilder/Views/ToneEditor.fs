﻿module DLCBuilder.Views.ToneEditor

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
open Media

let private enablePedalSelector gearList gearType =
    match gearType with
    | Amp | Cabinet ->
        true
    | PrePedal index | PostPedal index | Rack index when index = 0 ->
        true
    | PrePedal index ->
        gearList.PrePedals.[index - 1] |> Option.isSome
    | PostPedal index ->
        gearList.PostPedals.[index - 1] |> Option.isSome
    | Rack index ->
        gearList.Racks.[index - 1] |> Option.isSome

let private toggleButton dispatch gearList selectedGearType (content: string) gearType  =
    let isEnabled = enablePedalSelector gearList gearType
    let isChecked = gearType = selectedGearType

    ToggleButton.create [
        ToggleButton.margin (0., 2.)
        ToggleButton.minHeight 30.
        ToggleButton.fontSize 14.
        ToggleButton.content content
        ToggleButton.isChecked isChecked
        ToggleButton.isEnabled isEnabled
        if isEnabled then ToggleButton.cursor Cursors.hand
        ToggleButton.onChecked (fun _ -> gearType |> SetSelectedGearType |> dispatch)
        ToggleButton.onClick (fun e ->
            let b = e.Source :?> ToggleButton
            b.IsChecked <- true
            e.Handled <- true
        )
    ]

let private valueRangeText column knob value =
    TextBlock.create [
        Grid.column column
        TextBlock.text (string value)
        TextBlock.isVisible knob.EnumValues.IsNone
        TextBlock.fontSize 10.
        TextBlock.foreground Brushes.Gray
        TextBlock.verticalAlignment VerticalAlignment.Center
    ]

let private gearTemplate =
    DataTemplateView<GearData>.create (fun gear ->
        match gear.Type with
        | "Amps" | "Cabinets" ->
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

   
let private pedalSelectors dispatch selectedGearType gearList (locName, pedalFunc) =
    [ gearTypeHeader locName

      for index in 0..3 do
        let gearType = pedalFunc index
        let content =
            match getGearDataForCurrentPedal gearList gearType with
            | Some data -> data.Name
            | None -> String.Empty
        toggleButton dispatch gearList selectedGearType content gearType 
        |> generalize ]

let private gearTypeSelector state dispatch (gearList: Gear) =
    let ampName = ampDict.[gearList.Amp.Key].Name
    let cabinetName =
        let c = cabinetDict.[gearList.Cabinet.Key] in $"{c.Name} - {c.Category}"
    StackPanel.create [
        StackPanel.children [
            gearTypeHeader "amp"
            toggleButton dispatch gearList state.SelectedGearType ampName Amp

            gearTypeHeader "cabinet"
            toggleButton dispatch gearList state.SelectedGearType cabinetName Cabinet 

            yield! [ ("prePedals", PrePedal); ("loopPedals", PostPedal); ("rack", Rack) ]
                   |> List.collect (pedalSelectors dispatch state.SelectedGearType gearList)
        ]
    ] 

let private gearSelector dispatch (gearList: Gear) gearType =
    let gearData = getGearDataForCurrentPedal gearList gearType

    ComboBox.create [
        ComboBox.virtualizationMode ItemVirtualizationMode.Simple
        ComboBox.dataItems (
            match gearType with
            | Amp -> amps
            | Cabinet -> cabinetChoices
            | PrePedal _ | PostPedal _ -> pedals
            | Rack _ -> racks)
        ComboBox.itemTemplate gearTemplate
        ComboBox.selectedItem (
            match gearData with
            | Some data when data.Type = "Cabinets" ->
                cabinetChoices
                |> Array.find (fun x -> x.Name = data.Name)
            | Some data -> data
            | None -> Unchecked.defaultof<GearData>)
        ComboBox.onSelectedItemChanged (fun item ->
            match item with
            | :? GearData as gear -> Some gear
            | _ -> None
            |> SetSelectedGear |> dispatch)
    ]

let private formatValue knob value =
    match knob.EnumValues with
    | Some enums ->
        enums.[int value]
    | None ->
        let step = knob.ValueStep |> float
        let unit = if knob.UnitType = "number" then String.Empty else " " + knob.UnitType
        match Math.Ceiling step > step, knob.MinValue < 0.0f with
        | true,  true  -> sprintf "%+.1f%s" value unit
        | true,  false -> sprintf "%.1f%s" value unit
        | false, true  -> sprintf "%+.0f%s" value unit
        | false, false -> sprintf "%.0f%s" value unit

let private knobSliders dispatch (gearList: Gear) gearType gear =
    match gear with
    // Cabinets
    | { Knobs = None } as cabinet ->
        StackPanel.create [
            StackPanel.children [
                let micPositions = micPositionsForCabinet.[cabinet.Name]
                TextBlock.create [
                    TextBlock.margin (0., 4.)
                    TextBlock.text (translate <| if micPositions.Length = 1 then "nothingToConfigure" else "micPosition")
                ]
                StackPanel.create [
                    StackPanel.isVisible (micPositions.Length > 1)
                    StackPanel.children [
                        yield! micPositions
                        |> Array.map (fun cab ->
                            RadioButton.create [
                                RadioButton.content cab.Category
                                RadioButton.isChecked (gearList.Cabinet.Key = cab.Key)
                                // onChecked can cause an infinite update loop
                                RadioButton.onClick ((fun _ ->
                                    cab |> SetPedal |> EditTone |> dispatch
                                ), SubPatchOptions.Always)
                            ] |> generalize
                        )
                    ]
                ]
            ]
        ]
        |> generalize
        |> Array.singleton
    // Everything else
    | { Knobs = Some knobs } ->
        knobs
        |> Array.mapi (fun i knob ->
            let currentValue =
                getKnobValuesForGear gearList gearType
                |> Option.bind (Map.tryFind knob.Key)
                |> Option.defaultValue knob.DefaultValue

            StackPanel.create [
                StackPanel.background (if i % 2 = 0 then Brushes.toneKnobEven else Brushes.toneKnobOdd)
                StackPanel.children [
                    DockPanel.create [
                        DockPanel.margin (6., 2.)
                        DockPanel.children [
                            // Knob name
                            TextBlock.create [
                                DockPanel.dock Dock.Left
                                TextBlock.text knob.Name
                                TextBlock.horizontalAlignment HorizontalAlignment.Left
                            ]
                            // Current value
                            TextBlock.create [
                                DockPanel.dock Dock.Right
                                TextBlock.horizontalAlignment HorizontalAlignment.Right
                                TextBlock.text (formatValue knob currentValue)
                            ]
                        ]
                    ]

                    Grid.create [
                        Grid.margin (6., -15., 6., -5.)
                        Grid.columnDefinitions "auto,*,auto"
                        Grid.children [
                            valueRangeText 0 knob knob.MinValue
                            valueRangeText 2 knob knob.MaxValue

                            Slider.create [
                                Grid.column 1
                                Slider.margin (4., 0.)
                                Slider.isSnapToTickEnabled true
                                Slider.tickFrequency (float knob.ValueStep)
                                Slider.smallChange (float knob.ValueStep)
                                Slider.maximum (float knob.MaxValue)
                                Slider.minimum (float knob.MinValue)
                                Slider.value (float currentValue)
                                Slider.onValueChanged ((fun value ->
                                    SetKnobValue (knob.Key, float32 value) |> EditTone |> dispatch),
                                    SubPatchOptions.Always)
                            ]
                        ]
                    ]
                ]
            ] |> generalize)

let view state dispatch (tone: Tone) =
    Grid.create [
        Grid.width 620.
        Grid.minHeight 660.
        Grid.columnDefinitions "*,*"
        Grid.rowDefinitions "*,auto"
        Grid.children [
            gearTypeSelector state dispatch tone.GearList

            StackPanel.create [
                Grid.column 1
                StackPanel.margin (16., 0., 0., 0.)
                StackPanel.children [
                    yield gearSelector dispatch tone.GearList state.SelectedGearType

                    match state.SelectedGear with
                    | Some gear ->
                        yield (
                            Button.create [
                                Button.margin (0., 2.)
                                Button.content (translate "remove")
                                Button.isVisible (match state.SelectedGearType with Amp | Cabinet -> false | _ -> true)
                                Button.onClick (fun _ -> RemovePedal |> EditTone |> dispatch)
                            ])
                        yield! knobSliders dispatch tone.GearList state.SelectedGearType gear
                    | None ->
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
