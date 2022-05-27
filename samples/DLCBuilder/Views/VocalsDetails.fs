module DLCBuilder.Views.VocalsDetails

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.DLCProject
open System.IO
open DLCBuilder

let view dispatch (vocals: Vocals) =
    StackPanel.create [
        StackPanel.margin 6.
        StackPanel.children [
            // Japanese Lyrics
            CheckBox.create [
                CheckBox.margin 8.
                CheckBox.content (translate "Japanese")
                CheckBox.isChecked vocals.Japanese
                CheckBox.onChecked (fun _ -> true |> SetIsJapanese |> EditVocals |> dispatch)
                CheckBox.onUnchecked (fun _ -> false |> SetIsJapanese |> EditVocals |> dispatch)
                ToolTip.tip (translate "JapaneseLyricsToolTip")
            ]

            // Custom Font
            DockPanel.create [
                DockPanel.children [
                    locText "CustomFont" [ DockPanel.dock Dock.Left ]

                    Rectangle.create [
                        Rectangle.height 1.
                        Rectangle.fill Brushes.Gray
                        Rectangle.margin (8., 0.)
                    ]
                ]
            ]
            // Custom Font Filename
            FixedTextBox.create [
                TextBox.margin 8.
                TextBox.watermark (translate "CustomFontPath")
                FixedTextBox.text (vocals.CustomFont |> Option.toObj)
                FixedTextBox.validation (fun text ->
                    match text with
                    | null | "" ->
                        true
                    | path ->
                        File.Exists(Path.ChangeExtension(path, "glyphs.xml")))
                FixedTextBox.validationErrorMessage (translate "GlyphsFileNotFound")
                TextBox.onLostFocus (fun e ->
                    let txtBox = e.Source :?> TextBox
                    txtBox.Text |> Option.ofString |> SetCustomFont |> EditVocals |> dispatch)
            ]
            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    // Select Button
                    Button.create [
                        Button.margin (0.0, 4.0, 4.0, 4.0)
                        Button.padding (15., 5.)
                        Button.content (translate "Browse...")
                        Button.onClick (fun _ -> Dialog.CustomFont |> ShowDialog |> dispatch)
                        ToolTip.tip (translate "SelectCustomFontToolTip")
                    ]

                    // Remove Button
                    Button.create [
                        Button.margin (0.0, 4.0, 4.0, 4.0)
                        Button.padding (15., 5.)
                        Button.content (translate "Remove")
                        Button.isEnabled vocals.CustomFont.IsSome
                        Button.onClick (fun _ -> None |> SetCustomFont |> EditVocals |> dispatch)
                        ToolTip.tip (translate "RemoveCustomFontToolTip")
                    ]
                ]
            ]

            // View Lyrics Button
            Button.create [
                Button.padding (25., 10.)
                Button.content (translate "ViewLyrics")
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.onClick (fun _ -> dispatch ShowLyricsViewer)
            ]
        ]
    ]
