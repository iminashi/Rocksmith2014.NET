module DLCBuilder.Views.UpdateInfoMessage

open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Controls.Shapes
open Avalonia.Media
open DLCBuilder
open DLCBuilder.Media
open DLCBuilder.OnlineUpdate

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
                        Path.data Media.Icons.alertRound
                        Path.verticalAlignment VerticalAlignment.Center
                        Path.margin (0., 0., 10., 0.)
                    ]
                    locText "newVersionAvailable" [
                        TextBlock.fontSize 18.
                    ]
                ]
            ]

            TextBlock.create [
                TextBlock.text <| update.ReleaseDate.ToString("d")
            ]

            // Changes
            TextBlock.create [
                TextBlock.text <| update.Changes
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.children [
                    // Update button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "updateAndRestart")
                        Button.onClick (fun _ -> dispatch UpdateAndRestart)
                    ]

                    // Close button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "close")
                        Button.onClick (fun _ -> dispatch CloseOverlay)
                    ]
                ]
            ]
        ]
    ] |> generalize
