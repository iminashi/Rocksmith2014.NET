// fsharplint:disable MemberNames
namespace DLCBuilder

open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Media

type HelpButton() =
    inherit UserControl()

    let text = TextBlock(TextWrapping = TextWrapping.Wrap, MaxWidth = 400., FontSize = 16.)
    let flyOut = Flyout(ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway, Content = text)
    let icon = PathIcon(Data = Media.Icons.helpOutline, Foreground = Brushes.Gray)
    let button = Button(Content = icon, Flyout = flyOut)

    do
        button.Classes.Add("borderless-btn")
        base.Content <- button

    member _.HelpText
        with get() = text.Text
        and set(v) = text.Text <- v

    static member helpText(text: string) =
        let getter (c: HelpButton) = c.HelpText
        let setter : HelpButton * string -> unit = fun (c, v) -> c.HelpText <- v

        AttrBuilder<HelpButton>.CreateProperty<string>
            ("SelectedIndex", text, ValueSome getter, ValueSome setter, ValueNone)

[<RequireQualifiedAccess>]
module HelpButton =
    let create (attrs: IAttr<HelpButton> list): IView<HelpButton> =
        ViewBuilder.Create<HelpButton>(attrs)
