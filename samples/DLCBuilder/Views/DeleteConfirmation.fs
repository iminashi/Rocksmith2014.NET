module DLCBuilder.Views.DeleteConfirmation

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder

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
                    TextBlock.create [
                        TextBlock.fontSize 18.
                        TextBlock.text (translate "confirmDelete")
                    ]
                ]
            ]

            // Confirmation message
            TextBlock.create [
                TextBlock.fontSize 16.
                TextBlock.text (translatef "deleteConfirmation" [| files.Length |])
                TextBlock.margin 10.0
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
                        Button.content (translate "yes")
                        Button.onClick ((fun _ ->
                            files |> DeleteConfirmed |> dispatch), SubPatchOptions.Always)
                    ]

                    // No button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "no")
                        Button.onClick (fun _ -> dispatch CloseOverlay)
                    ]
                ]
            ]
        ]
    ] :> IView
