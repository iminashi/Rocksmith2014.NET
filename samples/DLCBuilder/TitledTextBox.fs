module DLCBuilder.TitledTextBox

open Avalonia.FuncUI.Types
open Avalonia.FuncUI.DSL
open Avalonia.Controls
open Avalonia.Layout

let create title (generalProps: IAttr<StackPanel> list) (textBoxProps: IAttr<TextBox> list) = 
    StackPanel.create [
        yield! generalProps

        StackPanel.orientation Orientation.Vertical
        StackPanel.children [
            TextBlock.create [
                TextBlock.margin (8.0, 0.)
                TextBlock.fontSize 12.0
                TextBlock.text title
            ]
            TextBox.create [
                ToolTip.tip title
                TextBox.minHeight 32.
                TextBox.height 32.
                yield! textBoxProps
            ]
        ]
    ]
