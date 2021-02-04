module DLCBuilder.Views.ProjectDetails

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.DLCProject
open Rocksmith2014.Common
open System
open DLCBuilder
open Media

let private notBuilding state =
    state.RunningTasks
    |> Set.intersect (Set([ BuildPackage; WemConversion ]))
    |> Set.isEmpty

let view state dispatch =
    let audioPath = state.Project.AudioFile.Path
    let noBuildInProgress = notBuilding state
    let notCalculatingVolume =
        not (state.RunningTasks |> Set.exists (function VolumeCalculation (MainAudio | PreviewAudio) -> true | _ -> false))

    let canBuild =
        noBuildInProgress
        && (not <| state.RunningTasks.Contains PsarcImport)
        && state.Project.Arrangements.Length > 0
        && state.Project.DLCKey.Length >= 5
        && String.notEmpty audioPath

    DockPanel.create [
        Grid.rowSpan 2
        DockPanel.margin (8.0, 8.0, 0.0, 8.0)
        DockPanel.children [
            Image.create [
                DockPanel.dock Dock.Top
                Image.source state.CoverArt
                Image.width 200.
                Image.height 200.
                Image.onTapped (fun _ -> dispatch (Msg.OpenFileDialog("selectCoverArt", Dialogs.imgFileFilter, SetCoverArt)))
                Image.cursor Cursors.hand
                ToolTip.tip (state.Localization.GetString "selectCoverArtToolTip")
            ]

            Grid.create [
                Grid.columnDefinitions "3*,*"
                Grid.rowDefinitions "46,46,46,46,46,40,40,*"
                //Grid.showGridLines true
                Grid.children [
                    TitledTextBox.create (state.Localization.GetString "dlcKey") [ Grid.column 0; Grid.row 0 ] [
                        TextBox.text state.Project.DLCKey
                        // Cannot filter pasted text: https://github.com/AvaloniaUI/Avalonia/issues/2611
                        TextBox.onTextInput (fun e -> e.Text <- StringValidator.dlcKey e.Text)
                        TextBox.onTextChanged (fun e ->
                            fun p -> { p with DLCKey = StringValidator.dlcKey e }
                            |> EditProject
                            |> dispatch)
                        // Display the validated DLC key if invalid characters were pasted into the textbox
                        TextBox.onLostFocus (
                            (fun e -> (e.Source :?> TextBox).Text <- state.Project.DLCKey),
                            SubPatchOptions.OnChangeOf state.Project.DLCKey)
                        ToolTip.tip (state.Localization.GetString "dlcKeyTooltip")
                    ]

                    TitledTextBox.create (state.Localization.GetString "version") [ Grid.column 1; Grid.row 0 ] [
                        TextBox.horizontalAlignment HorizontalAlignment.Left
                        TextBox.width 65.
                        TextBox.text state.Project.Version
                        TextBox.onTextChanged (fun e ->
                            fun p -> { p with Version = e }
                            |> EditProject
                            |> dispatch)
                    ]

                    TitledTextBox.create (state.Localization.GetString "artistName")
                        [ Grid.column 0
                          Grid.row 1
                          StackPanel.isVisible (not state.ShowSortFields && not state.ShowJapaneseFields) ]
                        [ TextBox.text state.Project.ArtistName.Value
                          TextBox.onTextChanged (fun e ->
                            fun p -> { p with ArtistName = { p.ArtistName with Value = StringValidator.field e } }
                            |> EditProject
                            |> dispatch)
                    ]

                    TitledTextBox.create (state.Localization.GetString "artistNameSort")
                        [ Grid.column 0
                          Grid.row 1
                          StackPanel.isVisible (state.ShowSortFields && not state.ShowJapaneseFields) ]
                        [ TextBox.text state.Project.ArtistName.SortValue
                          TextBox.onLostFocus (fun e -> 
                            let txtBox = e.Source :?> TextBox
                            let validValue = StringValidator.sortField txtBox.Text
                            txtBox.Text <- validValue

                            fun p -> { p with ArtistName = { p.ArtistName with SortValue = validValue } }
                            |> EditProject
                            |> dispatch)
                    ]

                    TitledTextBox.create (state.Localization.GetString "japaneseArtistName")
                        [ Grid.column 0
                          Grid.row 1
                          StackPanel.isVisible state.ShowJapaneseFields ]
                        [ TextBox.text (defaultArg state.Project.JapaneseArtistName String.Empty)
                          TextBox.fontFamily Fonts.japanese
                          TextBox.fontSize 15.
                          TextBox.onTextChanged (fun e -> 
                            fun p -> { p with JapaneseArtistName = Option.ofString (StringValidator.field e) }
                            |> EditProject
                            |> dispatch)
                    ]

                    TitledTextBox.create (state.Localization.GetString "title")
                        [ Grid.column 0
                          Grid.row 2
                          StackPanel.isVisible (not state.ShowSortFields && not state.ShowJapaneseFields) ]
                        [ TextBox.text state.Project.Title.Value
                          TextBox.onTextChanged (fun e ->
                            fun p -> { p with Title = { p.Title with Value = StringValidator.field e } }
                            |> EditProject
                            |> dispatch)
                    ]

                    TitledTextBox.create (state.Localization.GetString "titleSort")
                        [ Grid.column 0
                          Grid.row 2
                          StackPanel.isVisible state.ShowSortFields ]
                        [ TextBox.text state.Project.Title.SortValue
                          TextBox.onLostFocus (fun e -> 
                            let txtBox = e.Source :?> TextBox
                            let validValue = StringValidator.sortField txtBox.Text
                            txtBox.Text <- validValue

                            fun p -> { p with Title = { p.Title with SortValue = validValue } }
                            |> EditProject
                            |> dispatch)
                    ]

                    TitledTextBox.create (state.Localization.GetString "japaneseTitle")
                        [ Grid.column 0
                          Grid.row 2
                          StackPanel.isVisible state.ShowJapaneseFields ]
                        [ TextBox.text (defaultArg state.Project.JapaneseTitle String.Empty)
                          TextBox.fontFamily Fonts.japanese
                          TextBox.fontSize 15.
                          TextBox.onTextChanged (fun e ->
                            fun p -> { p with JapaneseTitle = Option.ofString (StringValidator.field e) }
                            |> EditProject
                            |> dispatch)
                    ]

                    TitledTextBox.create (state.Localization.GetString "albumName")
                        [ Grid.column 0
                          Grid.row 3
                          StackPanel.isVisible (not state.ShowSortFields) ]
                        [ TextBox.text state.Project.AlbumName.Value
                          TextBox.onTextChanged (fun e ->
                            fun p -> { p with AlbumName = { p.AlbumName with Value = StringValidator.field e } }
                            |> EditProject
                            |> dispatch)
                    ]

                    TitledTextBox.create (state.Localization.GetString "albumNameSort")
                        [ Grid.column 0
                          Grid.row 3
                          StackPanel.isVisible state.ShowSortFields ]
                        [ TextBox.text state.Project.AlbumName.SortValue
                          TextBox.onLostFocus (fun e -> 
                            let txtBox = e.Source :?> TextBox
                            let validValue = StringValidator.sortField txtBox.Text
                            txtBox.Text <- validValue

                            fun p -> { p with AlbumName = { p.AlbumName with SortValue = validValue } }
                            |> EditProject
                            |> dispatch)
                    ]

                    TitledTextBox.create (state.Localization.GetString "year")
                        [ Grid.column 1
                          Grid.row 3 ]
                        [ TextBox.horizontalAlignment HorizontalAlignment.Left
                          TextBox.width 65.
                          TextBox.text (string state.Project.Year)
                          TextBox.onTextChanged (fun e ->
                            fun p -> { p with Year = int e }
                            |> EditProject
                            |> dispatch)
                    ]

                    StackPanel.create [
                        Grid.columnSpan 2
                        Grid.row 4
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.horizontalAlignment HorizontalAlignment.Center
                        StackPanel.children [
                            CheckBox.create [
                                CheckBox.content (state.Localization.GetString "showSortFields")
                                CheckBox.isChecked (state.ShowSortFields && not state.ShowJapaneseFields)
                                CheckBox.onChecked (fun _ -> dispatch (ShowSortFields true))
                                CheckBox.onUnchecked (fun _ -> dispatch (ShowSortFields false))
                            ]
                            CheckBox.create [
                                CheckBox.margin (8., 0.,0., 0.)
                                CheckBox.content (state.Localization.GetString "showJapaneseFields")
                                CheckBox.isChecked (state.ShowJapaneseFields && not state.ShowSortFields)
                                CheckBox.onChecked (fun _ -> dispatch (ShowJapaneseFields true))
                                CheckBox.onUnchecked (fun _ -> dispatch (ShowJapaneseFields false))
                            ]
                        ]
                    ]

                    DockPanel.create [
                        Grid.column 0
                        Grid.row 5
                        DockPanel.children [
                            Button.create [
                                DockPanel.dock Dock.Right
                                Button.margin (0.0, 4.0, 4.0, 4.0)
                                Button.padding (10.0, 0.0)
                                Button.content "..."
                                Button.isEnabled notCalculatingVolume
                                Button.onClick (fun _ -> dispatch (Msg.OpenFileDialog("selectAudioFile", Dialogs.audioFileFilters, SetAudioFile)))
                                ToolTip.tip (state.Localization.GetString "selectAudioFile")
                            ]

                            Button.create [
                                DockPanel.dock Dock.Right
                                Button.content "W"
                                Button.margin (0.0, 4.0, 4.0, 4.0)
                                Button.isEnabled noBuildInProgress
                                Button.isVisible (String.notEmpty audioPath && not <| String.endsWith ".wem" audioPath)
                                Button.onClick (fun _ -> dispatch ConvertToWem)
                                ToolTip.tip (state.Localization.GetString "convertMultipleToWemTooltip")
                            ]

                            Button.create [
                                DockPanel.dock Dock.Right
                                Button.content "vol"
                                Button.margin (0.0, 4.0, 4.0, 4.0)
                                Button.isEnabled (noBuildInProgress && notCalculatingVolume)
                                Button.isVisible (String.notEmpty audioPath && not <| String.endsWith ".wem" audioPath)
                                Button.onClick ((fun _ ->
                                    dispatch (CalculateVolume MainAudio)
                                    let previewPath = state.Project.AudioPreviewFile.Path
                                    if IO.File.Exists previewPath && not <| String.endsWith "wem" previewPath then
                                        dispatch (CalculateVolume PreviewAudio)
                                    ), SubPatchOptions.OnChangeOf state.Project.AudioPreviewFile)
                            ]

                            TextBlock.create [
                                TextBlock.margin (4.0, 4.0, 0.0, 4.0)
                                TextBlock.verticalAlignment VerticalAlignment.Center
                                TextBlock.text (state.Localization.GetString "audioFile")
                            ]

                            TextBlock.create [
                                TextBlock.margin (4.0, 4.0, 0.0, 4.0)
                                TextBlock.verticalAlignment VerticalAlignment.Center
                                TextBlock.text (
                                    if String.notEmpty audioPath then
                                        IO.Path.GetFileName audioPath
                                    else
                                        state.Localization.GetString "noAudioFile"
                                )
                            ]
                        ]
                    ]

                    NumericUpDown.create [
                        Grid.column 1
                        Grid.row 5
                        NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                        NumericUpDown.width 65.
                        NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                        NumericUpDown.minimum -45.
                        NumericUpDown.maximum 45.
                        NumericUpDown.increment 0.5
                        NumericUpDown.value state.Project.AudioFile.Volume
                        NumericUpDown.formatString "F1"
                        NumericUpDown.isEnabled (not <| state.RunningTasks.Contains (VolumeCalculation MainAudio))
                        NumericUpDown.onValueChanged (fun v ->
                            fun p -> { p with AudioFile = { p.AudioFile with Volume = v } }
                            |> EditProject
                            |> dispatch)
                        ToolTip.tip (state.Localization.GetString "audioVolumeToolTip")
                    ]

                    DockPanel.create [
                        Grid.column 0
                        Grid.row 6
                        DockPanel.children [
                            let previewExists = IO.File.Exists state.Project.AudioPreviewFile.Path

                            Button.create [
                                DockPanel.dock Dock.Right
                                Button.margin (0.0, 4.0, 4.0, 4.0)
                                Button.horizontalAlignment HorizontalAlignment.Center
                                Button.content (state.Localization.GetString "createPreviewAudio")
                                Button.isEnabled (not <| String.endsWith ".wem" audioPath)
                                Button.onClick (fun _ -> dispatch (CreatePreviewAudio SetupStartTime))
                                ToolTip.tip (
                                    if previewExists then
                                        state.Localization.GetString "previewAudioExistsToolTip"
                                    else
                                        state.Localization.GetString "previewAudioDoesNotExistToolTip"
                                )
                            ]

                            TextBlock.create [
                                TextBlock.margin (4.0, 4.0, 0.0, 4.0)
                                TextBlock.verticalAlignment VerticalAlignment.Center
                                TextBlock.text (state.Localization.GetString "preview")
                            ]

                            Path.create [
                                Path.fill Brushes.Gray
                                Path.data (if previewExists then Icons.check else Icons.x)
                                Path.verticalAlignment VerticalAlignment.Center
                                Path.margin (0., 0., 4., 0.)
                                if IO.File.Exists state.Project.AudioPreviewFile.Path then
                                    ToolTip.tip (IO.Path.GetFileName state.Project.AudioPreviewFile.Path)
                            ]
                        ]
                    ]        

                    NumericUpDown.create [
                        Grid.column 1
                        Grid.row 6
                        NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                        NumericUpDown.width 65.
                        NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                        NumericUpDown.minimum -45.
                        NumericUpDown.maximum 45.
                        NumericUpDown.increment 0.5
                        NumericUpDown.value state.Project.AudioPreviewFile.Volume
                        NumericUpDown.formatString "F1"
                        NumericUpDown.isEnabled (not <| state.RunningTasks.Contains (VolumeCalculation PreviewAudio))
                        NumericUpDown.onValueChanged (fun v ->
                            fun p -> { p with AudioPreviewFile = { p.AudioPreviewFile with Volume = v } }
                            |> EditProject
                            |> dispatch)
                        ToolTip.tip (state.Localization.GetString "previewAudioVolumeToolTip")
                    ]

                    Grid.create [
                        Grid.columnSpan 2
                        Grid.row 7
                        Grid.verticalAlignment VerticalAlignment.Center
                        Grid.horizontalAlignment HorizontalAlignment.Center
                        Grid.columnDefinitions "*,*"
                        Grid.rowDefinitions "*,*,*"
                        Grid.children [
                            Button.create [
                                Grid.columnSpan 2
                                Button.padding (15., 8.)
                                Button.margin 4.
                                Button.fontSize 16.
                                Button.content (state.Localization.GetString "configuration")
                                Button.onClick (fun _ -> ShowConfigEditor |> dispatch)
                            ]

                            StackPanel.create [
                                Grid.row 1
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.children [
                                    Button.create [
                                        Button.padding (15., 8.)
                                        Button.margin (4., 4., 0., 4.)
                                        Button.fontSize 16.
                                        Button.content (state.Localization.GetString "openProject")
                                        Button.onClick (fun _ -> dispatch (Msg.OpenFileDialog("selectProjectFile", Dialogs.projectFilter, OpenProject)))
                                        Button.isEnabled (not <| state.RunningTasks.Contains PsarcImport)
                                    ]
                                    Menu.create [
                                        Menu.fontSize 16.
                                        Menu.background "#505050"
                                        Menu.margin (0., 4., 4., 4.)
                                        Menu.viewItems [
                                            MenuItem.create [
                                                MenuItem.isEnabled (not <| state.RunningTasks.Contains PsarcImport)
                                                MenuItem.header (TextBlock.create [
                                                    TextBlock.text "..."
                                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                                ])
                                                MenuItem.viewItems [
                                                    MenuItem.create [
                                                        MenuItem.header (state.Localization.GetString "newProject")
                                                        MenuItem.onClick (fun _ -> dispatch NewProject)
                                                    ]
                                                    MenuItem.create [
                                                        MenuItem.header "-"
                                                    ]
                                                    MenuItem.create [
                                                        MenuItem.header (state.Localization.GetString "toolkitImport")
                                                        MenuItem.onClick (fun _ ->
                                                            Msg.OpenFileDialog("selectImportToolkitTemplate", Dialogs.toolkitFilter, ImportToolkitTemplate)
                                                            |> dispatch)
                                                    ]
                                                    MenuItem.create [
                                                        MenuItem.header (state.Localization.GetString "psarcImport")
                                                        MenuItem.onClick (fun _ ->
                                                            Msg.OpenFileDialog("selectImportPsarc", Dialogs.psarcFilter, SelectImportPsarcFolder)
                                                            |>dispatch)
                                                    ]
                                                    if state.RecentFiles.Length > 0 then
                                                        MenuItem.create [
                                                            MenuItem.header "-"
                                                        ]
                                                        yield! state.RecentFiles |> List.map (fun fileName ->
                                                            MenuItem.create [
                                                                MenuItem.header ((IO.Path.GetFileName fileName).Replace("_", "__"))
                                                                MenuItem.onClick (
                                                                    (fun _ -> OpenProject fileName |>dispatch),
                                                                    SubPatchOptions.OnChangeOf state.RecentFiles)
                                                            ] |> Helpers.generalize
                                                        )
                                                ]
                                                
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                            StackPanel.create [
                                Grid.column 1
                                Grid.row 1
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.children [
                                    Button.create [
                                        Button.padding (15., 8.)
                                        Button.margin (4., 4., 0., 4.)
                                        Button.fontSize 16.
                                        Button.content (state.Localization.GetString "saveProject")
                                        Button.onClick (fun _ -> dispatch ProjectSaveOrSaveAs)
                                        Button.isEnabled (state.Project <> state.SavedProject)
                                    ]
                                    Button.create [
                                        Button.padding (8., 8.)
                                        Button.margin (0., 4., 4., 4.)
                                        Button.fontSize 16.
                                        Button.content "..."
                                        Button.onClick (fun _ -> dispatch ProjectSaveAs)
                                        ToolTip.tip (state.Localization.GetString "saveProjectAs")
                                    ]
                                ]
                            ]
                            Button.create [
                                Grid.row 2
                                Button.padding (15., 8.)
                                Button.margin 4.
                                Button.fontSize 16.
                                Button.content (state.Localization.GetString "buildTest")
                                Button.isEnabled (canBuild && String.notEmpty state.Config.TestFolderPath)
                                Button.onClick (fun _ -> dispatch BuildTest)
                            ]
                            Button.create [
                                Grid.column 1
                                Grid.row 2
                                Button.padding (15., 8.)
                                Button.margin 4.
                                Button.fontSize 16.
                                Button.content (state.Localization.GetString "buildRelease")
                                Button.isEnabled canBuild
                                Button.onClick (fun _ -> dispatch BuildRelease)
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
