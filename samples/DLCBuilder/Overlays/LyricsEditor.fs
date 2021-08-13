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

let view _state dispatch (editorState: LyricsCreatorState) =
    let dispatch = LyricsCreatorMsg >> dispatch

    StackPanel.create [
        StackPanel.orientation Orientation.Horizontal
        StackPanel.children [
            ScrollViewer.create [
                ScrollViewer.width 480.
                ScrollViewer.height 600.
                ScrollViewer.content (
                    editorState.MatchedLines
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
                                                TextBlock.classes [ "hover-highlight" ]
                                            TextBlock.onTapped ((fun _ ->
                                                dispatch (CombineJapaneseWithNext(lineNumber, wordNumber))),
                                                SubPatchOptions.Always)
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
                                                dispatch (CombineSyllableWithNext(lineNumber, wordNumber))),
                                                SubPatchOptions.Always)
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

            vStack [
                Button.create [
                    Button.content "Undo"
                    Button.onClick (fun _ -> UndoLyricsChange |> dispatch)
                    Button.isEnabled (LyricsCreatorState.canUndo())
                ]
                FixedTextBox.create [
                    TextBox.width 350.
                    TextBox.height 600.
                    TextBox.acceptsReturn true
                    TextBox.verticalContentAlignment VerticalAlignment.Top
                    TextBox.fontFamily Media.Fonts.japanese
                    FixedTextBox.text editorState.JapaneseLyrics
                    FixedTextBox.onTextChanged (SetJapaneseLyrics >> dispatch)
                ]
            ]
        ]
    ] |> generalize
