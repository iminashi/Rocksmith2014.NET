module DLCBuilder.Views.LyricsViewer

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open DLCBuilder
open DLCBuilder.Media

let view dispatch (lyrics: string) isJapanese =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            // Lyrics
            TextBox.create [
                TextBox.text lyrics
                TextBox.fontSize 16.
                if isJapanese then TextBox.fontFamily Fonts.japanese
                TextBox.verticalScrollBarVisibility ScrollBarVisibility.Auto
                TextBox.maxHeight 500.
                TextBox.minWidth 400.
                TextBox.maxWidth 800.
            ]

            // Close button
            Button.create [
                Button.fontSize 14.
                Button.padding (50., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (translate "Close")
                Button.onClick (fun _ -> dispatch (CloseOverlay OverlayCloseMethod.OverlayButton))
                Button.isDefault true
            ]
        ]
    ] |> generalize
