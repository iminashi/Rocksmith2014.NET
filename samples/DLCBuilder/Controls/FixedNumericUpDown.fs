// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Styling
open System
open System.Reactive.Linq

[<Sealed>]
type FixedNumericUpDown() =
    inherit NumericUpDown()
    let mutable sub: IDisposable = null
    let mutable changeCallback: float -> unit = ignore

    interface IStyleable with member _.StyleKey = typeof<NumericUpDown>

    member val NoNotify = false with get, set

    member this.OnValueChangedCallback
        with get(): float -> unit = changeCallback
        and set(v) =
            if notNull sub then sub.Dispose()
            changeCallback <- v
            sub <-
                this.GetObservable(NumericUpDown.ValueProperty)
                    // Skip initial value
                    .Skip(1)
                    .Where(fun _ -> not this.NoNotify)
                    .Subscribe(changeCallback)

    override _.OnDetachedFromLogicalTree(e) =
        if notNull sub then sub.Dispose()
        base.OnDetachedFromLogicalTree(e)

    static member onValueChanged(fn) =
        let getter: FixedNumericUpDown -> (float -> unit) = fun c -> c.OnValueChangedCallback
        let setter: FixedNumericUpDown * (float -> unit) -> unit = fun (c, f) -> c.OnValueChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<FixedNumericUpDown>.CreateProperty<float -> unit>
            ("OnValueChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member value(value: float) =
        let getter: FixedNumericUpDown -> float = fun c -> c.Value
        let setter: FixedNumericUpDown * float -> unit = fun (c, v) ->
            // Ignore notifications originating from code
            c.NoNotify <- true
            c.Value <- v
            c.NoNotify <- false

        AttrBuilder<FixedNumericUpDown>.CreateProperty<float>
            ("SelectedItem", value, ValueSome getter, ValueSome setter, ValueNone)

[<RequireQualifiedAccess>]
module FixedNumericUpDown =
    let create (attrs: IAttr<FixedNumericUpDown> list) : IView<FixedNumericUpDown> =
        ViewBuilder.Create<FixedNumericUpDown>(attrs)
