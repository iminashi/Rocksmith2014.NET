module DLCBuilder.ErrorMessage

open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.FuncUI.Types

let view state dispatch msg =
    StackPanel.create [
        StackPanel.spacing 8.
        StackPanel.children [
            TextBlock.create [
                TextBlock.fontSize 18.
                TextBlock.text (state.Localization.GetString "error")
                TextBlock.horizontalAlignment HorizontalAlignment.Center
            ]
            TextBlock.create [
                TextBlock.fontSize 16.
                TextBlock.text msg
                TextBlock.margin 10.0
            ]
            Button.create [
                Button.fontSize 18.
                Button.padding (80., 10.)
                Button.horizontalAlignment HorizontalAlignment.Center
                Button.content (state.Localization.GetString "ok")
                Button.onClick (fun _ -> CloseOverlay |> dispatch)
            ]
        ]
    ] :> IView
