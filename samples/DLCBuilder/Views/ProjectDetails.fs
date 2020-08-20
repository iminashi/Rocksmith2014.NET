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
                ToolTip.tip "Click to select a cover art file.\nThe image should be a square, preferably 512x512."
            ]

            Grid.create [
                Grid.columnDefinitions "3*,*"
                Grid.rowDefinitions "35,35,35,35,35,40,40,*"
                //Grid.showGridLines true
                Grid.children [
                    TextBox.create [
                        Grid.column 0
                        Grid.row 0
                        TextBox.watermark "DLC Key"
                        TextBox.text state.Project.DLCKey
                        ToolTip.tip "DLC Key"
                        // Cannot filter pasted text: https://github.com/AvaloniaUI/Avalonia/issues/2611
                        TextBox.onTextInput (fun e -> e.Text <- StringValidator.dlcKey e.Text)
                        TextBox.onTextChanged (fun e -> (fun p -> { p with DLCKey = StringValidator.dlcKey e }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 1
                        Grid.row 0
                        TextBox.horizontalAlignment HorizontalAlignment.Left
                        TextBox.width 65.
                        TextBox.watermark "Version"
                        TextBox.text state.Project.Version
                        ToolTip.tip "Version"
                        TextBox.onTextChanged (fun e -> (fun p -> { p with Version = e }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 1
                        TextBox.watermark "Artist Name"
                        TextBox.text state.Project.ArtistName.Value
                        ToolTip.tip "Artist Name"
                        TextBox.onTextChanged (fun e -> (fun p -> { p with ArtistName = { p.ArtistName with Value = StringValidator.field e } }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 1
                        TextBox.watermark "Artist Name Sort"
                        TextBox.text state.Project.ArtistName.SortValue
                        TextBox.isVisible state.ShowSortFields
                        ToolTip.tip "Artist Name Sort"
                        TextBox.onLostFocus (fun e -> 
                            let txtBox = e.Source :?> TextBox
                            let validValue = StringValidator.sortField txtBox.Text
                            txtBox.Text <- validValue
                            (fun p -> { p with ArtistName = { p.ArtistName with SortValue = validValue } }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 1
                        TextBox.watermark "Japanese Artist Name"
                        TextBox.text (defaultArg state.Project.JapaneseArtistName String.Empty)
                        TextBox.isVisible (state.ShowJapaneseFields)
                        ToolTip.tip "Japanese Artist Name"
                        TextBox.onTextChanged (fun e -> (fun p -> { p with JapaneseArtistName = Option.ofString (StringValidator.field e) }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 2
                        TextBox.watermark "Title"
                        TextBox.text state.Project.Title.Value
                        ToolTip.tip "Title"
                        TextBox.onTextChanged (fun e -> (fun p -> { p with Title = { p.Title with Value = StringValidator.field e } }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 2
                        TextBox.watermark "Title Sort"
                        TextBox.text state.Project.Title.SortValue
                        TextBox.isVisible state.ShowSortFields
                        ToolTip.tip "Title Sort"
                        TextBox.onLostFocus (fun e -> 
                            let txtBox = e.Source :?> TextBox
                            let validValue = StringValidator.sortField txtBox.Text
                            txtBox.Text <- validValue
                            (fun p -> { p with Title = { p.Title with SortValue = validValue } }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 2
                        TextBox.watermark "Japanese Title"
                        TextBox.text (defaultArg state.Project.JapaneseTitle String.Empty)
                        TextBox.isVisible (state.ShowJapaneseFields)
                        ToolTip.tip "Japanese Title"
                        TextBox.onTextChanged (fun e -> (fun p -> { p with JapaneseTitle = Option.ofString (StringValidator.field e) }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 3
                        TextBox.watermark "Album Name"
                        TextBox.text state.Project.AlbumName.Value
                        ToolTip.tip "Album Name"
                        TextBox.onTextChanged (fun e -> (fun p -> { p with AlbumName = { p.AlbumName with Value = StringValidator.field e } }) |> EditProject |> dispatch)
                    ]

                    TextBox.create [
                        Grid.column 0
                        Grid.row 3
                        TextBox.watermark "Album Name Sort"
                        TextBox.text state.Project.AlbumName.SortValue
                        TextBox.isVisible state.ShowSortFields
                        ToolTip.tip "Album Name Sort"
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
                        TextBox.watermark "Year"
                        TextBox.text (string state.Project.Year)
                        ToolTip.tip "Year"
                        TextBox.onTextChanged (fun e -> (fun p -> { p with Year = int e }) |> EditProject |> dispatch)
                    ]

                    StackPanel.create [
                        Grid.columnSpan 2
                        Grid.row 4
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.horizontalAlignment HorizontalAlignment.Center
                        StackPanel.children [
                            CheckBox.create [
                                CheckBox.content "Show Sort Fields"
                                CheckBox.isChecked (state.ShowSortFields)
                                CheckBox.onChecked (fun _ -> dispatch (ShowSortFields true))
                                CheckBox.onUnchecked (fun _ -> dispatch (ShowSortFields false))
                            ]
                            CheckBox.create [
                                  CheckBox.content "Show Japanese Fields"
                                  CheckBox.isChecked (state.ShowJapaneseFields)
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
                                Button.onClick (fun _ -> dispatch SelectAudioFile)
                                ToolTip.tip "Select Audio File"
                            ]
                            TextBox.create [
                                TextBox.margin (4.0, 4.0, 0.0, 4.0)
                                TextBox.watermark "Audio File"
                                TextBox.text (IO.Path.GetFileName state.Project.AudioFile.Path)
                                ToolTip.tip "Audio File"
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
                        ToolTip.tip "Audio Volume (dB)"
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
                                    ]
                                    TextBlock.create [
                                        TextBlock.verticalAlignment VerticalAlignment.Center
                                        TextBlock.text " Create Preview Audio"
                                    ]
                                ]
                            ]
                        )
                        Button.isEnabled (state.Project.AudioFile.Path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                        Button.onClick (fun _ -> dispatch (CreatePreviewAudio SetupStartTime))
                        ToolTip.tip (if previewExists then "Preview audio file exists, click to create a new one." else "Preview audio file does not exist, click to create one.")
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
                        ToolTip.tip "Preview Audio Volume (dB)"
                    ]

                    StackPanel.create [
                        StackPanel.verticalAlignment VerticalAlignment.Center
                        StackPanel.horizontalAlignment HorizontalAlignment.Center
                        Grid.columnSpan 2
                        Grid.row 7
                        StackPanel.children [
                            Button.create [
                                Button.padding (15., 8.)
                                Button.margin 4.
                                Button.fontSize 16.
                                Button.content "Configuration"
                                Button.onClick (fun _ -> ShowConfigEditor |> dispatch)
                            ]
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.children [
                                    Button.create [
                                        Button.padding (15., 8.)
                                        Button.margin 4.
                                        Button.fontSize 16.
                                        Button.content "Open Project"
                                        Button.onClick (fun _ -> SelectOpenProjectFile |> dispatch)
                                    ]
                                    Button.create [
                                        Button.padding (15., 8.)
                                        Button.margin (4., 4., 0., 4.)
                                        Button.fontSize 16.
                                        Button.content "Save Project"
                                        Button.onClick ((fun _ ->
                                            match state.OpenProjectFile with
                                            | Some _ as fn -> SaveProject fn |> dispatch
                                            | None -> SelectSaveProjectTarget |> dispatch),
                                            SubPatchOptions.OnChangeOf state.OpenProjectFile)
                                    ]
                                    Button.create [
                                        Button.padding (8., 8.)
                                        Button.margin (0., 4., 4., 4.)
                                        Button.fontSize 16.
                                        Button.content "..."
                                        Button.onClick (fun _ -> SelectSaveProjectTarget |> dispatch)
                                    ]
                                ]
                            ]
                            StackPanel.create [
                                StackPanel.orientation Orientation.Horizontal
                                StackPanel.children [
                                    Button.create [
                                        Button.padding (15., 8.)
                                        Button.margin 4.
                                        Button.fontSize 16.
                                        Button.content "Build Test"
                                    ]
                                    Button.create [
                                        Button.padding (15., 8.)
                                        Button.margin 4.
                                        Button.fontSize 16.
                                        Button.content "Build Release"
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
