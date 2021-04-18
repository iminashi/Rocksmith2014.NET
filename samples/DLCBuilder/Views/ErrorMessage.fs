module DLCBuilder.Views.ErrorMessage

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder

let view dispatch msg info =
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
                        Path.margin (0., 0., 14., 0.)
                    ]
                    locText "error" [
                        TextBlock.fontSize 18.
                    ]
                ]
            ]

            // Message
            TextBlock.create [
                TextBlock.fontSize 16.
                TextBlock.text msg
                TextBlock.margin 10.0
            ]

            match info with
            | None -> ()
            | Some moreInfo ->
                Expander.create [
                    Expander.header (translate "moreInfo")
                    Expander.content (
                        TextBox.create [
                            TextBox.maxWidth 450.
                            TextBox.maxHeight 450.
                            TextBox.horizontalScrollBarVisibility ScrollBarVisibility.Auto
                            TextBox.verticalScrollBarVisibility ScrollBarVisibility.Auto
                            TextBox.text moreInfo
                        ]
                    )
                ]

            // OK button
            Button.create [
                Button.fontSize 18.
                Button.padding (80., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (translate "ok")
                Button.onClick (fun _ -> dispatch CloseOverlay)
            ]
        ]
    ] :> IView
