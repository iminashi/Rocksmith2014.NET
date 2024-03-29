module DLCBuilder.Views.Templates

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Controls.Templates
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Interactivity
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Rocksmith2014.XML.Processing
open System
open DLCBuilder
open DLCBuilder.Media
open DLCBuilder.ArrangementNameUtils

/// Returns a template for a tone.
let tone state dispatch index (t: Tone) =
    let noArrangementUsesTone () =
        state.Project.Arrangements
        |> List.exists (Arrangement.getTones >> List.contains t.Key)
        |> not

    let isKeyless = String.IsNullOrEmpty(t.Key)
    let isUnused = isKeyless || noArrangementUsesTone ()

    let title =
        if isUnused then
            let nameStr = if String.IsNullOrEmpty(t.Name) then String.Empty else $"{t.Name} "

            sprintf "%s(%s)" nameStr (translate "Unused")
        elif t.Key = t.Name then
            t.Name
        else
            $"{t.Key} ({t.Name})"

    let description =
        String.Join(" ", Array.map (ToneDescriptor.uiNameToName >> translate) t.ToneDescriptors)

    StackPanel.create [
        StackPanel.classes [
            "list-item"
            if state.SelectedToneIndex = index then "selected"
        ]
        StackPanel.onPointerPressed ((fun e ->
            if e.Route = RoutingStrategies.Bubble then
                index |> SetSelectedToneIndex |> dispatch),
            SubPatchOptions.OnChangeOf index)
        StackPanel.onDoubleTapped (fun _ -> ShowToneEditor |> dispatch)
        StackPanel.contextMenu (Menus.Context.tone state dispatch)
        if isUnused then
            ToolTip.tip (translate "ToneIsUnused")
        StackPanel.children [
            // Title
            TextBlock.create [
                TextBlock.margin (6., 4., 6., 2.)
                TextBlock.fontSize 16.
                TextBlock.text title
                if isUnused then TextBlock.foreground "#afafaf"
            ]
            // Description
            TextBlock.create [
                TextBlock.margin (6., 4., 6., 2.)
                TextBlock.foreground "#afafaf"
                TextBlock.fontSize 14.
                TextBlock.text description
            ]
        ]
    ] |> generalize

/// Template for a tone descriptor.
let toneDescriptor : IDataTemplate =
    DataTemplateView<ToneDescriptor>.create (fun desc ->
        let text =
            let name = translate desc.Name
            if desc.IsExtra then $"{name} *" else name

        TextBlock.create [ TextBlock.text text ])

/// Template for an arrangement name.
let arrangementName : IDataTemplate =
    DataTemplateView<ArrangementName>.create (fun name -> locText $"{name}Arr" [])

let private getExtraText arr =
    match arr with
    | Instrumental inst ->
        let tuning =
            let actualTuning =
                if inst.TuningPitch = 220.0 && inst.RouteMask = RouteMask.Bass then
                    inst.Tuning |> Array.map (fun x -> x - 12s)
                else
                    inst.Tuning

            let tuningName = Utils.getTuningName actualTuning
            match tuningName with
            | Utils.CustomTuning(tuning) ->
                tuning
            | Utils.TranslatableTuning(tuning, notes) ->
                translatef tuning notes

        if inst.TuningPitch <> 440.0 then
            $"{tuning} (A{inst.TuningPitch})"
        else
            tuning

    | Vocals { CustomFont = Some _ } ->
        translate "CustomFont"
    | _ ->
        String.Empty

/// Returns a template for an arrangement.
let arrangement state dispatch index arr =
    let name = translateName state.Project WithExtra arr
    let icon, color =
        match arr with
        | Instrumental inst ->
            let color =
                match inst.RouteMask with
                | RouteMask.Lead -> Brushes.lead
                | RouteMask.Bass -> Brushes.bass
                | _ -> Brushes.rhythm

            Icons.guitar, color

        | Vocals v ->
            Icons.microphone, if v.Japanese then Brushes.jvocals else Brushes.vocals

        | Showlights _ ->
            Icons.spotlight, Brushes.showlights

    let noIssues =
        state.ArrangementIssues
        |> Map.tryFind (Arrangement.getId arr)
        |> Option.map (List.forall (fun x -> state.Project.IgnoredIssues.Contains(issueCode x.IssueType)))

    let missingTones =
        match arr with
        | Instrumental inst ->
            inst.BaseTone :: inst.Tones
            |> List.distinct
            |> List.filter (fun toneKey ->
                state.Project.Tones
                |> List.exists (fun pt -> pt.Key = toneKey)
                |> not)
        | _ ->
            List.empty

    let isEmptyBaseToneKey =
        match arr with
        | Instrumental inst when String.IsNullOrWhiteSpace(inst.BaseTone) ->
            true
        | _ ->
            false

    DockPanel.create [
        DockPanel.classes [ "list-item"; if state.SelectedArrangementIndex = index then "selected" ]
        DockPanel.onPointerPressed ((fun e ->
            if e.Route = RoutingStrategies.Bubble then
                index |> SetSelectedArrangementIndex |> dispatch),
            SubPatchOptions.OnChangeOf index)
        DockPanel.onDoubleTapped (fun _ -> ShowIssueViewer |> dispatch)
        DockPanel.contextMenu (Menus.Context.arrangement state dispatch)
        if isEmptyBaseToneKey then
            ToolTip.tip (translate "EmptyBaseToneToolTip")
        elif missingTones.Length <> 0 then
            translatef "MissingToneDefinitionsToolTip" [| String.Join(", ", missingTones) |]
            |> ToolTip.tip
        DockPanel.children [
            match noIssues with
            | Some noIssues ->
                // Validation Icon
                Path.create [
                    DockPanel.dock Dock.Right
                    Path.fill (if noIssues then Brushes.Green else Brushes.Red)
                    Path.data (if noIssues then Icons.check else Icons.x)
                    Path.verticalAlignment VerticalAlignment.Center
                    Path.margin (0., 0., 6., 0.)
                ]
            | None ->
                ()

            StackPanel.create [
                StackPanel.margin (6., 8.)
                StackPanel.minHeight 30.
                StackPanel.orientation Orientation.Horizontal
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
                                TextBlock.foreground (
                                    if missingTones.IsEmpty then Brushes.White else Brushes.Red
                                )
                            ]

                            // Extra Information
                            let extraText = getExtraText arr
                            TextBlock.create [
                                TextBlock.foreground "#afafaf"
                                TextBlock.fontSize 14.
                                TextBlock.text extraText
                                TextBlock.isVisible (String.notEmpty extraText)
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ] |> generalize
