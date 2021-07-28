// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Input
open Avalonia.Input.Platform
open Avalonia.Styling
open System.Threading
open System
open System.Reactive.Linq

[<AutoOpen>]
module private Extension =
    // Copy-paste from FuncUI source except for the skip part
    type AttrBuilder<'view> with
        static member CreateSubscriptionSkipFirst<'arg>(property: AvaloniaProperty<'arg>, func: 'arg -> unit, ?subPatchOptions: SubPatchOptions) : IAttr<'view> =
            let subscribeFunc (control: IControl, _handler: Delegate) =
                let cts = new CancellationTokenSource()
                control
                    .GetObservable(property)
                    // Skip the initial value of the observable to avoid unnecessary messages and infinite update loops
                    .Skip(1)
                    .Subscribe(func, cts.Token)
                cts
            
            let attr = Attr<'view>.Subscription {
                Name = property.Name + ".PropertySub"
                Subscribe = subscribeFunc
                Func = Action<_>(func)
                FuncType = func.GetType()
                Scope =
                    match Option.defaultValue Never subPatchOptions with
                    | Always -> Guid.NewGuid() |> box
                    | Never -> null
                    | OnChangeOf t -> t
            }
            attr :> IAttr<'view>

type FixedTextBox() =
    inherit TextBox()
    interface IStyleable with member _.StyleKey = typeof<TextBox>

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

    static member onTextChanged<'t when 't :> TextBox>(func: string -> unit, ?subPatchOptions) =
        AttrBuilder<'t>.CreateSubscriptionSkipFirst<string>(TextBox.TextProperty, func, ?subPatchOptions = subPatchOptions)

[<AutoOpen>]
module FixedTextBox =
    let create (attrs: IAttr<FixedTextBox> list): IView<FixedTextBox> =
        ViewBuilder.Create<FixedTextBox>(attrs)
