module DLCBuilder.Views.ToneEditor

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open System
open DLCBuilder
open ToneGear
open Media

let private enablePedalSelector gearList = function
    | Amp | Cabinet ->
        true
    | PrePedal 0 | PostPedal 0 | Rack 0 ->
        true
    | PrePedal index ->
        gearList.PrePedals.[index - 1] |> Option.isSome
    | PostPedal index ->
        gearList.PostPedals.[index - 1] |> Option.isSome
    | Rack index ->
        gearList.Racks.[index - 1] |> Option.isSome

let private toggleButton dispatch gearList selectedGearSlot (content: string) gearSlot =
    let isEnabled = enablePedalSelector gearList gearSlot
    let isChecked = gearSlot = selectedGearSlot

    ToggleButton.create [
        ToggleButton.margin (0., 2.)
        ToggleButton.minHeight 30.
        ToggleButton.fontSize 14.
        ToggleButton.content content
        ToggleButton.isChecked isChecked
        ToggleButton.isEnabled isEnabled
        if isEnabled then ToggleButton.cursor Cursors.hand
        ToggleButton.onChecked (fun _ -> gearSlot |> SetSelectedGearSlot |> dispatch)
        ToggleButton.onClick (fun e ->
            let b = e.Source :?> ToggleButton
            b.IsChecked <- true
            e.Handled <- true
        )
    ]

let private gearTemplate =
    DataTemplateView<GearData>.create (fun gear ->
        match gear.Type with
        | "Amps" | "Cabinets" ->
            let prefix = if String.startsWith "bass" gear.Key then "(Bass) " else String.Empty
            TextBlock.create [ TextBlock.text $"{prefix}{gear.Name}" ]
        | _ ->
            TextBlock.create [ TextBlock.text $"{gear.Category}: {gear.Name}" ])

let private gearSlotHeader locName =
    TextBlock.create [
        TextBlock.fontSize 14.
        TextBlock.margin (0., 2.)
        TextBlock.text (translate locName)
    ] |> generalize

let private pedalSelectors dispatch repository selectedGearSlot gearList (locName, pedalFunc) =
    [ gearSlotHeader locName

      for index in 0..3 do
        let gearSlot = pedalFunc index
        let content =
            match getGearDataForCurrentPedal repository gearList gearSlot with
            | Some data -> data.Name
            | None -> String.Empty
        toggleButton dispatch gearList selectedGearSlot content gearSlot
        |> generalize ]

let private gearSlotSelector repository state dispatch (gearList: Gear) =
    let ampName = repository.AmpDict.[gearList.Amp.Key].Name
    let cabinetName =
        let c = repository.CabinetDict.[gearList.Cabinet.Key] in $"{c.Name} - {c.Category}"

    vStack [
        gearSlotHeader "amp"
        toggleButton dispatch gearList state.SelectedGearSlot ampName Amp

        gearSlotHeader "cabinet"
        toggleButton dispatch gearList state.SelectedGearSlot cabinetName Cabinet

        yield! [ ("prePedals", PrePedal); ("loopPedals", PostPedal); ("rack", Rack) ]
                |> List.collect (pedalSelectors dispatch repository state.SelectedGearSlot gearList)

        Button.create [
            Button.content (translate "close")
            Button.horizontalAlignment HorizontalAlignment.Center
            Button.margin (0., 8.)
            Button.padding (20., 8.)
            Button.onClick (fun _ -> dispatch CloseOverlay)
        ]
    ]

let private gearSelector dispatch repository (gearList: Gear) gearSlot =
    let gearData = getGearDataForCurrentPedal repository gearList gearSlot

    ComboBox.create [
        ComboBox.virtualizationMode ItemVirtualizationMode.Simple
        ComboBox.dataItems (
            match gearSlot with
            | Amp -> repository.Amps
            | Cabinet -> repository.CabinetChoices
            | PrePedal _ | PostPedal _ -> repository.Pedals
            | Rack _ -> repository.Racks)
        ComboBox.itemTemplate gearTemplate
        ComboBox.selectedItem (
            match gearData with
            | Some data when data.Type = "Cabinets" ->
                repository.CabinetChoices
                |> Array.find (fun x -> x.Name = data.Name)
            | Some data ->
                data
            | None ->
                Unchecked.defaultof<GearData>)
        ComboBox.onSelectedItemChanged (fun item ->
            match item with
            | :? GearData as gear -> Some gear
            | _ -> None
            |> SetSelectedGear |> dispatch)
    ]

let private getFormatString knob =
    let step = float knob.ValueStep
    let hasDecimals = ceil step > step
    let canBeNegative = knob.MinValue < 0.0f
    match hasDecimals, canBeNegative with
    | true,  true  -> "+0.0;-0.0;0.0"
    | true,  false -> "F1"
    | false, true  -> "+0;-0;0"
    | false, false -> "F0"

let private formatValue knob value =
    match knob.EnumValues with
    | Some enums ->
        enums.[int value]
    | None ->
        let unit = if knob.UnitType = "number" then String.Empty else $" {knob.UnitType}"
        let format = sprintf "{0:%s}%s" (getFormatString knob) unit
        // Use String.Format to get culture specific decimal separators
        String.Format(format, value)

let private parseKnobValue knob (text: string) =
    match Single.TryParse text with
    | true, value ->
        Some (Math.Clamp(value, knob.MinValue, knob.MaxValue))
    | false, _ ->
        None

let private valueRangeText column knob value =
    TextBlock.create [
        Grid.column column
        TextBlock.text (string value)
        TextBlock.isVisible knob.EnumValues.IsNone
        TextBlock.fontSize 10.
        TextBlock.foreground Brushes.Gray
        TextBlock.verticalAlignment VerticalAlignment.Center
    ]

let private knobSliders state dispatch repository (gearList: Gear) gear =
    match gear with
    // Cabinets
    | { Knobs = None } as cabinet ->
        vStack [
            let micPositions = repository.MicPositionsForCabinet.[cabinet.Name]
            TextBlock.create [
                TextBlock.margin (0., 4.)
                TextBlock.text (translate <| if micPositions.Length = 1 then "nothingToConfigure" else "micPosition")
            ]
            StackPanel.create [
                StackPanel.isVisible (micPositions.Length > 1)
                StackPanel.children (
                    micPositions
                    |> Array.map (fun cab ->
                        RadioButton.create [
                            RadioButton.content cab.Category
                            RadioButton.isChecked (gearList.Cabinet.Key = cab.Key)
                            // onChecked can cause an infinite update loop
                            RadioButton.onClick ((fun _ ->
                                cab |> SetPedal |> EditTone |> dispatch
                            ), SubPatchOptions.Always)
                        ] |> generalize)
                    |> Array.toList
                )
            ]
        ]
        |> generalize
        |> Array.singleton
    // Everything else
    | { Knobs = Some knobs } ->
        knobs
        |> Array.mapi (fun i knob ->
            let currentValue =
                getKnobValuesForGear gearList state.SelectedGearSlot
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
                            match state.ManuallyEditingKnobKey with
                            | Some key when knob.Key = key ->
                                // Editable text box
                                AutoFocusTextBox.create [
                                    DockPanel.dock Dock.Right
                                    TextBox.minWidth 45.
                                    TextBox.maxWidth 100.
                                    TextBox.horizontalAlignment HorizontalAlignment.Right
                                    TextBox.text <| currentValue.ToString(getFormatString knob)
                                    TextBox.onLostFocus (fun _ -> None |> SetManuallyEditingKnobKey |> dispatch)
                                    TextBox.onKeyDown ((fun e ->
                                        if e.Key = Key.Enter then
                                            e.Handled <- true
                                            let tb = e.Source :?> TextBox
                                            parseKnobValue knob tb.Text
                                            |> Option.iter (fun value ->
                                                SetKnobValue (knob.Key, value) |> EditTone |> dispatch)

                                            None |> SetManuallyEditingKnobKey |> dispatch
                                    ), SubPatchOptions.OnChangeOf knob)
                                ]
                            | _ ->
                                TextBlock.create [
                                    DockPanel.dock Dock.Right
                                    TextBlock.background Brushes.Transparent
                                    TextBlock.horizontalAlignment HorizontalAlignment.Right
                                    TextBlock.text (formatValue knob currentValue)
                                    TextBlock.cursor (if knob.EnumValues.IsNone then Cursors.hand else Cursors.arrow)
                                    if knob.EnumValues.IsNone then
                                        TextBlock.onTapped ((fun e ->
                                            e.Handled <- true
                                            knob.Key
                                            |> Some
                                            |> SetManuallyEditingKnobKey
                                            |> dispatch), SubPatchOptions.OnChangeOf knob)
                                ]
                        ]
                    ]

                    Grid.create [
                        Grid.margin (6., 0.)
                        Grid.columnDefinitions "auto,*,auto"
                        Grid.children [
                            valueRangeText 0 knob knob.MinValue
                            valueRangeText 2 knob knob.MaxValue

                            ToneKnobSlider.create [
                                Grid.column 1
                                Slider.margin (4., -10.)
                                ToneKnobSlider.knob knob
                                ToneKnobSlider.value (float currentValue)
                                ToneKnobSlider.onKnobValueChanged (SetKnobValue >> EditTone >> dispatch)
                            ]
                        ]
                    ]
                ]
            ] |> generalize)

let view state dispatch (tone: Tone) =
    match state.ToneGearRepository with
    | None ->
        locText "loadingToneGearData" [
            TextBox.horizontalAlignment HorizontalAlignment.Center
            TextBox.verticalAlignment VerticalAlignment.Center
        ] |> generalize
    | Some repository ->
        Grid.create [
            Grid.width 620.
            Grid.minHeight 635.
            Grid.columnDefinitions "*,*"
            Grid.children [
                gearSlotSelector repository state dispatch tone.GearList

                StackPanel.create [
                    Grid.column 1
                    StackPanel.margin (16., 0., 0., 0.)
                    StackPanel.children [
                        yield gearSelector dispatch repository tone.GearList state.SelectedGearSlot

                        match state.SelectedGear with
                        | None ->
                            ()
                        | Some gear ->
                            // Remove Button
                            yield
                                Button.create [
                                    Button.margin (0., 2.)
                                    Button.content (translate "remove")
                                    Button.isVisible (match state.SelectedGearSlot with Amp | Cabinet -> false | _ -> true)
                                    Button.onClick (fun _ -> RemovePedal |> EditTone |> dispatch)
                                ]
                            yield! knobSliders state dispatch repository tone.GearList gear
                    ]
                ]
            ]
        ] |> generalize
