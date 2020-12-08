module DLCBuilder.Templates

open Avalonia.FuncUI.Components
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open System
open Media

// TODO: Fix for localization
let toneDescriptor state =
    DataTemplateView<ToneDescriptor>.create (fun desc ->
        TextBlock.create [
            TextBlock.text (state.Localization.GetString desc.Name)
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
                let  color =
                    if v.Japanese then
                        Brushes.jvocals
                    else
                        Brushes.vocals

                name, Icons.microphone, color

            | Showlights _ ->
                Arrangement.getHumanizedName arr, Icons.spotlight, Brushes.showlights

        StackPanel.create [
            StackPanel.orientation Orientation.Horizontal
            StackPanel.minHeight 33.
            StackPanel.children [
                Path.create [
                    Path.fill color
                    Path.data icon
                    Path.verticalAlignment VerticalAlignment.Center
                    Path.margin (0., 0., 4., 0.)
                ]
                StackPanel.create [
                    StackPanel.verticalAlignment VerticalAlignment.Center
                    StackPanel.children [
                        TextBlock.create [
                            TextBlock.fontSize 16.
                            TextBlock.text (name)
                        ]
                        match arr with
                        | Instrumental inst ->
                            let tuning =
                                let t = Utils.getTuningName inst.Tuning
                                if inst.TuningPitch <> 440.0 then $"{t} (A{inst.TuningPitch})" else t

                            TextBlock.create [
                                TextBlock.text tuning
                            ]
                        | Vocals { CustomFont = Some _ } ->
                            TextBlock.create [
                                TextBlock.text "Custom Font"
                            ]
                        | _ -> ()
                    ]
                ]
            ]
        ]
    )
