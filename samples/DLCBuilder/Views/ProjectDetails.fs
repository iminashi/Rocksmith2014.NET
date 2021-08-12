module DLCBuilder.Views.ProjectDetails

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.Platform
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open DLCBuilder
open Media

let private placeholder =
    let assets = AvaloniaLocator.Current.GetService<IAssetLoader>()
    new Bitmap(assets.Open(Uri("avares://DLCBuilder/Assets/coverart_placeholder.png")))

let private audioControls state dispatch =
    let audioPath = state.Project.AudioFile.Path
    let previewPath = state.Project.AudioPreviewFile.Path
    let previewExists = IO.File.Exists previewPath
    let notCalculatingVolume =
        not (state.RunningTasks |> Set.exists (function VolumeCalculation (MainAudio | PreviewAudio) -> true | _ -> false))

    Border.create [
        DockPanel.dock Dock.Top
        Border.background "#181818"
        Border.borderThickness 1.
        Border.cornerRadius 6.
        Border.margin 2.
        Border.child (
            StackPanel.create [
                StackPanel.margin 4.
                StackPanel.children [
                    // Header
                    locText "Audio" [
                        TextBlock.margin (0., 4.)
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                    ]

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            // Main audio filename
                            locText "MainAudioFile" [
                                TextBlock.margin (4.0, 4.0, 0.0, 4.0)
                                TextBlock.verticalAlignment VerticalAlignment.Center
                            ]
                            TextBlock.create [
                                TextBlock.margin (4.0, 4.0, 0.0, 4.0)
                                TextBlock.verticalAlignment VerticalAlignment.Center
                                TextBlock.text (
                                    if String.notEmpty audioPath then
                                        IO.Path.GetFileName audioPath
                                    else
                                        translate "NoAudioFile"
                                )
                            ]
                        ]
                    ]

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            // Main volume
                            FixedNumericUpDown.create [
                                NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                                NumericUpDown.minimum -45.
                                NumericUpDown.maximum 45.
                                NumericUpDown.increment 0.5
                                NumericUpDown.formatString "+0.0;-0.0;0.0"
                                NumericUpDown.isEnabled (not <| state.RunningTasks.Contains (VolumeCalculation MainAudio))
                                FixedNumericUpDown.value state.Project.AudioFile.Volume
                                FixedNumericUpDown.onValueChanged (SetAudioVolume >> EditProject >> dispatch)
                                ToolTip.tip (translate "AudioVolumeToolTip")
                            ]

                            // Select audio file
                            Button.create [
                                Button.minWidth 75.
                                Button.margin (0.0, 4.0, 4.0, 4.0)
                                Button.padding (10.0, 0.0)
                                Button.content (translate "Select")
                                Button.isEnabled notCalculatingVolume
                                Button.onClick (fun _ -> Dialog.AudioFile false |> ShowDialog |> dispatch)
                                ToolTip.tip (translate "SelectAudioFile")
                            ]

                            Menus.audio notCalculatingVolume state dispatch
                        ]
                    ]

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            // Preview audio filename
                            locText "Preview" [
                                TextBlock.margin (4.0, 4.0, 0.0, 4.0)
                            ]
                            TextBlock.create [
                                TextBlock.margin (4.0, 4.0, 0.0, 4.0)
                                TextBlock.text (
                                    if String.notEmpty previewPath then
                                        IO.Path.GetFileName previewPath
                                    else
                                        translate "NoAudioFile"
                                )
                            ]
                        ]
                    ]

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            // Preview audio volume
                            FixedNumericUpDown.create [
                                NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                                NumericUpDown.minimum -45.
                                NumericUpDown.maximum 45.
                                NumericUpDown.increment 0.5
                                NumericUpDown.formatString "+0.0;-0.0;0.0"
                                NumericUpDown.isEnabled (not <| state.RunningTasks.Contains (VolumeCalculation PreviewAudio))
                                FixedNumericUpDown.value state.Project.AudioPreviewFile.Volume
                                FixedNumericUpDown.onValueChanged (SetPreviewVolume >> EditProject >> dispatch)
                                ToolTip.tip (translate "PreviewAudioVolumeToolTip")
                            ]

                            // Create preview audio
                            Button.create [
                                Button.minWidth 75.
                                Button.margin (0.0, 4.0, 4.0, 4.0)
                                Button.padding (10.0, 0.0)
                                Button.content (translate "Create")
                                Button.isEnabled (not <| String.endsWith ".wem" audioPath && IO.File.Exists audioPath)
                                Button.onClick (fun _ -> dispatch (CreatePreviewAudio SetupStartTime))
                                ToolTip.tip (
                                    if previewExists then
                                        translate "PreviewAudioExistsToolTip"
                                    else
                                        translate "PreviewAudioDoesNotExistToolTip"
                                )
                            ]
                        ]
                    ]
                ]
            ]
        )
    ]

let private buildControls state dispatch =
    let canBuild = StateUtils.canBuild state

    Grid.create [
        Grid.verticalAlignment VerticalAlignment.Center
        Grid.horizontalAlignment HorizontalAlignment.Center
        Grid.columnDefinitions "*,*"
        Grid.children [
            // Build test
            Button.create [
                Button.padding (20., 8.)
                Button.margin 4.
                Button.fontSize 16.
                Button.content (translate "BuildTest")
                Button.isEnabled canBuild
                Button.onClick (fun _ -> dispatch <| Build Test)
            ]

            // Build release
            Button.create [
                Grid.column 1
                Button.padding (20., 8.)
                Button.margin 4.
                Button.fontSize 16.
                Button.content (translate "BuildRelease")
                Button.isEnabled canBuild
                Button.onClick (fun _ -> dispatch <| Build Release)
            ]
        ]
    ]

let private projectInfo state dispatch =
    Grid.create [
        DockPanel.dock Dock.Top
        Grid.columnDefinitions "*,auto"
        Grid.rowDefinitions "auto,auto,auto,auto,auto"
        //Grid.showGridLines true
        Grid.children [
            // DLC Key
            TitledTextBox.create "DLCKey" [ Grid.column 0; Grid.row 0 ] [
                FixedTextBox.text state.Project.DLCKey
                TextBox.onTextInput (fun e -> e.Text <- StringValidator.dlcKey e.Text)
                FixedTextBox.onTextChanged (StringValidator.dlcKey >> SetDLCKey >> EditProject >> dispatch)
                ToolTip.tip (translate "DLCKeyToolTip")
            ]

            // Version
            TitledTextBox.create "Version" [ Grid.column 1; Grid.row 0 ] [
                TextBox.horizontalAlignment HorizontalAlignment.Left
                TextBox.width 65.
                FixedTextBox.text state.Project.Version
                FixedTextBox.onTextChanged (SetVersion >> EditProject >> dispatch)
            ]

            // Artist name
            TitledTextBox.create "ArtistName"
                [ Grid.column 0
                  Grid.row 1
                  StackPanel.isVisible (not state.ShowSortFields && not state.ShowJapaneseFields) ]
                [ FixedTextBox.text state.Project.ArtistName.Value
                  FixedTextBox.onTextChanged (StringValidator.field >> SetArtistName >> EditProject >> dispatch)
                ]

            // Artist name sort
            TitledTextBox.create "ArtistNameSort"
                [ Grid.column 0
                  Grid.row 1
                  StackPanel.isVisible (state.ShowSortFields && not state.ShowJapaneseFields) ]
                [ FixedTextBox.text state.Project.ArtistName.SortValue
                  TextBox.onLostFocus (fun e ->
                    let txtBox = e.Source :?> TextBox
                    let validValue = StringValidator.sortField txtBox.Text
                    txtBox.Text <- validValue

                    validValue |> (SetArtistNameSort >> EditProject >> dispatch))
                ]

            // Japanese artist name
            TitledTextBox.create "JapaneseArtistName"
                [ Grid.column 0
                  Grid.row 1
                  StackPanel.isVisible state.ShowJapaneseFields ]
                [ FixedTextBox.text (defaultArg state.Project.JapaneseArtistName String.Empty)
                  TextBox.fontFamily Fonts.japanese
                  TextBox.fontSize 15.
                  FixedTextBox.onTextChanged (StringValidator.field >> Option.ofString >> SetJapaneseArtistName >> EditProject >> dispatch)
                ]

            // Title
            TitledTextBox.create "Title"
                [ Grid.column 0
                  Grid.row 2
                  StackPanel.isVisible (not state.ShowSortFields && not state.ShowJapaneseFields) ]
                [ FixedTextBox.text state.Project.Title.Value
                  FixedTextBox.onTextChanged (StringValidator.field >> SetTitle >> EditProject >> dispatch)
                ]

            // Title sort
            TitledTextBox.create "TitleSort"
                [ Grid.column 0
                  Grid.row 2
                  StackPanel.isVisible state.ShowSortFields ]
                [ FixedTextBox.text state.Project.Title.SortValue
                  TextBox.onLostFocus (fun e ->
                    let txtBox = e.Source :?> TextBox
                    let validValue = StringValidator.sortField txtBox.Text
                    txtBox.Text <- validValue

                    validValue |> (SetTitleSort >> EditProject >> dispatch))
                ]

            // Japanese title
            TitledTextBox.create "JapaneseTitle"
                [ Grid.column 0
                  Grid.row 2
                  StackPanel.isVisible state.ShowJapaneseFields ]
                [ FixedTextBox.text (defaultArg state.Project.JapaneseTitle String.Empty)
                  TextBox.fontFamily Fonts.japanese
                  TextBox.fontSize 15.
                  FixedTextBox.onTextChanged (StringValidator.field >> Option.ofString >> SetJapaneseTitle >> EditProject >> dispatch)
                ]

            // Album name
            TitledTextBox.create "AlbumName"
                [ Grid.column 0
                  Grid.row 3
                  StackPanel.isVisible (not state.ShowSortFields) ]
                [ FixedTextBox.text state.Project.AlbumName.Value
                  FixedTextBox.onTextChanged (StringValidator.field >> SetAlbumName >> EditProject >> dispatch)
                ]

            // Album name sort
            TitledTextBox.create "AlbumNameSort"
                [ Grid.column 0
                  Grid.row 3
                  StackPanel.isVisible state.ShowSortFields ]
                [ FixedTextBox.text state.Project.AlbumName.SortValue
                  TextBox.onLostFocus (fun e ->
                    let txtBox = e.Source :?> TextBox
                    let validValue = StringValidator.sortField txtBox.Text
                    txtBox.Text <- validValue

                    validValue |> (SetAlbumNameSort >> EditProject >> dispatch))
                ]

            // Year
            TitledTextBox.create "Year"
                [ Grid.column 1
                  Grid.row 3 ]
                [ TextBox.horizontalAlignment HorizontalAlignment.Left
                  TextBox.width 65.
                  FixedTextBox.text (string state.Project.Year)
                  FixedTextBox.onTextChanged (fun text ->
                    match Int32.TryParse text with
                    | true, year -> year |> SetYear |> EditProject |> dispatch
                    | false, _ -> ())
                ]

            StackPanel.create [
                Grid.columnSpan 2
                Grid.row 4
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    // Show sort fields
                    CheckBox.create [
                        CheckBox.content (translate "ShowSortFields")
                        CheckBox.isChecked (state.ShowSortFields && not state.ShowJapaneseFields)
                        CheckBox.onChecked (fun _ -> true |> ShowSortFields |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> ShowSortFields |> dispatch)
                    ]

                    // Show Japanese fields
                    CheckBox.create [
                        CheckBox.margin (8., 0.,0., 0.)
                        CheckBox.content (translate "ShowJapaneseFields")
                        CheckBox.isChecked (state.ShowJapaneseFields && not state.ShowSortFields)
                        CheckBox.onChecked (fun _ -> true |> ShowJapaneseFields |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> ShowJapaneseFields |> dispatch)
                    ]
                ]
            ]
        ]
    ]

let private coverArt state dispatch =
    let albumArt = AvaloniaBitmapLoader.getBitmap ()
    let brush, toolTip =
        if String.notEmpty state.Project.AlbumArtFile && albumArt.IsNone then
            Brushes.DarkRed, translatef "LoadingCoverArtFailed" [| IO.Path.GetFileName state.Project.AlbumArtFile  |]
        else
            Brushes.Black, translate "SelectCoverArtToolTip"

    Border.create [
        DockPanel.dock Dock.Top
        Border.borderThickness 2.
        Border.horizontalAlignment HorizontalAlignment.Center
        Border.borderBrush brush
        Border.child (
            Image.create [
                Image.source (albumArt |> Option.defaultValue placeholder)
                Image.width 200.
                Image.height 200.
                Image.onTapped (fun _ -> Dialog.CoverArt |> ShowDialog |> dispatch)
                Image.onKeyDown (fun args ->
                    if args.Key = Key.Space then
                        args.Handled <- true
                        Dialog.CoverArt |> ShowDialog |> dispatch)
                Image.cursor Cursors.hand
                Image.focusable true
                ToolTip.tip toolTip
            ])
    ]

let view state dispatch =
    DockPanel.create [
        Grid.rowSpan 2
        DockPanel.children [
            coverArt state dispatch

            projectInfo state dispatch

            audioControls state dispatch

            buildControls state dispatch
        ]
    ]
