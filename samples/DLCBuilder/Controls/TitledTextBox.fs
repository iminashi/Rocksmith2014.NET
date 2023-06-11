module DLCBuilder.TitledTextBox

open Avalonia.FuncUI.Types
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout

let create title (generalProps: IAttr<StackPanel> list) (textBoxProps: IAttr<FixedTextBox> list) =
    StackPanel.create [
        yield! generalProps

        StackPanel.orientation Orientation.Vertical
        StackPanel.children [
            TextBlock.create [
                TextBlock.margin (4.0, 0.)
                TextBlock.fontSize 12.0
                TextBlock.text (translate title)
            ]
            FixedTextBox.create [
                ToolTip.tip (translate title)
                TextBox.minHeight 32.
                TextBox.height 32.
                yield! textBoxProps
            ]
        ]
    ]
