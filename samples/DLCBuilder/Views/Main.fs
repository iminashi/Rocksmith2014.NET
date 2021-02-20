module DLCBuilder.Views.Main

open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open System
open Avalonia
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Components.Hosts
open DLCBuilder
open Media

let view (window: HostWindow) (state: State) dispatch =
    if state.RunningTasks.IsEmpty then
        window.Cursor <- Cursors.arrow
    else
        window.Cursor <- Cursors.appStarting
        
    window.Title <-
        match state.OpenProjectFile with
        | Some project ->
            let dot = if state.SavedProject <> state.Project then "*" else String.Empty
            $"{dot}Rocksmith 2014 DLC Builder - {project}"
        | None -> "Rocksmith 2014 DLC Builder"

    Grid.create [
        Grid.children [
            Grid.create [
                Grid.columnDefinitions "2*,*,2*"
                Grid.rowDefinitions "3*,2*"
                //Grid.showGridLines true
                Grid.children [
                    ProjectDetails.view state dispatch

                    // Arrangements
                    DockPanel.create [
                        Grid.column 1
                        DockPanel.children [
                            DockPanel.create [
                                DockPanel.dock Dock.Top
                                DockPanel.margin 5.0

                                DockPanel.children [
                                    Button.create [
                                        DockPanel.dock Dock.Right
                                        Button.padding (10.0, 5.0)
                                        Button.content (translate "validate")
                                        Button.onClick (fun _ -> dispatch CheckArrangements)
                                        Button.isEnabled (state.Project.Arrangements.Length > 0 && not (state.RunningTasks |> Set.contains ArrangementCheck))
                                    ]

                                    // Add arrangement
                                    Button.create [
                                        DockPanel.dock Dock.Right
                                        Button.padding (15.0, 5.0)
                                        Button.content (translate "addArrangement")
                                        Button.onClick (fun _ -> dispatch SelectOpenArrangement)
                                        // 5 instrumentals, 2 vocals, 1 showlights
                                        Button.isEnabled (state.Project.Arrangements.Length < 8)
                                    ]

                                    // Title
                                    TextBlock.create [
                                        TextBlock.text (translate "arrangements")
                                        TextBlock.verticalAlignment VerticalAlignment.Bottom
                                    ]
                                ]
                            ]

                            ListBox.create [
                                ListBox.virtualizationMode ItemVirtualizationMode.None
                                ListBox.margin 2.
                                ListBox.dataItems state.Project.Arrangements
                                ListBox.itemTemplate Templates.arrangement
                                match state.SelectedArrangement with
                                | Some a -> ListBox.selectedItem a
                                | None -> ()
                                ListBox.onSelectedItemChanged ((fun item ->
                                    match item with
                                    | :? Arrangement as arr -> dispatch (ArrangementSelected (Some arr))
                                    | null when state.Project.Arrangements.Length = 0 -> dispatch (ArrangementSelected None)
                                    | _ -> ()), SubPatchOptions.OnChangeOf state.Project.Arrangements)
                                ListBox.onKeyDown (fun k ->
                                    if k.Key = Key.Delete then
                                        k.Handled <- true
                                        dispatch DeleteArrangement)
                            ]
                        ]
                    ]

                    // Arrangement details
                    StackPanel.create [
                        Grid.column 2
                        StackPanel.margin 8.
                        StackPanel.children [
                            match state.SelectedArrangement with
                            | None ->
                                TextBlock.create [
                                    TextBlock.text (translate "selectArrangementPrompt")
                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                ]

                            | Some arr ->
                                let xmlFile = Arrangement.getFile arr

                                // Arrangement name
                                TextBlock.create [
                                    TextBlock.fontSize 17.
                                    TextBlock.text (Arrangement.getHumanizedName arr)
                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                ]

                                // Arrangement filename
                                TextBlock.create [
                                    TextBlock.text (IO.Path.GetFileName xmlFile)
                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                ]

                                // Validation Icon
                                if state.ArrangementIssues.ContainsKey xmlFile then
                                    let noIssues = state.ArrangementIssues.[xmlFile].IsEmpty
                                    StackPanel.create [
                                        StackPanel.orientation Orientation.Horizontal
                                        StackPanel.background Brushes.Transparent
                                        if not noIssues then
                                            StackPanel.onTapped (fun _ -> dispatch ShowIssueViewer)
                                            StackPanel.cursor Cursors.hand
                                        StackPanel.children [
                                            Path.create [
                                                Path.fill (if noIssues then Brushes.Green else Brushes.Red)
                                                Path.data (if noIssues then Icons.check else Icons.x)
                                                Path.verticalAlignment VerticalAlignment.Center
                                                Path.margin (0., 0., 6., 0.)
                                            ]

                                            TextBlock.create[
                                                TextBlock.text (if noIssues then "OK" else translate "issues")
                                                TextBlock.verticalAlignment VerticalAlignment.Center
                                            ]
                                        ]
                                    ]

                                match arr with
                                | Showlights _ -> ()
                                | Instrumental i -> InstrumentalDetails.view state dispatch i
                                | Vocals v -> VocalsDetails.view dispatch v
                        ]
                    ]

                    // Tones
                    DockPanel.create [
                        Grid.column 1
                        Grid.row 1
                        DockPanel.children [
                            // Title
                            TextBlock.create [
                                DockPanel.dock Dock.Top
                                TextBlock.text (translate "tones")
                                TextBlock.margin 5.0
                            ]

                            StackPanel.create [
                                DockPanel.dock Dock.Top
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.spacing 4.
                                StackPanel.margin 5.
                                StackPanel.children [
                                    // Import from profile
                                    Button.create [
                                        Button.padding (15.0, 5.0)
                                        Button.horizontalAlignment HorizontalAlignment.Left
                                        Button.content (translate "fromProfile")
                                        Button.onClick (fun _ -> dispatch ImportProfileTones)
                                        Button.isEnabled (IO.File.Exists state.Config.ProfilePath)
                                        ToolTip.tip (translate "profileImportToolTip")
                                    ]
                                    // Import from a file
                                    Button.create [
                                        Button.padding (15.0, 5.0)
                                        Button.horizontalAlignment HorizontalAlignment.Left
                                        Button.content (translate "import")
                                        Button.onClick (fun _ -> dispatch (Msg.OpenFileDialog("selectImportToneFile", Dialogs.toneImportFilter, ImportTonesFromFile)))
                                    ]
                                ]
                            ]

                            ListBox.create [
                                ListBox.margin 2.
                                ListBox.dataItems state.Project.Tones
                                ListBox.selectedItem (
                                    match state.SelectedTone with
                                    | Some t -> t
                                    | None -> Unchecked.defaultof<Tone>)
                                ListBox.onSelectedItemChanged (function
                                    | :? Tone as tone -> tone |> Some |> ToneSelected |> dispatch
                                    | _ -> ())
                                ListBox.onKeyDown (fun k ->
                                    match k.KeyModifiers, k.Key with
                                    | KeyModifiers.None, Key.Delete -> dispatch DeleteTone
                                    | KeyModifiers.Alt, Key.Up -> dispatch (MoveTone Up)
                                    | KeyModifiers.Alt, Key.Down -> dispatch (MoveTone Down)
                                    | _ -> ())
                            ]
                        ]
                    ]

                    // Tone details
                    StackPanel.create [
                        Grid.column 2
                        Grid.row 1
                        StackPanel.margin 8.
                        StackPanel.children [
                            match state.SelectedTone with
                            | None ->
                                TextBlock.create [
                                    TextBlock.text(translate "selectTonePrompt")
                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                ]
                            | Some tone -> ToneDetails.view state dispatch tone
                        ]
                    ]
                ]
            ]

            match state.Overlay with
            | NoOverlay -> ()
            | _ ->
                Grid.create [
                    Grid.children [
                        Rectangle.create [
                            Rectangle.fill "#77000000"
                            Rectangle.onTapped (fun _ -> CloseOverlay |> dispatch)
                        ]
                        Border.create [
                            Border.padding (20., 10.)
                            Border.cornerRadius 5.0
                            Border.horizontalAlignment HorizontalAlignment.Center
                            Border.verticalAlignment VerticalAlignment.Center
                            Border.background "#444444"
                            Border.child (
                                match state.Overlay with
                                | NoOverlay -> failwith "This can not happen."
                                | ErrorMessage(msg, info) -> ErrorMessage.view dispatch msg info
                                | SelectPreviewStart audioLength -> SelectPreviewStart.view state dispatch audioLength
                                | ImportToneSelector tones -> SelectImportTones.view dispatch tones
                                | ConfigEditor -> ConfigEditor.view state dispatch
                                | IssueViewer issues -> IssueViewer.view dispatch issues
                                | ToneEditor -> ToneEditor.view state dispatch state.SelectedTone.Value
                            )
                        ]
                    ]
                ]
        ]
    ]
