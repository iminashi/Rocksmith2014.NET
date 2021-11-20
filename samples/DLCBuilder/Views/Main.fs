module DLCBuilder.Views.Main

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Rocksmith2014.DLCProject
open System
open DLCBuilder
open Media

let private arrangementList state dispatch =
    ListBoxEx.create [
        ListBoxEx.selectedIndex state.SelectedArrangementIndex
        ListBoxEx.onSelectedIndexChanged (SetSelectedArrangementIndex >> dispatch)
        ListBoxEx.onItemMoved (MoveArrangement >> dispatch)
        ListBoxEx.onItemDeleted (fun () -> DeleteSelectedArrangement |> dispatch)
        ListBoxEx.children (
            state.Project.Arrangements
            |> List.mapi (Templates.arrangement state dispatch))
    ]

let private validationIcon dispatch noIssues =
    StackPanel.create [
        StackPanel.margin (12., 0.)
        StackPanel.horizontalAlignment HorizontalAlignment.Left
        StackPanel.orientation Orientation.Horizontal
        StackPanel.background Brushes.Transparent
        if not noIssues then
            StackPanel.onTapped (fun _ -> dispatch ShowIssueViewer)
            StackPanel.onKeyDown (fun args ->
                if args.Key = Key.Space then
                    args.Handled <- true
                    dispatch ShowIssueViewer)
            StackPanel.cursor Cursors.hand
            StackPanel.focusable true
        StackPanel.children [
            Path.create [
                Path.fill (if noIssues then Brushes.Green else Brushes.Red)
                Path.data (if noIssues then Icons.check else Icons.x)
                Path.verticalAlignment VerticalAlignment.Center
                Path.margin (0., 0., 6., 0.)
            ]

            TextBlock.create [
                TextBlock.text <| translate (if noIssues then "OK" else "Issues")
                TextBlock.verticalAlignment VerticalAlignment.Center
            ]
        ]
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
                        locText "SelectArrangementPrompt" [
                            TextBlock.foreground Brushes.Gray
                            TextBlock.margin 4.
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                        ]

                    | index ->
                        let arr = state.Project.Arrangements.[index]
                        let xmlFile = Arrangement.getFile arr

                        Panel.create [
                            Panel.margin (0., 4., 0., 0.)
                            Panel.children [
                                StackPanel.create [
                                    StackPanel.orientation Orientation.Horizontal
                                    StackPanel.horizontalAlignment HorizontalAlignment.Center
                                    StackPanel.children [
                                        // Arrangement name
                                        TextBlock.create [
                                            TextBlock.fontSize 17.
                                            TextBlock.text (ArrangementNameUtils.translateName state.Project ArrangementNameUtils.NameOnly arr)
                                            TextBlock.verticalAlignment VerticalAlignment.Bottom
                                        ]

                                        // Arrangement filename
                                        TextBlock.create [
                                            TextBlock.fontSize 12.
                                            TextBlock.margin (8., 0.)
                                            TextBlock.text $"{IO.Path.GetFileName(xmlFile)}"
                                            TextBlock.foreground "#cccccc"
                                            TextBlock.verticalAlignment VerticalAlignment.Center
                                        ]
                                    ]
                                ]

                                // Validation Icon
                                if state.ArrangementIssues.ContainsKey xmlFile then
                                    let noIssues = state.ArrangementIssues.[xmlFile].IsEmpty
                                    validationIcon dispatch noIssues
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
                DockPanel.margin (4., 4., 0., 4.)
                DockPanel.children [
                    // Title
                    Grid.create [
                        DockPanel.dock Dock.Top
                        Grid.columnDefinitions "*,auto,auto"
                        Grid.rowDefinitions "auto,auto"
                        Grid.children [
                            locText "Arrangements" [
                                TextBlock.margin (8., 4., 0., 4.)
                                TextBlock.verticalAlignment VerticalAlignment.Center
                            ]

                            // Add arrangement
                            Button.create [
                                Grid.column 1
                                Button.classes [ "borderless-btn" ]
                                Button.padding (10., 8.)
                                Button.content (
                                    PathIcon.create [
                                        PathIcon.data Icons.plus
                                        PathIcon.width 16.
                                        PathIcon.height 16.
                                    ])
                                Button.onClick (fun _ -> Dialog.AddArrangements |> ShowDialog |> dispatch)
                                // 5 instrumentals, 2 vocals, 1 showlights
                                Button.isEnabled (state.Project.Arrangements.Length < 8)
                                ToolTip.tip (translate "AddArrangementToolTip")
                            ]

                            // Validate arrangements
                            Button.create [
                                Grid.column 2
                                Button.classes [ "borderless-btn" ]
                                Button.padding (10., 8.)
                                Button.content (
                                    PathIcon.create [
                                        PathIcon.data Icons.checkList
                                    ])
                                Button.onClick (fun _ -> dispatch CheckArrangements)
                                Button.isEnabled (StateUtils.canRunValidation state)
                                ToolTip.tip (translate "ValidateArrangementsToolTip")
                            ]

                            Rectangle.create [
                                Grid.row 1
                                Grid.columnSpan 3
                                Rectangle.height 1.
                                Rectangle.fill Brushes.Gray
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
    ListBoxEx.create [
        ListBoxEx.selectedIndex state.SelectedToneIndex
        ListBoxEx.onSelectedIndexChanged (SetSelectedToneIndex >> dispatch)
        ListBoxEx.onItemMoved (MoveTone >> dispatch)
        ListBoxEx.onItemDeleted (fun () -> DeleteSelectedTone |> dispatch)
        ListBoxEx.children (
            state.Project.Tones
            |> List.mapi (Templates.tone state dispatch))
    ]

let private tonesPanel state dispatch =
    Grid.create [
        Grid.columnDefinitions "*,2.5*"
        Grid.children [
            DockPanel.create [
                DockPanel.margin (4., 4., 0., 4.)
                DockPanel.children [
                    // Title
                    Grid.create [
                        DockPanel.dock Dock.Top
                        Grid.columnDefinitions "*,auto"
                        Grid.rowDefinitions "auto,auto"
                        Grid.children [
                            locText "Tones" [
                                TextBlock.margin (8., 4., 0., 4.)
                                TextBlock.verticalAlignment VerticalAlignment.Center
                            ]

                            Menus.addTone dispatch

                            Rectangle.create [
                                Grid.row 1
                                Grid.columnSpan 2
                                Rectangle.height 1.
                                Rectangle.fill Brushes.Gray
                            ]
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
                        locText "SelectTonePrompt" [
                            TextBlock.foreground Brushes.Gray
                            TextBlock.margin 4.
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
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
    | IdRegenerationConfirmation (arrangements, reply) ->
        IdRegenerationConfirmation.view state dispatch reply.Reply arrangements
    | ErrorMessage (msg, info) ->
        ErrorMessage.view dispatch msg info
    | SelectPreviewStart data ->
        PreviewStartSelector.view state dispatch data
    | ImportToneSelector tones ->
        ImportTonesSelector.view state dispatch tones
    | ConfigEditor focusedSetting ->
        ConfigEditor.view state dispatch focusedSetting
    | IssueViewer arrangement ->
        IssueViewer.view state dispatch arrangement
    | JapaneseLyricsCreator editorState ->
        JapaneseLyricsCreatorOverlay.view state dispatch editorState
    | ToneEditor ->
        match state.SelectedToneIndex with
        | -1 ->
            ErrorMessage.view dispatch "No tone selected. This should not happen." None
        | index ->
            ToneEditor.view state dispatch state.Project.Tones.[index]
    | ToneCollection collectionState ->
        ToneCollectionOverlay.view state dispatch collectionState
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
                    TextBlock.fontSize 15.
                    TextBlock.text (
                        match task with
                        | FileDownload { LocString = s } ->
                            translate s
                        | other ->
                            other |> string |> translate)
                ]
                ProgressBar.create [
                    ProgressBar.maximum 100.
                    ProgressBar.isIndeterminate (progress < 0.0)
                    ProgressBar.value progress
                ]
            ]
        ] |> generalize

    | TaskWithoutProgress task ->
        TextBlock.create [
            TextBlock.text (
                match task with
                | VolumeCalculation (CustomAudio _) ->
                    translate $"VolumeCalculationCustomAudio"
                | VolumeCalculation target ->
                    translate $"VolumeCalculation{target}"
                | FileDownload { LocString = s } ->
                    translate s
                | other ->
                    other |> string |> translate)
        ] |> generalize

    | MessageString (_, message) ->
        TextBlock.create [
            TextBlock.horizontalAlignment HorizontalAlignment.Center
            TextBlock.fontSize 15.
            TextBlock.text message
        ] |> generalize

    | UpdateMessage update ->
        let verType = translate (update.AvailableUpdate.ToString())
        let message = translatef "UpdateAvailable" [| verType |]
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
                            TextBlock.text (translate "Details")
                            TextBlock.onTapped (fun _ ->
                                dispatch DismissUpdateMessage
                                dispatch ShowUpdateInformation)
                        ]
                        TextBlock.create [
                            TextBlock.classes [ "link" ]
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.text (translate "Dismiss")
                            TextBlock.onTapped (fun _ -> dispatch DismissUpdateMessage)
                        ]
                    ]
                ]
            ]
        ] |> generalize

let view (customTitleBar: TitleBarButtons option) (window: Window) (state: State) dispatch =
    if state.RunningTasks.IsEmpty then
        window.Cursor <- Cursors.arrow
    else
        window.Cursor <- Cursors.wait

    let title, titleToolTip =
        match state.OpenProjectFile with
        | Some project ->
            let dot = if state.SavedProject <> state.Project then "*" else String.Empty
            $"{dot}{state.Project.ArtistName.Value} - {state.Project.Title.Value}", project
        | None ->
            "Rocksmith 2014 DLC Builder", String.Empty

    if customTitleBar.IsNone then
        window.Title <- title

    let noOverlayIsOpen = (state.Overlay = NoOverlay)

    DockPanel.create [
        DockPanel.background "#040404"
        DragDrop.allowDrop noOverlayIsOpen
        DragDrop.onDragEnter (fun e ->
            e.DragEffects <-
                if e.Data.Contains(DataFormats.FileNames) then
                    DragDropEffects.Copy
                else
                    DragDropEffects.None)

        DragDrop.onDrop (fun e ->
            e.Handled <- true

            e.Data.GetFileNames()
            |> LoadMultipleFiles
            |> dispatch)

        DockPanel.children [
            // Custom title bar
            Panel.create [
                DockPanel.dock Dock.Top
                Panel.children [
                    Rectangle.create [
                        Rectangle.fill "#181818"
                        Rectangle.horizontalAlignment HorizontalAlignment.Stretch
                        Rectangle.verticalAlignment VerticalAlignment.Stretch
                        if customTitleBar.IsSome then
                            Rectangle.onPointerPressed window.PlatformImpl.BeginMoveDrag
                            Rectangle.onDoubleTapped (fun _ -> maximizeOrRestore window)
                    ]

                    DockPanel.create [
                        DockPanel.children [
                            // Main menu
                            Menu.create [
                                DockPanel.dock Dock.Left
                                KeyboardNavigation.tabNavigation (
                                    if noOverlayIsOpen then
                                        KeyboardNavigationMode.Continue
                                    else
                                        KeyboardNavigationMode.None)
                                Menu.isEnabled noOverlayIsOpen
                                Menu.horizontalAlignment HorizontalAlignment.Left
                                Menu.background Brushes.Transparent
                                Menu.viewItems [
                                    Menus.file state dispatch

                                    Menus.project state dispatch

                                    Menus.build state dispatch

                                    Menus.tools state dispatch

                                    Menus.help dispatch
                                ]
                            ]

                            // Custom minimize, maximize & close buttons
                            match customTitleBar with
                            | Some buttons ->
                                Border.create [
                                    DockPanel.dock Dock.Right
                                    Border.child buttons
                                ]
                            | None ->
                                ()

                            // Configuration shortcut button
                            Button.create [
                                DockPanel.dock Dock.Right
                                Button.classes [ "borderless-btn" ]
                                Button.isEnabled noOverlayIsOpen
                                KeyboardNavigation.isTabStop false
                                Button.onClick (fun _ -> FocusedSetting.None |> ConfigEditor |> ShowOverlay |> dispatch)
                                Button.content (
                                    PathIcon.create [
                                        PathIcon.data Icons.cog
                                        PathIcon.foreground Brushes.GhostWhite
                                    ])
                            ]

                            // Title Text
                            TextBlock.create [
                                TextBlock.verticalAlignment VerticalAlignment.Center
                                TextBlock.horizontalAlignment HorizontalAlignment.Center
                                if customTitleBar.IsSome then
                                    TextBlock.text title
                                    TextBlock.onPointerPressed window.BeginMoveDrag
                                    TextBlock.onDoubleTapped (fun _ -> maximizeOrRestore window)
                                    if String.notEmpty titleToolTip then
                                        ToolTip.tip titleToolTip
                            ]
                        ]
                    ]
                ]
            ]

            Panel.create [
                Panel.children [
                    Grid.create [
                        Grid.columnDefinitions "*,1.8*"
                        Grid.rowDefinitions "3.4*,2.6*"
                        // Prevent tab navigation when an overlay is open
                        KeyboardNavigation.tabNavigation (
                            if noOverlayIsOpen then
                                KeyboardNavigationMode.Continue
                            else
                                KeyboardNavigationMode.None)
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

                    match state.Overlay with
                    | NoOverlay ->
                        ()
                    | _ ->
                        Panel.create [
                            KeyboardNavigation.tabNavigation KeyboardNavigationMode.Cycle
                            Panel.children [
                                Rectangle.create [
                                    Rectangle.fill "#99000000"
                                    Rectangle.onTapped (fun _ -> (CloseOverlay OverlayCloseMethod.ClickedOutside) |> dispatch)
                                ]
                                Border.create [
                                    Border.padding (20., 10.)
                                    Border.cornerRadius 6.0
                                    Border.boxShadow BoxShadow.overlay
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
        ]
    ]
