module DLCBuilder.Views.VocalsDetails

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open Rocksmith2014.DLCProject
open System
open System.IO
open DLCBuilder
open DLCBuilder.Media

let view state dispatch (vocals: Vocals) =
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

            Grid.create [
                 Grid.columnDefinitions "auto,*"
                 Grid.rowDefinitions "*,*"
                 Grid.margin (0., 0., 0., 8.)
                 Grid.children [
                     // Master ID
                     locText "MasterID" [
                         TextBlock.isVisible state.Config.ShowAdvanced
                         TextBlock.verticalAlignment VerticalAlignment.Center
                         TextBlock.horizontalAlignment HorizontalAlignment.Center
                     ]
                     FixedTextBox.create [
                         Grid.column 1
                         TextBox.isVisible state.Config.ShowAdvanced
                         TextBox.horizontalAlignment HorizontalAlignment.Stretch
                         FixedTextBox.text (string vocals.MasterId)
                         FixedTextBox.validationErrorMessage (translate "EnterNumberLargerThanZero")
                         FixedTextBox.validation Utils.isNumberGreaterThanZero
                         TextBox.onLostFocus (fun arg ->
                             let txtBox = arg.Source :?> TextBox
                             match Int32.TryParse(txtBox.Text) with
                             | true, masterId when masterId > 0 ->
                                 SetVocalsMasterId masterId |> EditVocals |> dispatch
                             | _ ->
                                 ()
                         )
                     ]

                     // Persistent ID
                     locText "PersistentID" [
                         Grid.row 1
                         TextBlock.isVisible state.Config.ShowAdvanced
                         TextBlock.verticalAlignment VerticalAlignment.Center
                         TextBlock.horizontalAlignment HorizontalAlignment.Center
                     ]
                     FixedTextBox.create [
                         Grid.row 1
                         Grid.column 1
                         TextBox.isVisible state.Config.ShowAdvanced
                         TextBox.horizontalAlignment HorizontalAlignment.Stretch
                         FixedTextBox.text (vocals.PersistentId.ToString("N"))
                         FixedTextBox.validationErrorMessage (translate "EnterAValidGUID")
                         FixedTextBox.validation (Guid.TryParse >> fst)
                         TextBox.onLostFocus (fun arg ->
                             let txtBox = arg.Source :?> TextBox
                             match Guid.TryParse(txtBox.Text) with
                             | true, id ->
                                 SetVocalsPersistentId id |> EditVocals |> dispatch
                             | false, _ ->
                                 ()
                         )
                     ]
                 ]
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
            DockPanel.create [
                DockPanel.children [
                    // Select Button
                    iconButton Icons.folderOpen [
                        DockPanel.dock Dock.Right
                        Button.onClick (fun _ -> Dialog.CustomFont |> ShowDialog |> dispatch)
                        ToolTip.tip (translate "SelectCustomFontToolTip")
                    ]

                    // Custom Font Filename
                    FixedTextBox.create [
                        TextBox.watermark (translate "CustomFontPath")
                        FixedTextBox.text (vocals.CustomFont |> Option.toObj)
                        FixedTextBox.validation (fun path ->
                            String.IsNullOrEmpty(path) || File.Exists(Path.ChangeExtension(path, "glyphs.xml")))
                        FixedTextBox.validationErrorMessage (translate "GlyphsFileNotFound")
                        TextBox.onLostFocus (fun e ->
                            let txtBox = e.Source :?> TextBox
                            txtBox.Text |> Option.ofString |> SetCustomFont |> EditVocals |> dispatch)
                    ]
                ]
            ]

            StackPanel.create [
                StackPanel.orientation Orientation.Horizontal
                StackPanel.horizontalAlignment HorizontalAlignment.Center
                StackPanel.children [
                    // Generate Font Button
                    Button.create [
                        Button.margin (0.0, 4.0, 4.0, 4.0)
                        Button.padding (15., 5.)
                        Button.content (translate "GenerateFont")
                        Button.onClick (fun _ -> dispatch StartFontGenerator)
                        Button.isEnabled state.Config.FontGeneratorPath.IsSome
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
