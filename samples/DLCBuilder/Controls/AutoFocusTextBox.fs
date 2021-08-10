namespace DLCBuilder

open Avalonia.Controls
open Avalonia.Styling
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Builder

[<Sealed>]
type AutoFocusTextBox() =
    inherit TextBox()
    interface IStyleable with member _.StyleKey = typeof<TextBox>

    override _.OnInitialized() =
        base.OnInitialized()
        base.Focus()
        match base.Text with
        | null ->
            ()
        | text ->
            base.CaretIndex <- text.Length

[<RequireQualifiedAccess>]
module AutoFocusTextBox =
    let create (attrs: IAttr<AutoFocusTextBox> list): IView<AutoFocusTextBox> =
        ViewBuilder.Create<AutoFocusTextBox>(attrs)
