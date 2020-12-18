module DLCBuilder.ErrorMessage

open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.Layout
open Avalonia.Media

let view state dispatch msg =
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
                    TextBlock.create [
                        TextBlock.fontSize 18.
                        TextBlock.text (state.Localization.GetString "error")
                    ]
                ]
            ]
            
            // Message
            TextBlock.create [
                TextBlock.fontSize 16.
                TextBlock.text msg
                TextBlock.margin 10.0
            ]

            // OK button
            Button.create [
                Button.fontSize 18.
                Button.padding (80., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (state.Localization.GetString "ok")
                Button.onClick (fun _ -> dispatch CloseOverlay)
            ]
        ]
    ] :> IView
