module DLCBuilder.ProjectDetails

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.DLCProject
open System
open Media

let view state dispatch =
    let canBuild =
        not state.BuildInProgress
        && state.Project.Arrangements.Length > 0
        && String.notEmpty state.Project.DLCKey
        && String.notEmpty state.Project.AudioFile.Path

    DockPanel.create [
        Grid.rowSpan 2
        DockPanel.margin (8.0, 8.0, 0.0, 8.0)
        DockPanel.children [
            Image.create [
                DockPanel.dock Dock.Top
                Image.source (Option.toObj state.CoverArt)
                Image.width 200.
                Image.height 200.
                Image.onTapped (fun _ -> dispatch SelectCoverArt)
                Image.cursor (Cursor(StandardCursorType.Hand))
                ToolTip.tip (state.Localization.GetString "selectCoverArtToolTip")
            ]

            Grid.create [
                Grid.columnDefinitions "3*,*"
                Grid.rowDefinitions "35,35,35,35,35,40,40,*"
                //Grid.showGridLines true
                Grid.children [
                    TextBox.create [
                        Grid.column 0
                        Grid.row 0
                        TextBox.watermark (state.Localization.GetString "dlcKey")
                        TextBox.text state.Project.DLCKey
                        ToolTip.tip (state.Localization.GetString "dlcKey")
                        // Cannot filter pasted text: https://github.com/AvaloniaUI/Avalonia/issues/2611
                        TextBox.onTextInput (fun e -> e.Text <- StringValidator.dlcKey e.Text)
                        TextBox.onTextChanged (fun e -> (fun p -> { p with DLCKey = StringValidator.dlcKey e }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 1
                        Grid.row 0
                        TextBox.horizontalAlignment HorizontalAlignment.Left
                        TextBox.width 65.
                        TextBox.watermark (state.Localization.GetString "version")
                        TextBox.text state.Project.Version
                        ToolTip.tip (state.Localization.GetString "version")
                        TextBox.onTextChanged (fun e -> (fun p -> { p with Version = e }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 1
                        TextBox.watermark (state.Localization.GetString "artistName")
                        TextBox.text state.Project.ArtistName.Value
                        ToolTip.tip (state.Localization.GetString "artistName")
                        TextBox.onTextChanged (fun e -> (fun p -> { p with ArtistName = { p.ArtistName with Value = StringValidator.field e } }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 1
                        TextBox.watermark (state.Localization.GetString "artistNameSort")
                        TextBox.text state.Project.ArtistName.SortValue
                        TextBox.isVisible state.ShowSortFields
                        ToolTip.tip (state.Localization.GetString "artistNameSort")
                        TextBox.onLostFocus (fun e -> 
                            let txtBox = e.Source :?> TextBox
                            let validValue = StringValidator.sortField txtBox.Text
                            txtBox.Text <- validValue
                            (fun p -> { p with ArtistName = { p.ArtistName with SortValue = validValue } }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 1
                        TextBox.watermark (state.Localization.GetString "japaneseArtistName")
                        TextBox.text (defaultArg state.Project.JapaneseArtistName String.Empty)
                        TextBox.isVisible state.ShowJapaneseFields
                        ToolTip.tip (state.Localization.GetString "japaneseArtistName")
                        TextBox.onTextChanged (fun e -> (fun p -> { p with JapaneseArtistName = Option.ofString (StringValidator.field e) }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 2
                        TextBox.watermark (state.Localization.GetString "title")
                        TextBox.text state.Project.Title.Value
                        ToolTip.tip (state.Localization.GetString "title")
                        TextBox.onTextChanged (fun e -> (fun p -> { p with Title = { p.Title with Value = StringValidator.field e } }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 2
                        TextBox.watermark (state.Localization.GetString "titleSort")
                        TextBox.text state.Project.Title.SortValue
                        TextBox.isVisible state.ShowSortFields
                        ToolTip.tip (state.Localization.GetString "titleSort")
                        TextBox.onLostFocus (fun e -> 
                            let txtBox = e.Source :?> TextBox
                            let validValue = StringValidator.sortField txtBox.Text
                            txtBox.Text <- validValue
                            (fun p -> { p with Title = { p.Title with SortValue = validValue } }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 2
                        TextBox.watermark (state.Localization.GetString "japaneseTitle")
                        TextBox.text (defaultArg state.Project.JapaneseTitle String.Empty)
                        TextBox.isVisible state.ShowJapaneseFields
                        ToolTip.tip (state.Localization.GetString "japaneseTitle")
                        TextBox.onTextChanged (fun e -> (fun p -> { p with JapaneseTitle = Option.ofString (StringValidator.field e) }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 3
                        TextBox.watermark (state.Localization.GetString "albumName")
                        TextBox.text state.Project.AlbumName.Value
                        ToolTip.tip (state.Localization.GetString "albumName")
                        TextBox.onTextChanged (fun e -> (fun p -> { p with AlbumName = { p.AlbumName with Value = StringValidator.field e } }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 3
                        TextBox.watermark (state.Localization.GetString "albumNameSort")
                        TextBox.text state.Project.AlbumName.SortValue
                        TextBox.isVisible state.ShowSortFields
                        ToolTip.tip (state.Localization.GetString "albumNameSort")
                        TextBox.onLostFocus (fun e -> 
                            let txtBox = e.Source :?> TextBox
                            let validValue = StringValidator.sortField txtBox.Text
                            txtBox.Text <- validValue
                            (fun p -> { p with AlbumName = { p.AlbumName with SortValue = validValue } }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 1
                        Grid.row 3
                        TextBox.horizontalAlignment HorizontalAlignment.Left
                        TextBox.width 65.
                        TextBox.watermark (state.Localization.GetString "year")
                        TextBox.text (string state.Project.Year)
                        ToolTip.tip (state.Localization.GetString "year")
                        TextBox.onTextChanged (fun e -> (fun p -> { p with Year = int e }) |> EditProject |> dispatch)
                    ]

                    StackPanel.create [
                        Grid.columnSpan 2
                        Grid.row 4
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.horizontalAlignment HorizontalAlignment.Center
                        StackPanel.children [
                            CheckBox.create [
                                CheckBox.content (state.Localization.GetString "showSortFields")
                                CheckBox.isChecked state.ShowSortFields
                                CheckBox.onChecked (fun _ -> dispatch (ShowSortFields true))
                                CheckBox.onUnchecked (fun _ -> dispatch (ShowSortFields false))
                            ]
                            CheckBox.create [
                                CheckBox.margin (8., 0.,0., 0.)
                                CheckBox.content (state.Localization.GetString "showJapaneseFields")
                                CheckBox.isChecked state.ShowJapaneseFields
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
                                Button.content "wem"
                                Button.margin (0.0, 4.0, 4.0, 4.0)
                                Button.isEnabled (not state.BuildInProgress)
                                Button.onClick (fun _ -> dispatch ConvertToWem)
                            ]
                            Button.create [
                                DockPanel.dock Dock.Right
                                Button.margin (0.0, 4.0, 4.0, 4.0)
                                Button.padding (10.0, 0.0)
                                Button.content "..."
                                Button.onClick (fun _ -> dispatch SelectAudioFile)
                                ToolTip.tip (state.Localization.GetString "selectAudioFile")
                            ]
                            TextBox.create [
                                TextBox.margin (4.0, 4.0, 0.0, 4.0)
                                TextBox.watermark (state.Localization.GetString "audioFile")
                                TextBox.text (IO.Path.GetFileName state.Project.AudioFile.Path)
                                ToolTip.tip (state.Localization.GetString "audioFile")
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
                        NumericUpDown.value state.Project.AudioFile.Volume
                        NumericUpDown.formatString "F1"
                        NumericUpDown.onValueChanged (fun v -> (fun p -> { p with AudioFile = { p.AudioFile with Volume = v } }) |> EditProject |> dispatch)
                        ToolTip.tip (state.Localization.GetString "audioVolumeToolTip")
                    ]
        
                    Button.create [
                        let previewExists = IO.File.Exists state.Project.AudioPreviewFile.Path

                        Grid.column 0
                        Grid.row 6
                        Button.horizontalAlignment HorizontalAlignment.Center
                        Button.content (
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.children [
                                    Path.create [
                                        Path.fill Brushes.Gray
                                        Path.data (if previewExists then Icons.check else Icons.x)
                                        Path.horizontalAlignment HorizontalAlignment.Center
                                        Path.margin (0., 0., 4., 0.)
                                    ]
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text (state.Localization.GetString "createPreviewAudio")
                                    ]
                                ]
                            ]
                        )
                        Button.isEnabled (state.Project.AudioFile.Path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                        Button.onClick (fun _ -> dispatch (CreatePreviewAudio SetupStartTime))
                        ToolTip.tip (if previewExists then state.Localization.GetString "previewAudioExistsToolTip" else state.Localization.GetString "previewAudioDoesNotExistToolTip")
                    ]

                    NumericUpDown.create [
                        Grid.column 1
                        Grid.row 6
                        NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                        NumericUpDown.width 65.
                        NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                        NumericUpDown.minimum -45.
                        NumericUpDown.maximum 45.
                        NumericUpDown.value state.Project.AudioPreviewFile.Volume
                        NumericUpDown.formatString "F1"
                        NumericUpDown.onValueChanged (fun v -> (fun p -> { p with AudioPreviewFile = { p.AudioPreviewFile with Volume = v } }) |> EditProject |> dispatch)
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

                            Button.create [
                                Grid.row 1
                                Button.padding (15., 8.)
                                Button.margin 4.
                                Button.fontSize 16.
                                Button.content (state.Localization.GetString "openProject")
                                Button.onClick (fun _ -> SelectOpenProjectFile |> dispatch)
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
                                        Button.onClick (fun _ -> ProjectSaveOrSaveAs |> dispatch)
                                    ]
                                    Button.create [
                                        Button.padding (8., 8.)
                                        Button.margin (0., 4., 4., 4.)
                                        Button.fontSize 16.
                                        Button.content "..."
                                        Button.onClick (fun _ -> ProjectSaveAs |> dispatch)
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
                                Button.isEnabled (canBuild && (not (String.IsNullOrWhiteSpace state.Config.TestFolderPath)))
                                Button.onClick (fun _ -> BuildTest |> dispatch)
                            ]
                            Button.create [
                                Grid.column 1
                                Grid.row 2
                                Button.padding (15., 8.)
                                Button.margin 4.
                                Button.fontSize 16.
                                Button.content (state.Localization.GetString "buildRelease")
                                Button.isEnabled canBuild
                                Button.onClick (fun _ -> BuildRelease |> dispatch)
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
