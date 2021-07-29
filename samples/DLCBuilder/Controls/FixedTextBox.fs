// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Input.Platform
open Avalonia.Styling
open System
open System.Reactive.Linq

type FixedTextBox() =
    inherit TextBox()
    let mutable sub : IDisposable = null
    let mutable changeCallback : string -> unit = ignore

    interface IStyleable with member _.StyleKey = typeof<TextBox>

    member val NoNotify = false with get, set

    member this.OnTextChangedCallback
        with get() : string -> unit = changeCallback
        and set(v) =
            if not <| isNull sub then sub.Dispose()
            changeCallback <- v
            sub <-
                this.GetObservable(TextBox.TextProperty)
                    // Skip initial value
                    .Skip(1)
                    .Where(fun _ -> not <| this.NoNotify)
                    .Subscribe(changeCallback)

    // Workaround for the inability to validate text that is pasted into the textbox
    // https://github.com/AvaloniaUI/Avalonia/issues/2611
    override this.OnKeyDown(e) =
        let keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>()
        let matchGesture (gestures: ResizeArray<KeyGesture>) = gestures.Exists(fun g -> g.Matches e)

        if matchGesture keymap.Paste then
            async {
                let! text =
                    (AvaloniaLocator.Current.GetService(typeof<IClipboard>) :?> IClipboard).GetTextAsync()
                    |> Async.AwaitTask
                this.RaiseEvent(TextInputEventArgs(RoutedEvent = InputElement.TextInputEvent, Text = text))
            } |> Async.StartImmediate
            e.Handled <- true
        else
            base.OnKeyDown(e)

    static member onTextChanged<'t when 't :> FixedTextBox> fn =
        let getter : 't -> (string -> unit) = fun c -> c.OnTextChangedCallback
        let setter : ('t * (string -> unit)) -> unit = fun (c, f) -> c.OnTextChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<'t>.CreateProperty<string -> unit>("OnTextChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member text<'t when 't :> FixedTextBox>(text: string) =
        let getter : 't -> string = fun c -> c.Text
        let setter : 't * string -> unit = fun (c, v) ->
            // Ignore notifications originating from code
            c.NoNotify <- true
            c.Text <- v
            c.NoNotify <- false

        AttrBuilder<'t>.CreateProperty<string>("Text", text, ValueSome getter, ValueSome setter, ValueNone)

[<AutoOpen>]
module FixedTextBox =
    let create (attrs: IAttr<FixedTextBox> list): IView<FixedTextBox> =
        ViewBuilder.Create<FixedTextBox>(attrs)
