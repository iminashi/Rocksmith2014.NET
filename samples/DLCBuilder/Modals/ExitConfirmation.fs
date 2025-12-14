module DLCBuilder.Views.ExitConfirmation

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder

let view dispatch =
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
                    locText "ExitConfirmation" [
                        TextBlock.fontSize 18.
                    ]
                ]
            ]

            // Confirmation message
            TextBlock.create [
                TextBlock.fontSize 16.
                TextBlock.text (translate "ExitConfirmationMessage")
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
                        Button.content (translate "Yes")
                        Button.onClick (fun _ -> true |> ExitConfirmed |> dispatch)
                    ]

                    // No button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "No")
                        Button.onClick (fun _ -> false |> ExitConfirmed |> dispatch)
                    ]

                    // Cancel button
                    Button.create [
                        Button.fontSize 18.
                        Button.padding (80., 10.)
                        Button.content (translate "Cancel")
                        Button.onClick (fun _ -> dispatch (CloseModal ModalCloseMethod.UIButton))
                    ]
                ]
            ]
        ]
    ] |> generalize
