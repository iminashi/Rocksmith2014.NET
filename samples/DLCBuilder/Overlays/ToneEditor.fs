module DLCBuilder.Views.ToneEditor

open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
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

let private getValidMoves (array: 'a option array) index =
    let exists = array.[index].IsSome
    let up = exists && index > 0 
    let down = exists && index < 3 && array.[index + 1].IsSome
    up, down

let private gearSlotSelector dispatch gearList selectedGearSlot (content: string) gearSlot =
    let isEnabled = enablePedalSelector gearList gearSlot
    let isSelected = gearSlot = selectedGearSlot
    let canUp, canDown =
        match gearSlot with
        | Amp | Cabinet ->
            false, false
        | PrePedal index ->
            getValidMoves gearList.PrePedals index
        | PostPedal index ->
            getValidMoves gearList.PostPedals index
        | Rack index ->
            getValidMoves gearList.Racks index

    let validDirections =
        if canUp then ValidSpinDirections.Increase else ValidSpinDirections.None
        ||| if canDown then ValidSpinDirections.Decrease else ValidSpinDirections.None

    Border.create [
        if isEnabled then Border.cursor Cursors.hand
        Border.focusable true
        Border.margin (0., 2.)
        Border.isEnabled isEnabled
        Border.onTapped (fun e ->
            e.Handled <- true
            gearSlot |> SetSelectedGearSlot |> dispatch)
        Border.onKeyDown (fun e ->
            if e.Key = Key.Space then
                e.Handled <- true
                gearSlot |> SetSelectedGearSlot |> dispatch)
        Border.child (
            ButtonSpinner.create [
                ButtonSpinner.background (
                    if isSelected then
                        "#185f99"
                    elif isEnabled then
                        "#181818"
                    else
                        "#484848")
                ButtonSpinner.content content
                ButtonSpinner.validSpinDirection validDirections
                ButtonSpinner.fontSize 14.
                ButtonSpinner.showButtonSpinner (isSelected && (canUp || canDown))
                ButtonSpinner.onSpin (fun e ->
                    let dir = if e.Direction = SpinDirection.Decrease then Down else Up
                    MovePedal(gearSlot, dir) |> EditTone |> dispatch
                )
            ]
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
        gearSlotSelector dispatch gearList selectedGearSlot content gearSlot
        |> generalize ]

let private effectCount (gearList: Gear) =
    seq {
        yield! gearList.PrePedals
        yield! gearList.PostPedals
        yield! gearList.Racks }
    |> Seq.choose id
    |> Seq.length

let private gearSlots repository state dispatch (gearList: Gear) =
    let ampName = repository.AmpDict.[gearList.Amp.Key].Name
    let cabinetName =
        let c = repository.CabinetDict.[gearList.Cabinet.Key] in $"{c.Name} - {c.Category}"

    vStack [
        gearSlotHeader "Amp"
        gearSlotSelector dispatch gearList state.SelectedGearSlot ampName Amp

        gearSlotHeader "Cabinet"
        gearSlotSelector dispatch gearList state.SelectedGearSlot cabinetName Cabinet

        yield! [ ("PrePedals", PrePedal); ("LoopPedals", PostPedal); ("Rack", Rack) ]
                |> List.collect (pedalSelectors dispatch repository state.SelectedGearSlot gearList)

        if effectCount gearList > 4 then
            hStack [
                TextBlock.create [
                    TextBlock.foreground Brushes.OrangeRed
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.verticalAlignment VerticalAlignment.Center
                    TextBlock.fontSize 16.
                    TextBlock.text (translate "WarningMoreThanFourEffects")
                ]
                HelpButton.create [
                    HelpButton.helpText (translate "WarningMoreThanFourEffectsHelp")
                ]
            ]

        Button.create [
            Button.content (translate "Close")
            Button.horizontalAlignment HorizontalAlignment.Center
            Button.margin (0., 8.)
            Button.padding (20., 8.)
            Button.onClick (fun _ -> dispatch (CloseOverlay OverlayCloseMethod.OverlayButton))
        ]
    ]

let private gearSelector dispatch repository gearData gearSlot =
    FixedComboBox.create [
        ComboBox.virtualizationMode ItemVirtualizationMode.Simple
        ComboBox.itemTemplate gearTemplate
        FixedComboBox.dataItems (
            match gearSlot with
            | Amp -> repository.Amps
            | Cabinet -> repository.CabinetChoices
            | PrePedal _ | PostPedal _ -> repository.Pedals
            | Rack _ -> repository.Racks)
        FixedComboBox.selectedItem (
            match gearData with
            | Some data when data.Type = "Cabinets" ->
                repository.CabinetChoices
                |> Array.find (fun x -> x.Name = data.Name)
            | Some data ->
                data
            | None ->
                Unchecked.defaultof<GearData>)
        FixedComboBox.onSelectedItemChanged (fun item ->
            match item with
            | :? GearData as gear ->
                gear |> SetPedal |> EditTone |> dispatch
            | _ ->
                ())
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
        // Some old CDLC may have out-of-bounds values
        let index = Math.Clamp(int value, 0, enums.Length - 1)
        enums.[index]
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
                TextBlock.text (translate <| if micPositions.Length = 1 then "NothingToConfigure" else "MicPosition")
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
                                FixedTextBox.create [
                                    DockPanel.dock Dock.Right
                                    TextBox.minWidth 45.
                                    TextBox.maxWidth 100.
                                    TextBox.horizontalAlignment HorizontalAlignment.Right
                                    FixedTextBox.text <| currentValue.ToString(getFormatString knob)
                                    FixedTextBox.autoFocus true
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
        locText "LoadingToneGearData" [
            TextBox.horizontalAlignment HorizontalAlignment.Center
            TextBox.verticalAlignment VerticalAlignment.Center
        ] |> generalize
    | Some repository ->
        let gearData = getGearDataForCurrentPedal repository tone.GearList state.SelectedGearSlot

        Grid.create [
            Grid.width 620.
            Grid.minHeight 635.
            Grid.columnDefinitions "*,*"
            Grid.children [
                gearSlots repository state dispatch tone.GearList

                StackPanel.create [
                    Grid.column 1
                    StackPanel.margin (16., 0., 0., 0.)
                    StackPanel.children [
                        yield gearSelector dispatch repository gearData state.SelectedGearSlot

                        match gearData with
                        | None ->
                            ()
                        | Some gear ->
                            // Remove Button
                            yield
                                Button.create [
                                    Button.margin (0., 2.)
                                    Button.content (translate "Remove")
                                    Button.isVisible (match state.SelectedGearSlot with Amp | Cabinet -> false | _ -> true)
                                    Button.onClick (fun _ -> RemovePedal |> EditTone |> dispatch)
                                ]
                            yield! knobSliders state dispatch repository tone.GearList gear
                    ]
                ]
            ]
        ] |> generalize
