module DLCBuilder.Views.Main

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open DLCBuilder
open Media

let private arrangementList state dispatch =
    ScrollViewer.create [
        ScrollViewer.horizontalScrollBarVisibility ScrollBarVisibility.Auto
        ScrollViewer.content (
            StackPanel.create [
                StackPanel.focusable true
                StackPanel.children (
                    state.Project.Arrangements
                    |> List.mapi (Templates.arrangement state dispatch)
                )
                if state.Overlay = NoOverlay then
                    StackPanel.onKeyDown ((fun e ->
                        e.Handled <- true
                        let arrIndex = state.SelectedArrangementIndex
                        match e.KeyModifiers, e.Key with
                        | KeyModifiers.None, Key.Delete ->
                            dispatch DeleteArrangement
                        | KeyModifiers.Alt, Key.Up ->
                            dispatch (MoveArrangement Up)
                        | KeyModifiers.Alt, Key.Down ->
                            dispatch (MoveArrangement Down)
                        | KeyModifiers.None, Key.Up when arrIndex > 0 ->
                            dispatch (SetSelectedArrangementIndex (arrIndex - 1))
                        | KeyModifiers.None, Key.Down when arrIndex <> state.Project.Arrangements.Length - 1 ->
                            dispatch (SetSelectedArrangementIndex (arrIndex + 1))
                        | _ ->
                            e.Handled <- false), SubPatchOptions.OnChangeOf state.SelectedArrangementIndex)
            ]
        )
    ]

let private arrangementDetails state dispatch =
    ScrollViewer.create [
        Grid.column 1
        ScrollViewer.content (
            StackPanel.create [
                StackPanel.background "#252525"
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

                        StackPanel.create [
                            StackPanel.orientation Orientation.Horizontal
                            StackPanel.horizontalAlignment HorizontalAlignment.Center
                            StackPanel.margin (0., 2., 0., 0.)
                            StackPanel.children [
                                // Arrangement name
                                TextBlock.create [
                                    TextBlock.fontSize 17.
                                    TextBlock.text (Templates.translateArrangementName arr state.Project Templates.NameOnly)
                                    TextBlock.verticalAlignment VerticalAlignment.Bottom
                                ]

                                // Arrangement filename
                                TextBlock.create [
                                    TextBlock.fontSize 12.
                                    TextBlock.margin (8., 0.)
                                    TextBlock.text $"{IO.Path.GetFileName xmlFile}"
                                    TextBlock.foreground "#cccccc"
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                ]
                            ]
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
        )
    ]

let private arrangementPanel state dispatch =
    Grid.create [
        Grid.columnDefinitions "*,2.5*"
        Grid.children [
            DockPanel.create [
                DockPanel.margin 4.
                DockPanel.children [
                    // Title
                    locText "arrangements" [
                        DockPanel.dock Dock.Top
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
                                Button.onClick (fun _ -> Dialog.AddArrangements |> ShowDialog |> dispatch)
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
                    arrangementList state dispatch
                ]
            ]

            // Arrangement details
            arrangementDetails state dispatch
        ]
    ]

let private tonesList state dispatch =
    ScrollViewer.create [
        ScrollViewer.horizontalScrollBarVisibility ScrollBarVisibility.Auto
        ScrollViewer.content (
            StackPanel.create [
                StackPanel.focusable true
                StackPanel.children (
                    state.Project.Tones
                    |> List.mapi (Templates.tone state dispatch)
                )
                if state.Overlay = NoOverlay then
                    StackPanel.onKeyDown ((fun e ->
                        e.Handled <- true
                        let toneIndex = state.SelectedToneIndex
                        match e.KeyModifiers, e.Key with
                        | KeyModifiers.None, Key.Delete ->
                            dispatch DeleteTone
                        | KeyModifiers.Alt, Key.Up ->
                            dispatch (MoveTone Up)
                        | KeyModifiers.Alt, Key.Down ->
                            dispatch (MoveTone Down)
                        | KeyModifiers.None, Key.Up when toneIndex > 0 ->
                            dispatch (SetSelectedToneIndex (toneIndex - 1))
                        | KeyModifiers.None, Key.Down when toneIndex <> state.Project.Tones.Length - 1 ->
                            dispatch (SetSelectedToneIndex (toneIndex + 1))
                        | _ ->
                            e.Handled <- false), SubPatchOptions.OnChangeOf state.SelectedToneIndex)
            ]
        )
    ]

let private tonesPanel state dispatch =
    Grid.create [
        Grid.columnDefinitions "*,2.5*"
        Grid.children [
            DockPanel.create [
                DockPanel.margin 4.
                DockPanel.children [
                    // Title
                    StackPanel.create [
                        DockPanel.dock Dock.Top
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.horizontalAlignment HorizontalAlignment.Center
                        StackPanel.children [
                            locText "tones" [
                                TextBlock.margin (0.0, 4.0)
                                TextBlock.verticalAlignment VerticalAlignment.Center
                            ]

                            Menus.addTone state dispatch 
                        ] 
                    ]

                    // Tones list
                    tonesList state dispatch
                ]
            ]

            // Tone details
            StackPanel.create [
                Grid.column 1
                StackPanel.background "#252525"
                StackPanel.children [
                    match state.SelectedToneIndex with
                    | -1 ->
                        locText "selectTonePrompt" [
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
    | ErrorMessage (msg, info) ->
        ErrorMessage.view dispatch msg info
    | SelectPreviewStart audioLength ->
        PreviewStartSelector.view state dispatch audioLength
    | ImportToneSelector tones ->
        ImportTonesSelector.view state dispatch tones
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
    | ToneCollection collectionState ->
        let dispatch = ToneCollectionMsg >> dispatch
        ToneCollectionOverlay.view dispatch collectionState
    | DeleteConfirmation files ->
        DeleteConfirmation.view dispatch files
    | PitchShifter ->
        PitchShifter.view dispatch state
    | AbnormalExitMessage ->
        AbnormalExitMessage.view dispatch
    | AboutMessage ->
        AboutMessage.view dispatch
    | UpdateInformationDialog update ->
        UpdateInfoMessage.view update dispatch

let private statusMessageContents dispatch = function
    | TaskWithProgress (task, progress) ->
        StackPanel.create [
            StackPanel.horizontalAlignment HorizontalAlignment.Center
            StackPanel.children [
                TextBlock.create [
                    TextBlock.text (task |> string |> translate)
                ]
                ProgressBar.create [
                    ProgressBar.maximum 100.
                    ProgressBar.value progress
                ]
            ]
        ] |> generalize

    | TaskWithoutProgress task ->
        TextBlock.create [
            TextBlock.text (
                match task with
                | VolumeCalculation (CustomAudio(_)) ->
                    translate $"VolumeCalculationCustomAudio"
                | VolumeCalculation target ->
                    translate $"VolumeCalculation{target}"
                | other ->
                    other |> string |> translate)
        ] |> generalize

    | MessageString (_, message) ->
        TextBlock.create [
            TextBlock.horizontalAlignment HorizontalAlignment.Center
            TextBlock.text message
        ] |> generalize

    | UpdateMessage update ->
        let verType = translate (update.AvailableUpdate.ToString())
        let message = translatef "updateAvailable" [| verType |]
        StackPanel.create [
            StackPanel.horizontalAlignment HorizontalAlignment.Center
            StackPanel.children [
                TextBlock.create [
                    TextBlock.text message
                ]

                UniformGrid.create [
                    UniformGrid.rows 1
                    UniformGrid.columns 2
                    UniformGrid.margin 4.
                    UniformGrid.children [
                        TextBlock.create [
                            TextBlock.classes [ "link" ]
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.text (translate "details")
                            TextBlock.onTapped (fun _ ->
                                dispatch DismissUpdateMessage
                                dispatch ShowUpdateInformation)
                        ]
                        TextBlock.create [
                            TextBlock.classes [ "link" ]
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.text (translate "dismiss")
                            TextBlock.onTapped (fun _ -> dispatch DismissUpdateMessage)
                        ]
                    ]
                ]
            ]
        ] |> generalize

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
        | None ->
            "Rocksmith 2014 DLC Builder"

    Panel.create [
        Panel.background "#040404"
        Panel.children [
            DockPanel.create [
                // Prevent tab navigation when an overlay is open
                DockPanel.isEnabled (state.Overlay = NoOverlay)
                DockPanel.children [
                    // Main menu
                    Menu.create [
                        DockPanel.dock Dock.Top
                        Menu.background "#181818"
                        Menu.viewItems [
                            Menus.file state dispatch

                            Menus.build state dispatch

                            Menus.tools state dispatch

                            Menus.help dispatch
                        ]
                    ]

                    Grid.create [
                        Grid.columnDefinitions "*,1.8*"
                        Grid.rowDefinitions "3*,2*"
                        Grid.children [
                            // Project details
                            ProjectDetails.view state dispatch

                            // Arrangements
                            Border.create [
                                Grid.column 1
                                Border.background "#181818"
                                Border.cornerRadius 6.
                                Border.margin 2.
                                Border.child (arrangementPanel state dispatch)
                            ]

                            // Tones
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
                ]
            ]

            match state.Overlay with
            | NoOverlay ->
                ()
            | _ ->
                Panel.create [
                    Panel.children [
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

            if not state.StatusMessages.IsEmpty then
                StackPanel.create [
                    StackPanel.horizontalAlignment HorizontalAlignment.Right
                    StackPanel.verticalAlignment VerticalAlignment.Bottom
                    StackPanel.margin 8.
                    StackPanel.children (
                        state.StatusMessages
                        |> List.map (fun message ->
                            Border.create [
                                Border.margin (0., 1.)
                                Border.padding (20., 10.)
                                Border.cornerRadius 6.0
                                Border.minWidth 250.
                                Border.background Brushes.Black
                                Border.child (statusMessageContents dispatch message)
                            ] |> generalize)
                    )
                ]
        ]
    ]
