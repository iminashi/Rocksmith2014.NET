module DLCBuilder.Views.AudioControls

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open DLCBuilder
open Media

let private panel state dispatch =
    let audioPath = state.Project.AudioFile.Path
    let previewPath = state.Project.AudioPreviewFile.Path
    let previewExists = IO.File.Exists(previewPath)
    let notCalculatingVolume =
        not (state.RunningTasks |> Set.exists (function VolumeCalculation (MainAudio | PreviewAudio) -> true | _ -> false))

    Panel.create [
        Panel.children [
            ViewBox.create [
                Viewbox.width 80.
                Viewbox.height 80.
                Viewbox.margin 12.
                Viewbox.verticalAlignment VerticalAlignment.Center
                Viewbox.horizontalAlignment HorizontalAlignment.Right
                Viewbox.child (
                    Path.create [
                        Path.data Icons.volumeHigh
                        Path.fill "#44444444"
                    ]
                )
            ]
            StackPanel.create [
                StackPanel.margin 4.
                StackPanel.children [
                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            // Main audio filename
                            TextBlock.create [
                                TextBlock.margin (4.0, 4.0, 0.0, 4.0)
                                TextBlock.verticalAlignment VerticalAlignment.Center
                                TextBlock.text (
                                    if String.notEmpty audioPath then
                                        IO.Path.GetFileName(audioPath)
                                    else
                                        translate "NoMainAudioFile"
                                )
                            ]

                            match state.AudioLength with
                            | Some audioLength ->
                                let lengthStr =
                                    if audioLength.TotalHours >= 1 then
                                        "hh\:mm\:ss"
                                    else
                                        "mm\:ss"
                                    |> audioLength.ToString

                                // Separator
                                TextBlock.create [
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                    TextBlock.text "  |  "
                                ]

                                // Audio length
                                TextBlock.create [
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                    TextBlock.text lengthStr
                                    TextBlock.cursor Cursors.hand
                                    TextBlock.onTapped (fun e ->
                                        match e.Source with
                                        | :? TextBlock as t ->
                                            Application.Current.Clipboard.SetTextAsync(t.Text)
                                            |> ignore
                                        | _ ->
                                            ())
                                    ToolTip.tip (translate "ClickToCopyDuration")
                                ]
                            | None ->
                                ()
                        ]
                    ]

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            // Main volume
                            FixedNumericUpDown.create [
                                NumericUpDown.minWidth 130.
                                NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                                NumericUpDown.minimum -45.
                                NumericUpDown.maximum 45.
                                NumericUpDown.increment 0.5
                                NumericUpDown.formatString "+0.0;-0.0;0.0"
                                NumericUpDown.isEnabled (not <| state.RunningTasks.Contains(VolumeCalculation MainAudio))
                                FixedNumericUpDown.value state.Project.AudioFile.Volume
                                FixedNumericUpDown.onValueChanged (SetAudioVolume >> EditProject >> dispatch)
                                ToolTip.tip (translate "AudioVolumeToolTip")
                            ]

                            // Select audio file
                            Button.create [
                                Button.content (
                                    PathIcon.create [
                                        PathIcon.data Icons.folderOpen
                                    ])
                                Button.isEnabled notCalculatingVolume
                                Button.classes [ "borderless-btn" ]
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
                            TextBlock.create [
                                TextBlock.margin (4.0, 4.0, 0.0, 4.0)
                                TextBlock.text (
                                    if String.notEmpty previewPath then
                                        IO.Path.GetFileName(previewPath)
                                    else
                                        translate "NoPreviewAudioFile"
                                )
                            ]
                        ]
                    ]

                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.children [
                            // Preview audio volume
                            FixedNumericUpDown.create [
                                NumericUpDown.minWidth 130.
                                NumericUpDown.margin (2.0, 2.0, 2.0, 2.0)
                                NumericUpDown.horizontalAlignment HorizontalAlignment.Left
                                NumericUpDown.minimum -45.
                                NumericUpDown.maximum 45.
                                NumericUpDown.increment 0.5
                                NumericUpDown.formatString "+0.0;-0.0;0.0"
                                NumericUpDown.isEnabled (not <| state.RunningTasks.Contains(VolumeCalculation PreviewAudio))
                                FixedNumericUpDown.value state.Project.AudioPreviewFile.Volume
                                FixedNumericUpDown.onValueChanged (SetPreviewVolume >> EditProject >> dispatch)
                                ToolTip.tip (translate "PreviewAudioVolumeToolTip")
                            ]

                            // Select preview audio file
                            Button.create [
                                Button.content (
                                    PathIcon.create [
                                        PathIcon.data Icons.folderOpen
                                    ])
                                Button.isEnabled notCalculatingVolume
                                Button.classes [ "borderless-btn" ]
                                Button.onClick (fun _ -> Dialog.PreviewFile |> ShowDialog |> dispatch)
                                ToolTip.tip (translate "SelectPreviewAudioFile")
                            ]

                            // Create preview audio
                            Button.create [
                                Button.content (
                                    hStack [
                                        PathIcon.create [
                                            PathIcon.data Icons.scissors
                                        ]
                                        locText "Create..." [ TextBlock.margin (8., 0., 0., 0.) ]
                                    ])
                                Button.classes [ "borderless-btn" ]
                                Button.isEnabled (IO.File.Exists(audioPath))
                                Button.onClick (fun _ -> dispatch (CreatePreviewAudio InitialSetup))
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
        ]
    ]

let view state dispatch =
    Border.create [
        DockPanel.dock Dock.Top
        Border.background "#181818"
        Border.borderThickness 1.
        Border.cornerRadius 6.
        Border.margin 2.
        Border.child (panel state dispatch)
    ]
