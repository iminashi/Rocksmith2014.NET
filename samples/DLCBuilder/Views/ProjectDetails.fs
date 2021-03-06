﻿module DLCBuilder.Views.ProjectDetails

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.Platform
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open DLCBuilder
open Media

let private placeholderAlbumArt =
    lazy
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
                    locText "audio" [
                        TextBlock.margin (0., 4.)
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                    ]

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            // Main audio filename
                            locText "mainAudio" [
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
                                        translate "noAudioFile"
                                )
                            ]
                        ]
                    ]

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            // Main volume
                            NumericUpDown.create [
                                NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                                NumericUpDown.minimum -45.
                                NumericUpDown.maximum 45.
                                NumericUpDown.increment 0.5
                                NumericUpDown.value state.Project.AudioFile.Volume
                                NumericUpDown.formatString "+0.0;-0.0;0.0"
                                NumericUpDown.isEnabled (not <| state.RunningTasks.Contains (VolumeCalculation MainAudio))
                                NumericUpDown.onValueChanged (SetAudioVolume >> EditProject >> dispatch)
                                ToolTip.tip (translate "audioVolumeToolTip")
                            ]

                            // Select audio file
                            Button.create [
                                Button.minWidth 75.
                                Button.margin (0.0, 4.0, 4.0, 4.0)
                                Button.padding (10.0, 0.0)
                                Button.content (translate "select")
                                Button.isEnabled notCalculatingVolume
                                Button.onClick (fun _ -> Dialog.AudioFile false |> ShowDialog |> dispatch)
                                ToolTip.tip (translate "selectAudioFile")
                            ]

                            Menus.audio notCalculatingVolume state dispatch
                        ]
                    ]

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            // Preview audio filename
                            locText "preview" [
                                TextBlock.margin (4.0, 4.0, 0.0, 4.0)
                            ]
                            TextBlock.create [
                                TextBlock.margin (4.0, 4.0, 0.0, 4.0)
                                TextBlock.text (
                                    if String.notEmpty previewPath then
                                        IO.Path.GetFileName previewPath
                                    else
                                        translate "noAudioFile"
                                )
                            ]
                        ]
                    ]

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            // Preview audio volume
                            NumericUpDown.create [
                                NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                                NumericUpDown.minimum -45.
                                NumericUpDown.maximum 45.
                                NumericUpDown.increment 0.5
                                NumericUpDown.value state.Project.AudioPreviewFile.Volume
                                NumericUpDown.formatString "+0.0;-0.0;0.0"
                                NumericUpDown.isEnabled (not <| state.RunningTasks.Contains (VolumeCalculation PreviewAudio))
                                NumericUpDown.onValueChanged (SetPreviewVolume >> EditProject >> dispatch)
                                ToolTip.tip (translate "previewAudioVolumeToolTip")
                            ]

                            // Create preview audio
                            Button.create [
                                Button.minWidth 75.
                                Button.margin (0.0, 4.0, 4.0, 4.0)
                                Button.padding (10.0, 0.0)
                                Button.content (translate "create")
                                Button.isEnabled (not <| String.endsWith ".wem" audioPath && IO.File.Exists audioPath)
                                Button.onClick (fun _ -> dispatch (CreatePreviewAudio SetupStartTime))
                                ToolTip.tip (
                                    if previewExists then
                                        translate "previewAudioExistsToolTip"
                                    else
                                        translate "previewAudioDoesNotExistToolTip"
                                )
                            ]
                        ]
                    ]
                ]
            ]
        )
    ]

let private buildControls state dispatch =
    let canBuild = Utils.canBuild state

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
                Button.content (translate "buildTest")
                Button.isEnabled (canBuild && String.notEmpty state.Config.TestFolderPath)
                Button.onClick (fun _ -> dispatch <| Build Test)
            ]

            // Build release
            Button.create [
                Grid.column 1
                Button.padding (20., 8.)
                Button.margin 4.
                Button.fontSize 16.
                Button.content (translate "buildRelease")
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
            TitledTextBox.create (translate "dlcKey") [ Grid.column 0; Grid.row 0 ] [
                TextBox.text state.Project.DLCKey
                // Cannot filter pasted text: https://github.com/AvaloniaUI/Avalonia/issues/2611
                TextBox.onTextInput (fun e -> e.Text <- StringValidator.dlcKey e.Text)
                TextBox.onTextChanged (StringValidator.dlcKey >> SetDLCKey >> EditProject >> dispatch)
                // Display the validated DLC key if invalid characters were pasted into the textbox
                TextBox.onLostFocus ((fun e ->
                    (e.Source :?> TextBox).Text <- state.Project.DLCKey),
                    SubPatchOptions.OnChangeOf state.Project.DLCKey)
                ToolTip.tip (translate "dlcKeyTooltip")
            ]

            // Version
            TitledTextBox.create (translate "version") [ Grid.column 1; Grid.row 0 ] [
                TextBox.horizontalAlignment HorizontalAlignment.Left
                TextBox.width 65.
                TextBox.text state.Project.Version
                TextBox.onTextChanged (SetVersion >> EditProject >> dispatch)
            ]

            // Artist name
            TitledTextBox.create (translate "artistName")
                [ Grid.column 0
                  Grid.row 1
                  StackPanel.isVisible (not state.ShowSortFields && not state.ShowJapaneseFields) ]
                [ TextBox.text state.Project.ArtistName.Value
                  TextBox.onTextChanged (StringValidator.field >> SetArtistName >> EditProject >> dispatch)
                ]

            // Artist name sort
            TitledTextBox.create (translate "artistNameSort")
                [ Grid.column 0
                  Grid.row 1
                  StackPanel.isVisible (state.ShowSortFields && not state.ShowJapaneseFields) ]
                [ TextBox.text state.Project.ArtistName.SortValue
                  TextBox.onLostFocus (fun e -> 
                    let txtBox = e.Source :?> TextBox
                    let validValue = StringValidator.sortField txtBox.Text
                    txtBox.Text <- validValue

                    validValue |> (SetArtistNameSort >> EditProject >> dispatch))
                ]

            // Japanese artist name
            TitledTextBox.create (translate "japaneseArtistName")
                [ Grid.column 0
                  Grid.row 1
                  StackPanel.isVisible state.ShowJapaneseFields ]
                [ TextBox.text (defaultArg state.Project.JapaneseArtistName String.Empty)
                  TextBox.fontFamily Fonts.japanese
                  TextBox.fontSize 15.
                  TextBox.onTextChanged (StringValidator.field >> Option.ofString >> SetJapaneseArtistName >> EditProject >> dispatch)
                ]

            // Title
            TitledTextBox.create (translate "title")
                [ Grid.column 0
                  Grid.row 2
                  StackPanel.isVisible (not state.ShowSortFields && not state.ShowJapaneseFields) ]
                [ TextBox.text state.Project.Title.Value
                  TextBox.onTextChanged (StringValidator.field >> SetTitle >> EditProject >> dispatch)
                ]

            // Title sort
            TitledTextBox.create (translate "titleSort")
                [ Grid.column 0
                  Grid.row 2
                  StackPanel.isVisible state.ShowSortFields ]
                [ TextBox.text state.Project.Title.SortValue
                  TextBox.onLostFocus (fun e -> 
                    let txtBox = e.Source :?> TextBox
                    let validValue = StringValidator.sortField txtBox.Text
                    txtBox.Text <- validValue

                    validValue |> (SetTitleSort >> EditProject >> dispatch))
                ]

            // Japanese title
            TitledTextBox.create (translate "japaneseTitle")
                [ Grid.column 0
                  Grid.row 2
                  StackPanel.isVisible state.ShowJapaneseFields ]
                [ TextBox.text (defaultArg state.Project.JapaneseTitle String.Empty)
                  TextBox.fontFamily Fonts.japanese
                  TextBox.fontSize 15.
                  TextBox.onTextChanged (StringValidator.field >> Option.ofString >> SetJapaneseTitle >> EditProject >> dispatch)
                ]

            // Album name
            TitledTextBox.create (translate "albumName")
                [ Grid.column 0
                  Grid.row 3
                  StackPanel.isVisible (not state.ShowSortFields) ]
                [ TextBox.text state.Project.AlbumName.Value
                  TextBox.onTextChanged (StringValidator.field >> SetAlbumName >> EditProject >> dispatch)
                ]

            // Album name sort
            TitledTextBox.create (translate "albumNameSort")
                [ Grid.column 0
                  Grid.row 3
                  StackPanel.isVisible state.ShowSortFields ]
                [ TextBox.text state.Project.AlbumName.SortValue
                  TextBox.onLostFocus (fun e -> 
                    let txtBox = e.Source :?> TextBox
                    let validValue = StringValidator.sortField txtBox.Text
                    txtBox.Text <- validValue

                    validValue |> (SetAlbumNameSort >> EditProject >> dispatch))
                ]

            // Year
            TitledTextBox.create (translate "year")
                [ Grid.column 1
                  Grid.row 3 ]
                [ TextBox.horizontalAlignment HorizontalAlignment.Left
                  TextBox.width 65.
                  TextBox.text (string state.Project.Year)
                  TextBox.onTextChanged (fun text ->
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
                        CheckBox.content (translate "showSortFields")
                        CheckBox.isChecked (state.ShowSortFields && not state.ShowJapaneseFields)
                        CheckBox.onChecked (fun _ -> true |> ShowSortFields |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> ShowSortFields |> dispatch)
                    ]

                    // Show Japanese fields
                    CheckBox.create [
                        CheckBox.margin (8., 0.,0., 0.)
                        CheckBox.content (translate "showJapaneseFields")
                        CheckBox.isChecked (state.ShowJapaneseFields && not state.ShowSortFields)
                        CheckBox.onChecked (fun _ -> true |> ShowJapaneseFields |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> ShowJapaneseFields |> dispatch)
                    ]
                ]
            ]
        ]
    ]

let private coverArt state dispatch =
    Image.create [
        DockPanel.dock Dock.Top
        Image.source (state.CoverArt |> Option.defaultWith placeholderAlbumArt.Force)
        Image.width 200.
        Image.height 200.
        Image.onTapped (fun _ -> Dialog.CoverArt |> ShowDialog |> dispatch)
        Image.cursor Cursors.hand
        ToolTip.tip (translate "selectCoverArtToolTip")
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
