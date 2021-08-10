[<AutoOpen>]
module Helpers

open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
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

let maximizeOrRestore (window: Window) =
    if window.WindowState = WindowState.Maximized then
        window.WindowState <- WindowState.Normal
    else
        window.WindowState <- WindowState.Maximized
