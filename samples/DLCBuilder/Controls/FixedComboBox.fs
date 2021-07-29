// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Styling
open System
open System.Reactive.Linq

type FixedComboBox() =
    inherit ComboBox()
    let mutable sub : IDisposable = null
    let mutable changeCallback : obj -> unit = ignore

    interface IStyleable with member _.StyleKey = typeof<ComboBox>

    member val NoNotify = false with get, set

    member this.OnValueChangedCallback
        with get() : obj -> unit = changeCallback
        and set(v) =
            if not <| isNull sub then sub.Dispose()
            changeCallback <- v
            sub <-
                this.GetObservable(ComboBox.SelectedItemProperty)
                    // Skip initial value
                    .Skip(1)
                    .Where(fun _ -> not <| this.NoNotify)
                    .Subscribe(changeCallback)

    static member onSelectedItemChanged<'t when 't :> FixedComboBox> fn =
        let getter : 't -> (obj -> unit) = fun c -> c.OnValueChangedCallback
        let setter : ('t * (obj -> unit)) -> unit = fun (c, f) -> c.OnValueChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<'t>.CreateProperty<obj -> unit>("OnSelectedItemChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member selectedItem<'t when 't :> FixedComboBox>(value: obj) =
        let getter : 't -> obj = fun c -> c.SelectedItem
        let setter : 't * obj -> unit = fun (c, v) ->
            // Ignore notifications originating from code
            c.NoNotify <- true
            c.SelectedItem <- v
            c.NoNotify <- false

        AttrBuilder<'t>.CreateProperty<obj>("SelectedItem", value, ValueSome getter, ValueSome setter, ValueNone)

[<AutoOpen>]
module FixedComboBox =
    let create (attrs: IAttr<FixedComboBox> list): IView<FixedComboBox> =
        ViewBuilder.Create<FixedComboBox>(attrs)
