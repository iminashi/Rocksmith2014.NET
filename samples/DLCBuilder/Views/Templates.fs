module DLCBuilder.Views.Templates

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
open DLCBuilder
open DLCBuilder.Media

/// Returns a template for a tone.
let tone state dispatch index (t: Tone) =
    let title =
        if String.IsNullOrEmpty t.Key || t.Key = t.Name then
            t.Name
        else
            $"{t.Name} [{t.Key}]"

    let description =
        String.Join(" ", Array.map (ToneDescriptor.uiNameToName >> translate) t.ToneDescriptors)

    StackPanel.create [
        StackPanel.classes [ "listitem"; if state.SelectedToneIndex = index then "selected" ]
        StackPanel.onPointerPressed ((fun _ -> index |> SetSelectedToneIndex |> dispatch), SubPatchOptions.OnChangeOf index)
        StackPanel.onDoubleTapped (fun _ -> ShowToneEditor |> dispatch)
        StackPanel.contextMenu (Menus.Context.tone state dispatch)
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

/// Template for a tone descriptor.
let toneDescriptor =
    DataTemplateView<ToneDescriptor>.create (fun desc ->
        let text =
            let name = translate desc.Name
            if desc.IsExtra then $"{name} *" else name

        TextBlock.create [ TextBlock.text text ])

/// Template for an arrangement name.
let arrangementName =
    DataTemplateView<ArrangementName>.create (fun name ->
        let locString = $"{name}Arr"
        TextBlock.create [
            TextBlock.text (translate locString)
        ])

let private getArrangementNumber arr project =
    match arr with
    | Instrumental inst ->
        let groups =
            project.Arrangements
            |> List.choose Arrangement.pickInstrumental
            |> List.groupBy (fun a -> a.Priority, a.Name)
            |> Map.ofList

        let group = groups.[inst.Priority, inst.Name]
        if group.Length > 1 then
            sprintf " %i" (1 + (group |> List.findIndex (fun x -> x.PersistentID = inst.PersistentID)))
        else
            String.Empty
    | _ ->
        String.Empty

/// Returns the translated name for the arrangement.
let translateArrangementName arr project withExtra =
    match arr with
    | Instrumental inst ->
        let baseName =
            let n, p = Arrangement.getNameAndPrefix arr
            if p.Length > 0 then
                $"{translate p} {translate n}"
            else
                translate n

        let arrNumber = getArrangementNumber arr project
        let baseName = $"{baseName}{arrNumber}"

        if withExtra then
            let extra =
                if inst.Name = ArrangementName.Combo then
                    let c = translate "ComboArr" in $" ({c})"
                elif inst.RouteMask = RouteMask.Bass && inst.BassPicked then
                    let p = translate "picked" in $" ({p})"
                else
                    String.Empty
      
            $"{baseName}{extra}"
        else
            baseName
    | _ ->
        Arrangement.getNameAndPrefix arr |> fst |> translate

let private getExtraText = function
    | Instrumental inst ->
        let tuning =
            let tuningType, notes = Utils.getTuningName inst.Tuning
            match tuningType with
            | "DADGAD" -> tuningType
            | _ when notes.Length > 0 -> translatef tuningType notes
            | _ -> translate tuningType
        if inst.TuningPitch <> 440.0 then
            $"{tuning} (A{inst.TuningPitch})"
        else
            tuning
            
    | Vocals { CustomFont = Some _ } ->
        translate "customFont"
    | _ ->
        String.Empty

/// Returns a template for an arrangement.
let arrangement state dispatch index arr =
    let name = translateArrangementName arr state.Project true
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
    
    let xmlFile = Arrangement.getFile arr
    let hasIssues =
        state.ArrangementIssues
        |> Map.tryFind xmlFile
        |> Option.map (List.isEmpty >> not)

    let missingTones =
        match arr with
        | Instrumental inst ->
            [ inst.BaseTone; yield! inst.Tones ]
            |> List.distinct
            |> List.filter (fun toneKey ->
                state.Project.Tones
                |> List.exists (fun pt -> pt.Key = toneKey)
                |> not)
        | _ ->
            []

    DockPanel.create [
        DockPanel.classes [ "listitem"; if state.SelectedArrangementIndex = index then "selected" ]
        DockPanel.onPointerPressed ((fun _ -> index |> SetSelectedArrangementIndex |> dispatch), SubPatchOptions.OnChangeOf index)
        DockPanel.contextMenu (Menus.Context.arrangement state dispatch)
        if not <| missingTones.IsEmpty then
            let tip = translatef "missingDefinitions" [| String.Join(", ", missingTones) |]
            ToolTip.tip tip
        DockPanel.children [
            match hasIssues with
            | Some hasIssues ->
                // Validation Icon
                Path.create [
                    DockPanel.dock Dock.Right
                    Path.fill (if hasIssues then Brushes.Red else Brushes.Green)
                    Path.data (if hasIssues then Icons.x else Icons.check)
                    Path.verticalAlignment VerticalAlignment.Center
                    Path.margin (0., 0., 6., 0.)
                ]
            | None -> ()

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
                                    if missingTones.IsEmpty then Brushes.White else Brushes.OrangeRed
                                )
                            ]
            
                            // Extra Information
                            TextBlock.create [
                                TextBlock.foreground "#afafaf"
                                TextBlock.fontSize 14.
                                TextBlock.text (getExtraText arr)
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ] |> generalize
