[<AutoOpen>]
module Helpers

open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout

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
