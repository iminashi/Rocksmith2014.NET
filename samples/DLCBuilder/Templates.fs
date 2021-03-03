module DLCBuilder.Templates

open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI
open Avalonia.Media
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open System
open Media

let private toneContextMenu state dispatch =
    ContextMenu.create [
        ContextMenu.isVisible (state.SelectedToneIndex <> -1)
        ContextMenu.viewItems [
            MenuItem.create [
                MenuItem.header (translate "duplicate")
                MenuItem.onClick (fun _ -> DuplicateTone |> dispatch)
            ]

            MenuItem.create [
                MenuItem.header (translate "moveUp")
                //MenuItem.inputGesture (KeyGesture(Key.Up, KeyModifiers.Alt))
                MenuItem.onClick (fun _ -> Up |> MoveTone |> dispatch)
            ]

            MenuItem.create [
                MenuItem.header (translate "moveDown")
                //MenuItem.inputGesture (KeyGesture(Key.Down, KeyModifiers.Alt))
                MenuItem.onClick (fun _ -> Down |> MoveTone |> dispatch)
            ]

            MenuItem.create [
                MenuItem.header (translate "edit")
                MenuItem.onClick (fun _ -> ShowToneEditor |> dispatch)
            ]

            MenuItem.create [
                MenuItem.header (translate "export")
                MenuItem.onClick (fun _ -> ExportSelectedTone |> dispatch)
            ]

            MenuItem.create [ MenuItem.header "-" ]

            MenuItem.create [
                MenuItem.header (translate "remove")
                //MenuItem.inputGesture (KeyGesture(Key.Delete, KeyModifiers.None))
                MenuItem.onClick (fun _ -> dispatch DeleteTone)
            ]
        ]
    ]

let tone dispatch state index (t: Tone) =
    let title =
        if String.IsNullOrEmpty t.Key || t.Key = t.Name then
            t.Name
        else
            t.Name + " [" + t.Key + "]"

    let description =
        if isNull t.ToneDescriptors || t.ToneDescriptors.Length = 0 then
            String.Empty
        else
            ToneDescriptor.combineUINames t.ToneDescriptors

    let bg =
        if state.SelectedToneIndex = index then
            SolidColorBrush.Parse "#0a528b" :> ISolidColorBrush
        else
            Brushes.Transparent

    StackPanel.create [
        StackPanel.background bg
        StackPanel.onPointerPressed ((fun e -> index |> SetSelectedToneIndex |> dispatch), SubPatchOptions.OnChangeOf index)
        StackPanel.onDoubleTapped (fun _ -> ShowToneEditor |> dispatch)
        StackPanel.contextMenu (toneContextMenu state dispatch)
        StackPanel.children [
            TextBlock.create [
                TextBlock.margin (6., 4., 6., 2.)
                TextBlock.fontSize 16.
                TextBlock.text title
            ]
            TextBlock.create [
                TextBlock.margin (6., 4., 6., 2.)
                TextBlock.foreground "#afafaf"
                TextBlock.fontSize 14.
                TextBlock.text description
            ]
        ]
    ] |> generalize

let toneDescriptor =
    DataTemplateView<ToneDescriptor>.create (fun desc ->
        TextBlock.create [
            TextBlock.text <| translate desc.Name
        ])

/// Template for a selectable arrangement in the ListBox.
let arrangement =
    DataTemplateView<Arrangement>.create (fun arr ->
        let name, icon, color =
            match arr with
            | Instrumental inst ->
                let baseName = Arrangement.getHumanizedName arr

                let extra =
                    if inst.Name = ArrangementName.Combo then
                        " (Combo)"
                    elif inst.RouteMask = RouteMask.Bass && inst.BassPicked then
                        " (Picked)"
                    else
                        String.Empty

                let color =
                    match inst.RouteMask with
                    | RouteMask.Lead -> Brushes.lead
                    | RouteMask.Bass -> Brushes.bass
                    | _ -> Brushes.rhythm

                sprintf "%s%s" baseName extra,
                Icons.guitar,
                color

            | Vocals v ->
                let name = Arrangement.getHumanizedName arr
                let color =
                    if v.Japanese then
                        Brushes.jvocals
                    else
                        Brushes.vocals

                name, Icons.microphone, color

            | Showlights _ ->
                Arrangement.getHumanizedName arr, Icons.spotlight, Brushes.showlights

        StackPanel.create [
            StackPanel.orientation Orientation.Horizontal
            StackPanel.minHeight 30.
            StackPanel.children [
                // Icon
                Path.create [
                    Path.fill color
                    Path.data icon
                    Path.verticalAlignment VerticalAlignment.Center
                    Path.margin (0., 0., 6., 0.)
                ]

                StackPanel.create [
                    StackPanel.verticalAlignment VerticalAlignment.Center
                    StackPanel.children [
                        // Name
                        TextBlock.create [
                            TextBlock.fontSize 16.
                            TextBlock.text name
                        ]

                        let extra =
                            match arr with
                            | Instrumental inst ->
                                let tuning = Utils.getTuningName inst.Tuning
                                if inst.TuningPitch <> 440.0 then
                                    $"{tuning} (A{inst.TuningPitch})"
                                else
                                    tuning

                            | Vocals { CustomFont = Some _ } ->
                                translate "customFont"
                            | _ ->
                                String.Empty

                        TextBlock.create [
                            TextBlock.foreground "#afafaf"
                            TextBlock.fontSize 14.
                            TextBlock.text extra
                        ]
                    ]
                ]
            ]
        ]
    )
