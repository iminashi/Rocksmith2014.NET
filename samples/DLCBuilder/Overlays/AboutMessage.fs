module DLCBuilder.Views.AboutMessage

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder
open DLCBuilder.Media

let view dispatch =
    DockPanel.create [
        DockPanel.children [
            // Close button
            Button.create [
                DockPanel.dock Dock.Bottom
                Button.fontSize 16.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (translate "Close")
                Button.onClick (fun _ -> CloseOverlay |> dispatch)
            ]

            vStack [
                TextBlock.create [
                    TextBlock.text "Rocksmith 2014 DLC Builder"
                    TextBlock.fontSize 20.
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.margin 4.
                ]

                // Separator
                Rectangle.create [
                    Rectangle.height 2.
                    Rectangle.fill Brushes.Gray
                ]

                // Version
                TextBlock.create [
                    TextBlock.text (translatef "ProgramVersion" [| AppVersion.current.ToString 3 |])
                    TextBlock.margin 4.
                ]

                // Disclaimer
                locText "AboutDisclaimer" [
                    TextBlock.margin (4., 8.)
                ]

                // GitHub Link
                locText "GitHubPage" [
                    TextBlock.classes [ "link" ]
                    TextBlock.margin 4.
                    TextBlock.onTapped (fun ev ->
                        ev.Handled <- true
                        Utils.openLink "https://github.com/iminashi/Rocksmith2014.NET"
                    )
                ]
            ]
        ]
    ] |> generalize
