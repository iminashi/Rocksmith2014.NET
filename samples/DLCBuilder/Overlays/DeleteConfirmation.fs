module DLCBuilder.Views.DeleteConfirmation

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder
open System.IO

let view dispatch (files: string list) =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    Path.create [
                        Path.fill Brushes.Gray
                        Path.data Media.Icons.help
                        Path.verticalAlignment VerticalAlignment.Center
                        Path.margin (0., 0., 10., 0.)
                    ]
                    locText "ConfirmDelete" [
                        TextBlock.fontSize 18.
                    ]
                ]
            ]

            // Confirmation message
            TextBlock.create [
                TextBlock.fontSize 16.
                TextBlock.text (translatef "DeleteConfirmation" [| files.Length |])
                TextBlock.margin 10.0
            ]

            // List of files to be deleted
            ScrollViewer.create [
                ScrollViewer.maxHeight 250.
                ScrollViewer.content (
                    ItemsControl.create [
                        ItemsControl.dataItems (files |> List.map Path.GetFileName)
                    ]
                )
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.spacing 8.
                StackPanel.children [
                    // Yes button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "Yes")
                        Button.onClick ((fun _ ->
                            files |> DeleteConfirmed |> dispatch),
                            SubPatchOptions.Always)
                    ]

                    // No button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "No")
                        Button.onClick (fun _ -> dispatch (CloseOverlay OverlayCloseMethod.OverlayButton))
                    ]
                ]
            ]
        ]
    ] |> generalize
