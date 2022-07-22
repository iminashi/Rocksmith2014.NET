[<AutoOpen>]
module Helpers

open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.Media
open DLCBuilder

let hStack children =
    StackPanel.create [
        StackPanel.orientation Orientation.Horizontal
        StackPanel.children children
    ]

let vStack children =
    StackPanel.create [
        StackPanel.orientation Orientation.Vertical
        StackPanel.children children
    ]

let locText key attr =
    TextBlock.create [
        yield! attr
        TextBlock.text (translate key)
    ]

let iconButton (icon: Geometry) attr =
    let pathIcon =
        PathIcon.create [
            PathIcon.data icon
        ]

    Button.create [
        Button.content pathIcon
        Button.classes [ "borderless-btn" ]
        yield! attr
    ]

let maximizeOrRestore (window: Window) =
    if window.WindowState = WindowState.Maximized then
        window.WindowState <- WindowState.Normal
    else
        window.WindowState <- WindowState.Maximized
