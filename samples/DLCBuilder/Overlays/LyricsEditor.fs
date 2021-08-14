module DLCBuilder.LyricsEditor

open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Media
open Avalonia.Layout
open System
open JapaneseLyricsCreator

let wrap ch =
    WrapPanel.create [
        WrapPanel.children ch
    ]

let view _state dispatch (creatorState: LyricsCreatorState) =
    let dispatch' = LyricsCreatorMsg >> dispatch

    vStack [
        hStack [
            Button.create [
                Button.content "Save..."
                Button.padding (15., 5.)
                Button.margin 4.
                Button.onClick (fun _ -> Dialog.SaveJapaneseLyrics |> ShowDialog |> dispatch)
            ]

            Button.create [
                Button.content "Undo"
                Button.padding (15., 5.)
                Button.margin 4.
                Button.onClick (fun _ -> UndoLyricsChange |> dispatch')
                Button.isEnabled (LyricsCreatorState.canUndo creatorState)
            ]
        ]

        StackPanel.create [
            StackPanel.orientation Orientation.Horizontal
            StackPanel.children [
                ScrollViewer.create [
                    ScrollViewer.width 480.
                    ScrollViewer.height 600.
                    ScrollViewer.content (
                        creatorState.MatchedLines
                        |> Array.mapi (fun lineNumber line ->
                            line
                            |> Array.mapi (fun wordNumber syllable ->
                                Border.create [
                                    Border.margin (0., 4.)
                                    Border.background Brushes.Black
                                    Border.child (
                                        vStack [
                                            TextBlock.create [
                                                TextBlock.padding (12., 12.)
                                                TextBlock.text (syllable.Japanese |> Option.toObj)
                                                if syllable.Japanese.IsSome then
                                                    TextBlock.cursor Media.Cursors.hand
                                                    TextBlock.classes [ "hover-highlight-jp" ]
                                                TextBlock.onTapped ((fun _ ->
                                                    dispatch' (CombineJapaneseWithNext { LineNumber = lineNumber; Index = wordNumber })),
                                                    SubPatchOptions.OnChangeOf(lineNumber, wordNumber))
                                            ]

                                            Rectangle.create [
                                                Rectangle.height 1.
                                                Rectangle.fill Brushes.Gray
                                            ]

                                            TextBlock.create [
                                                TextBlock.classes [ "hover-highlight" ]
                                                TextBlock.padding (12., 12.)
                                                TextBlock.text syllable.Vocal.Lyric
                                                TextBlock.cursor Media.Cursors.hand
                                                TextBlock.onTapped ((fun _ ->
                                                    dispatch' (CombineSyllableWithNext { LineNumber = lineNumber; Index = wordNumber })),
                                                    SubPatchOptions.OnChangeOf(lineNumber, wordNumber))
                                            ]
                                        ])
                                ] |> generalize)
                            |> List.ofArray
                            |> wrap
                            |> generalize)
                        |> List.ofArray
                        |> vStack
                    )
                ]

                FixedTextBox.create [
                    TextBox.width 350.
                    TextBox.height 600.
                    TextBox.watermark "Paste Japanese lyrics here."
                    TextBox.acceptsReturn true
                    TextBox.verticalContentAlignment VerticalAlignment.Top
                    TextBox.fontFamily Media.Fonts.japanese
                    FixedTextBox.text creatorState.JapaneseLyrics
                    FixedTextBox.onTextChanged (SetJapaneseLyrics >> dispatch')
                ]
            ]
        ]
    ] |> generalize
