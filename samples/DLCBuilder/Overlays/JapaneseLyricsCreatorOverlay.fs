module DLCBuilder.JapaneseLyricsCreatorOverlay

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL
open Avalonia.Media
open Avalonia.Input
open Avalonia.Layout
open System
open JapaneseLyricsCreator

let private wrap ch =
    WrapPanel.create [
        WrapPanel.children ch
    ]

let rec private findParent<'a when 'a :> IControl and 'a : null> (control: IControl) =
    if isNull control.Parent then
        null
    elif control.Parent :? 'a then
        control.Parent :?> 'a
    else
        findParent control.Parent

let view _state dispatch (creatorState: LyricsCreatorState) =
    let dispatch' = LyricsCreatorMsg >> dispatch

    vStack [
        Panel.create [
            Panel.children [
                hStack [
                    Button.create [
                        Button.content (translate "Save...")
                        Button.padding (15., 5.)
                        Button.margin 4.
                        Button.onClick (fun _ -> Dialog.SaveJapaneseLyrics |> ShowDialog |> dispatch)
                    ]

                    Button.create [
                        Button.content (translate "Undo")
                        Button.padding (15., 5.)
                        Button.margin 4.
                        Button.onClick (fun _ -> UndoLyricsChange |> dispatch')
                        Button.isEnabled (LyricsCreatorState.canUndo creatorState)
                    ]
                ]

                Button.create [
                    Button.content (translate "Close")
                    Button.horizontalAlignment HorizontalAlignment.Right
                    Button.verticalAlignment VerticalAlignment.Top
                    Button.padding (15., 5.)
                    Button.onClick (fun _ -> dispatch (CloseOverlay OverlayCloseMethod.OverlayButton))
                ]
            ]
        ]
        
        StackPanel.create [
            StackPanel.orientation Orientation.Horizontal
            StackPanel.onKeyDown (fun e ->
                if e.Key = Key.Z && e.KeyModifiers = KeyModifiers.Control then
                    e.Handled <- true
                    UndoLyricsChange |> dispatch')
            StackPanel.children [
                ScrollViewer.create [
                    ScrollViewer.width 480.
                    ScrollViewer.height 600.
                    ScrollViewer.onKeyDown (fun e ->
                        let scrollViewer = findParent<ScrollViewer>(e.Source :?> IControl)
                        e.Handled <- true
                        match e.Key with
                        | Key.Down ->
                            scrollViewer.LineDown()
                        | Key.Up ->
                            scrollViewer.LineUp()
                        | Key.PageDown ->
                            scrollViewer.PageDown()
                        | Key.PageUp ->
                            scrollViewer.PageUp()
                        | _ ->
                            e.Handled <- false)
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
                                            LyricsCreatorTextBlock.create [
                                                TextBlock.classes [ "hover-highlight-jp" ]
                                                TextBlock.padding (12., 12.)
                                                TextBlock.focusable syllable.Japanese.IsSome
                                                TextBlock.text (Option.toObj syllable.Japanese)
                                                TextBlock.cursor Media.Cursors.hand
                                                LyricsCreatorTextBlock.location (lineNumber, wordNumber)
                                                LyricsCreatorTextBlock.onClick (CombineJapaneseWithNext >> dispatch')
                                            ]

                                            Rectangle.create [
                                                Rectangle.height 1.
                                                Rectangle.fill Brushes.Gray
                                            ]

                                            LyricsCreatorTextBlock.create [
                                                TextBlock.classes [ "hover-highlight" ]
                                                TextBlock.padding (12., 12.)
                                                TextBlock.focusable true
                                                TextBlock.text syllable.Vocal.Lyric
                                                TextBlock.cursor Media.Cursors.hand
                                                LyricsCreatorTextBlock.location (lineNumber, wordNumber)
                                                LyricsCreatorTextBlock.onClick (CombineSyllableWithNext >> dispatch')
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
                    TextBox.watermark (translate "PasteJapaneseLyricsHere")
                    TextBox.acceptsReturn true
                    TextBox.verticalContentAlignment VerticalAlignment.Top
                    TextBox.fontFamily Media.Fonts.japanese
                    FixedTextBox.text creatorState.JapaneseLyrics
                    FixedTextBox.onTextChanged (SetJapaneseLyrics >> dispatch')
                ]
            ]
        ]
    ] |> generalize
