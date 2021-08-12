// fsharplint:disable MemberNames
namespace DLCBuilder

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.Layout
open Avalonia.Media
open System

[<Sealed>]
type AutoFocusSearchBox() =
    inherit UserControl()

    let textBox =
        FixedTextBox(Watermark = translate "Search", AutoFocus = true)

    let deleteButton =
        Border(Background = Brushes.Transparent,
               IsVisible = false,
               HorizontalAlignment = HorizontalAlignment.Right,
               VerticalAlignment = VerticalAlignment.Center,
               Margin = Thickness(0., 0., 10., 2.),
               Cursor = Media.Cursors.hand,
               Child = Path(Fill = Brushes.Gray, Data = Media.Icons.x))

    do
        textBox.KeyUp.Add (fun _ ->
            deleteButton.IsVisible <- not <| String.IsNullOrEmpty textBox.Text)

        deleteButton.Tapped.Add (fun _ ->
            deleteButton.IsVisible <- false
            textBox.Clear()
            textBox.Focus())

        let panel = Panel()
        panel.Children.Add textBox
        panel.Children.Add deleteButton
        base.Content <- panel

    member _.TextBox = textBox

    member val TextChanged : string -> unit = ignore with get, set

    override this.OnInitialized() =
        base.OnInitialized()
        textBox.GetObservable(TextBox.TextProperty).Add this.TextChanged

[<RequireQualifiedAccess>]
module AutoFocusSearchBox =
    let create (attrs: IAttr<AutoFocusSearchBox> list): IView<AutoFocusSearchBox> =
        ViewBuilder.Create<AutoFocusSearchBox>(attrs)

type AutoFocusSearchBox with
    static member text(text: string) =
        let getter (c: AutoFocusSearchBox) = c.TextBox.Text
        let setter : AutoFocusSearchBox * string -> unit = (fun (c, v) -> c.TextBox.Text <- v)

        AttrBuilder<AutoFocusSearchBox>.CreateProperty<string>("Text", text, ValueSome getter, ValueSome setter, ValueNone)

    static member onTextChanged(func: string -> unit) =
        let getter (c: AutoFocusSearchBox) = c.TextChanged
        let setter : AutoFocusSearchBox * (string -> unit) -> unit = (fun (c, v) -> c.TextChanged <- v)

        AttrBuilder<AutoFocusSearchBox>.CreateProperty<string -> unit>("TextChanged", func, ValueSome getter, ValueSome setter, ValueNone)
