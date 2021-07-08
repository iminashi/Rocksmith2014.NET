// fsharplint:disable MemberNames
namespace DLCBuilder

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Shapes
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Builder
open Avalonia.Layout
open Avalonia.Media
open System

type AutoFocusSearchBox() =
    inherit UserControl()

    let textBox =
        TextBox(Watermark = translate "search")

    let deleteButton =
        Button(Background = Brushes.Transparent,
               IsVisible = false,
               HorizontalAlignment = HorizontalAlignment.Right,
               VerticalAlignment = VerticalAlignment.Center,
               Margin = Thickness(0., 0., 10., 2.),
               Padding = Thickness(0.),
               Cursor = Media.Cursors.hand,
               Content = Path(Fill = Brushes.Gray, Data = Media.Icons.x))

    do
        textBox.KeyUp.Add (fun _ ->
            deleteButton.IsVisible <- not <| String.IsNullOrEmpty textBox.Text)

        deleteButton.Click.Add (fun _ ->
            deleteButton.IsVisible <- false
            textBox.Clear()
            textBox.Focus())

        let panel = Panel()
        panel.Children.Add textBox
        panel.Children.Add deleteButton
        base.Content <- panel

    let mutable textChanged : string -> unit = ignore
    let mutable textChangedSubscription : IDisposable option = None

    member _.TextBox = textBox

    member _.TextChanged
        with get() = textChanged
        and set(value) =
            textChangedSubscription |> Option.iter (fun x -> x.Dispose())
            textChanged <- value
            textChangedSubscription <- Some (textBox.GetObservable(TextBox.TextProperty).Subscribe value)

    override this.OnInitialized() =
        base.OnInitialized()
        textBox.Focus()

[<RequireQualifiedAccess>]
module AutoFocusSearchBox =
    let create (attrs: IAttr<AutoFocusSearchBox> list): IView<AutoFocusSearchBox> =
        ViewBuilder.Create<AutoFocusSearchBox>(attrs)

type AutoFocusSearchBox with
    static member text<'t when 't :> AutoFocusSearchBox>(text: string) =
        let getter : 't -> string = (fun c -> c.TextBox.Text)
        let setter : 't * string -> unit = (fun (c, v) -> c.TextBox.Text <- v)

        AttrBuilder<'t>.CreateProperty<string>("Text", text, ValueSome getter, ValueSome setter, ValueNone)

    static member onTextChanged<'t when 't :> AutoFocusSearchBox>(func: string -> unit) =
        let getter : 't -> (string -> unit) = (fun c -> c.TextChanged)
        let setter : 't * (string -> unit) -> unit = (fun (c, v) -> c.TextChanged <- v)

        AttrBuilder<'t>.CreateProperty<string -> unit>("TextChanged", func, ValueSome getter, ValueSome setter, ValueNone)
