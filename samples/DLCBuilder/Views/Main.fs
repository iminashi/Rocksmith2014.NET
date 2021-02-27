module DLCBuilder.Views.Main

open System
open Avalonia
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open DLCBuilder
open Media

let private arrangementContextMenu state dispatch =
    let isInstrumental = match state.SelectedArrangement with Some (Instrumental _) -> true | _ -> false
    ContextMenu.create [
        ContextMenu.isVisible state.SelectedArrangement.IsSome
        ContextMenu.viewItems [
            MenuItem.create [
                MenuItem.header (translate "generateNewArrIDs")
                MenuItem.isEnabled isInstrumental
                MenuItem.onClick (fun _ -> GenerateNewIds |> EditInstrumental |> dispatch)
                ToolTip.tip (translate "generateNewArrIDsToolTip")
            ]

            MenuItem.create [
                MenuItem.header (translate "reloadToneKeys")
                MenuItem.isEnabled isInstrumental
                MenuItem.onClick (fun _ -> UpdateToneInfo |> EditInstrumental |> dispatch)
                ToolTip.tip (translate "reloadToneKeysTooltip")
            ]

            MenuItem.create [ MenuItem.header "-" ]

            MenuItem.create [
                MenuItem.header (translate "remove")
                //MenuItem.inputGesture (KeyGesture(Key.Delete, KeyModifiers.None))
                MenuItem.onClick (fun _ -> dispatch DeleteArrangement)
            ]
        ]
    ]

let private toneContextMenu state dispatch =
    ContextMenu.create [
        ContextMenu.isVisible state.SelectedTone.IsSome
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

let view (window: Window) (state: State) dispatch =
    if state.RunningTasks.IsEmpty then
        window.Cursor <- Cursors.arrow
    else
        window.Cursor <- Cursors.wait
        
    window.Title <-
        match state.OpenProjectFile with
        | Some project ->
            let dot = if state.SavedProject <> state.Project then "*" else String.Empty
            $"{dot}Rocksmith 2014 DLC Builder - {project}"
        | None -> "Rocksmith 2014 DLC Builder"

    Grid.create [
        Grid.background "#040404"
        Grid.children [
            Grid.create [
                // Prevent tab navigation when and overlay is open
                Grid.isEnabled (state.Overlay = NoOverlay)
                Grid.columnDefinitions "*,1.8*"
                Grid.rowDefinitions "3*,2*"
                //Grid.showGridLines true
                Grid.children [
                    ProjectDetails.view state dispatch

                    Border.create [
                        Grid.column 1
                        Border.background "#181818"
                        Border.cornerRadius 6.
                        Border.margin 2.
                        Border.child (
                            // Arrangements
                            Grid.create [
                                Grid.columnDefinitions "245,*"
                                Grid.children [
                                    DockPanel.create [
                                        DockPanel.margin 4.
                                        StackPanel.children [
                                            // Title
                                            TextBlock.create [
                                                DockPanel.dock Dock.Top
                                                TextBlock.text (translate "arrangements")
                                                TextBlock.margin (0.0, 4.0)
                                                TextBlock.horizontalAlignment HorizontalAlignment.Center
                                            ]

                                            Grid.create [
                                                DockPanel.dock Dock.Top
                                                Grid.columnDefinitions "*,*"
                                                Grid.children [
                                                    // Add arrangement
                                                    Button.create [
                                                        Button.padding (15.0, 5.0)
                                                        Button.content (translate "addArrangement")
                                                        Button.onClick (fun _ -> dispatch SelectOpenArrangement)
                                                        // 5 instrumentals, 2 vocals, 1 showlights
                                                        Button.isEnabled (state.Project.Arrangements.Length < 8)
                                                    ]

                                                    // Validate arrangements
                                                    Button.create [
                                                        Grid.column 1
                                                        Button.padding (10.0, 5.0)
                                                        Button.content (translate "validate")
                                                        Button.onClick (fun _ -> dispatch CheckArrangements)
                                                        Button.isEnabled (
                                                            state.Project.Arrangements.Length > 0
                                                            &&
                                                            not (state.RunningTasks |> Set.contains ArrangementCheck))
                                                    ]
                                                ]
                                            ]

                                            // Arrangement list
                                            ListBox.create [
                                                ListBox.contextMenu (arrangementContextMenu state dispatch)
                                                ListBox.background Brushes.Transparent
                                                ListBox.virtualizationMode ItemVirtualizationMode.None
                                                ListBox.dataItems state.Project.Arrangements
                                                ListBox.itemTemplate Templates.arrangement
                                                match state.SelectedArrangement with
                                                | Some a -> ListBox.selectedItem a
                                                | None -> ()
                                                ListBox.onSelectedItemChanged (function
                                                    | :? Arrangement as arr -> arr |> Some |> SetSelectedArrangement |> dispatch
                                                    | _ -> ())
                                                ListBox.onKeyDown (fun k ->
                                                    if k.Key = Key.Delete then
                                                        k.Handled <- true
                                                        dispatch DeleteArrangement)
                                            ]
                                        ]
                                    ]

                                    // Arrangement details
                                    StackPanel.create [
                                        Grid.column 1
                                        StackPanel.background "#252525"
                                        StackPanel.verticalAlignment VerticalAlignment.Stretch
                                        StackPanel.children [
                                            match state.SelectedArrangement with
                                            | None ->
                                                TextBlock.create [
                                                    TextBlock.text (translate "selectArrangementPrompt")
                                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                                    TextBlock.verticalAlignment VerticalAlignment.Center
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
                                ]
                            ]
                        )
                    ]

                    Border.create [
                        Grid.column 1
                        Grid.row 1
                        Border.background "#181818"
                        Border.cornerRadius 6.
                        Border.margin 2.
                        Border.child (
                            // Tones
                            Grid.create [
                                Grid.columnDefinitions "245,*"
                                Grid.children [
                                    DockPanel.create [
                                        DockPanel.margin 4.
                                        DockPanel.children [
                                            // Title
                                            TextBlock.create [
                                                DockPanel.dock Dock.Top
                                                TextBlock.text (translate "tones")
                                                TextBlock.margin (0.0, 4.0)
                                                TextBlock.horizontalAlignment HorizontalAlignment.Center
                                            ]

                                            Grid.create [
                                                DockPanel.dock Dock.Top
                                                Grid.columnDefinitions "auto,*"
                                                Grid.children [
                                                    // Import from profile
                                                    Button.create [
                                                        Button.padding (15.0, 5.0)
                                                        Button.content (translate "fromProfile")
                                                        Button.onClick (fun _ -> dispatch ImportProfileTones)
                                                        Button.isEnabled (IO.File.Exists state.Config.ProfilePath)
                                                        ToolTip.tip (translate "profileImportToolTip")
                                                    ]

                                                    // Import from file
                                                    Button.create [
                                                        Grid.column 1
                                                        Button.padding (15.0, 5.0)
                                                        Button.content (translate "import")
                                                        Button.onClick (fun _ ->
                                                            Msg.OpenFileDialog("selectImportToneFile", Dialogs.toneImportFilter, ImportTonesFromFile)
                                                            |> dispatch)
                                                    ]
                                                ]
                                            ]

                                            // Tones list
                                            ListBox.create [
                                                ListBox.horizontalScrollBarVisibility ScrollBarVisibility.Auto
                                                ListBox.contextMenu (toneContextMenu state dispatch)
                                                ListBox.background Brushes.Transparent
                                                ListBox.dataItems state.Project.Tones
                                                ListBox.selectedItem (
                                                    match state.SelectedTone with
                                                    | Some t -> t
                                                    | None -> Unchecked.defaultof<Tone>)
                                                ListBox.onSelectedItemChanged (function
                                                    | :? Tone as tone -> tone |> Some |> SetSelectedTone |> dispatch
                                                    | _ -> ())
                                                ListBox.onDoubleTapped (fun _ -> ShowToneEditor |> dispatch)
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
                                        Grid.column 1
                                        StackPanel.background "#252525"
                                        StackPanel.children [
                                            match state.SelectedTone with
                                            | None ->
                                                TextBlock.create [
                                                    TextBlock.text(translate "selectTonePrompt")
                                                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                                ]
                                            | Some tone ->
                                                ToneDetails.view state dispatch tone
                                        ]
                                    ]
                                ]
                            ]
                        )
                    ]
                ]
            ]

            match state.Overlay with
            | NoOverlay -> ()
            | _ ->
                Grid.create [
                    Grid.children [
                        Rectangle.create [
                            Rectangle.fill "#99000000"
                            Rectangle.onTapped (fun _ -> CloseOverlay |> dispatch)
                        ]
                        Border.create [
                            Border.padding (20., 10.)
                            Border.cornerRadius 5.0
                            Border.horizontalAlignment HorizontalAlignment.Center
                            Border.verticalAlignment VerticalAlignment.Center
                            Border.background "#343434"
                            Border.child (
                                match state.Overlay with
                                | NoOverlay ->
                                    failwith "This can not happen."
                                | ErrorMessage(msg, info) ->
                                    ErrorMessage.view dispatch msg info
                                | SelectPreviewStart audioLength ->
                                    PreviewStartSelector.view state dispatch audioLength
                                | ImportToneSelector tones ->
                                    ImportTonesSelector.view dispatch tones
                                | ConfigEditor ->
                                    ConfigEditor.view state dispatch
                                | IssueViewer issues ->
                                    IssueViewer.view dispatch issues
                                | ToneEditor ->
                                    ToneEditor.view state dispatch state.SelectedTone.Value
                            )
                        ]
                    ]
                ]
        ]
    ]
