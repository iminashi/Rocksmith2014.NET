module DLCBuilder.Views.Main

open System
open Avalonia
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Rocksmith2014.DLCProject
open DLCBuilder
open Media

let private arrangementPanel state dispatch =
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
                    ScrollViewer.create [
                        ScrollViewer.content (
                            StackPanel.create [
                                StackPanel.focusable true
                                StackPanel.children (
                                    state.Project.Arrangements
                                    |> List.mapi (Templates.arrangement state dispatch)
                                )
                                StackPanel.onKeyDown ((fun e ->
                                    e.Handled <- true
                                    match e.KeyModifiers, e.Key with
                                    | KeyModifiers.None, Key.Delete ->
                                        dispatch DeleteArrangement
                                    | KeyModifiers.Alt, Key.Up ->
                                        dispatch (MoveArrangement Up)
                                    | KeyModifiers.Alt, Key.Down ->
                                        dispatch (MoveArrangement Down)
                                    | KeyModifiers.None, Key.Up ->
                                        if state.SelectedArrangementIndex > 0 then
                                            dispatch (SetSelectedArrangementIndex (state.SelectedArrangementIndex - 1))
                                    | KeyModifiers.None, Key.Down ->
                                        if state.SelectedArrangementIndex <> state.Project.Arrangements.Length - 1 then
                                            dispatch (SetSelectedArrangementIndex (state.SelectedArrangementIndex + 1))
                                    | _ ->
                                        e.Handled <- false), SubPatchOptions.OnChangeOf state.SelectedArrangementIndex)
                            ]
                        )
                    ]
                ]
            ]

            // Arrangement details
            StackPanel.create [
                Grid.column 1
                StackPanel.background "#252525"
                StackPanel.verticalAlignment VerticalAlignment.Stretch
                StackPanel.children [
                    match state.SelectedArrangementIndex with
                    | -1 ->
                        TextBlock.create [
                            TextBlock.text (translate "selectArrangementPrompt")
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.verticalAlignment VerticalAlignment.Center
                        ]

                    | index ->
                        let arr = state.Project.Arrangements.[index]
                        let xmlFile = Arrangement.getFile arr

                        // Arrangement name
                        TextBlock.create [
                            TextBlock.fontSize 17.
                            TextBlock.text (Templates.translateArrangementName arr state.Project false)
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

let private tonesPanel state dispatch =
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
                                ToolTip.tip (translate "profileImportToolTip" + " (Ctrl + P)")
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
                    ScrollViewer.create [
                        ScrollViewer.horizontalScrollBarVisibility ScrollBarVisibility.Auto
                        ScrollViewer.content (
                            StackPanel.create [
                                StackPanel.focusable true
                                StackPanel.children (
                                    state.Project.Tones
                                    |> List.mapi (Templates.tone state dispatch)
                                )
                                StackPanel.onKeyDown ((fun e ->
                                    e.Handled <- true
                                    match e.KeyModifiers, e.Key with
                                    | KeyModifiers.None, Key.Delete ->
                                        dispatch DeleteTone
                                    | KeyModifiers.Alt, Key.Up ->
                                        dispatch (MoveTone Up)
                                    | KeyModifiers.Alt, Key.Down ->
                                        dispatch (MoveTone Down)
                                    | KeyModifiers.None, Key.Up ->
                                        if state.SelectedToneIndex > 0 then
                                            dispatch (SetSelectedToneIndex (state.SelectedToneIndex - 1))
                                    | KeyModifiers.None, Key.Down ->
                                        if state.SelectedToneIndex <> state.Project.Tones.Length - 1 then
                                            dispatch (SetSelectedToneIndex (state.SelectedToneIndex + 1))
                                    | _ ->
                                        e.Handled <- false), SubPatchOptions.OnChangeOf state.SelectedToneIndex)
                            ]
                        )
                    ]
                ]
            ]

            // Tone details
            StackPanel.create [
                Grid.column 1
                StackPanel.background "#252525"
                StackPanel.children [
                    match state.SelectedToneIndex with
                    | -1 ->
                        TextBlock.create [
                            TextBlock.text(translate "selectTonePrompt")
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.verticalAlignment VerticalAlignment.Center
                        ]
                    | index ->
                        ToneDetails.view state dispatch state.Project.Tones.[index]
                ]
            ]
        ]
    ]

let private overlay state dispatch =
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
        match state.SelectedToneIndex with
        | -1 ->
            ErrorMessage.view dispatch "No tone selected. This should not happen." None
        | index ->
            ToneEditor.view state dispatch state.Project.Tones.[index]

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
                // Prevent tab navigation when an overlay is open
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
                        Border.child (arrangementPanel state dispatch)
                    ]

                    Border.create [
                        Grid.column 1
                        Grid.row 1
                        Border.background "#181818"
                        Border.cornerRadius 6.
                        Border.margin 2.
                        Border.child (tonesPanel state dispatch)
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
                            Border.cornerRadius 6.0
                            Border.horizontalAlignment HorizontalAlignment.Center
                            Border.verticalAlignment VerticalAlignment.Center
                            Border.background "#343434"
                            Border.child (overlay state dispatch)
                        ]
                    ]
                ]
        ]
    ]
