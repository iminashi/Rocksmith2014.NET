module DLCBuilder.Views.UpdateInfoMessage

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder
open DLCBuilder.Media
open DLCBuilder.OnlineUpdate
open System

let view (update: UpdateInformation) dispatch =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    Path.create [
                        Path.fill Brushes.Gray
                        Path.data Icons.alertRound
                        Path.verticalAlignment VerticalAlignment.Center
                        Path.margin (0., 0., 10., 0.)
                    ]
                    locText "NewVersionAvailable" [
                        TextBlock.fontSize 18.
                    ]
                ]
            ]

            // Current version
            hStack [
                locText "CurrentVersion" [ TextBlock.minWidth 120. ]

                TextBlock.create [
                    TextBlock.text AppVersion.versionString
                ]
            ]

            // Available version
            hStack [
                locText "AvailableVersion" [ TextBlock.minWidth 120. ]

                TextBlock.create [
                    let version = update.UpdateVersion.ToString(3)
                    let date = update.ReleaseDate.ToString("d")
                    TextBlock.text $"v{version} ({date})"
                ]
            ]

            // Changes
            ScrollViewer.create [
                ScrollViewer.background "#181818"
                ScrollViewer.maxHeight 200.
                ScrollViewer.maxWidth 700.
                ScrollViewer.content (
                    TextBlock.create [
                        TextBlock.margin 8.
                        TextBlock.text update.Changes
                        TextBlock.textWrapping TextWrapping.Wrap
                    ])
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.children [
                    // Update button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (40., 10.)
                        Button.content (
                            if OperatingSystem.IsWindows() then
                                translate "UpdateAndRestart"
                            else
                                translate "GoToDownloadPage")
                        Button.onClick (fun _ ->
                            if OperatingSystem.IsWindows() then
                                dispatch DownloadUpdate
                            else
                                Utils.openWithShell "https://github.com/iminashi/Rocksmith2014.NET/releases"
                                dispatch (CloseOverlay OverlayCloseMethod.OverlayButton))
                    ]

                    // Close button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "Close")
                        Button.onClick (fun _ -> dispatch (CloseOverlay OverlayCloseMethod.OverlayButton))
                    ]
                ]
            ]
        ]
    ] |> generalize
