// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open System
open System.Reactive.Linq

[<Sealed>]
type FixedNumericUpDown() =
    inherit NumericUpDown()
    let mutable sub: IDisposable = null
    let mutable changeCallback: decimal -> unit = ignore

    override _.StyleKeyOverride = typeof<NumericUpDown>

    member val NoNotify = false with get, set

    member this.OnValueChangedCallback
        with get(): decimal -> unit = changeCallback
        and set(v) =
            if notNull sub then sub.Dispose()
            changeCallback <- v
            sub <-
                this.GetObservable(NumericUpDown.ValueProperty)
                    // Skip initial value
                    .Skip(1)
                    .Where(fun _ -> not this.NoNotify)
                    .Subscribe(fun x -> x |> ValueOption.ofNullable |> ValueOption.defaultValue 0.0m |> changeCallback)

    override _.OnDetachedFromLogicalTree(e) =
        if notNull sub then sub.Dispose()
        base.OnDetachedFromLogicalTree(e)

    static member onValueChanged(fn) =
        let getter: FixedNumericUpDown -> (decimal -> unit) = fun c -> c.OnValueChangedCallback
        let setter: FixedNumericUpDown * (decimal -> unit) -> unit = fun (c, f) -> c.OnValueChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<FixedNumericUpDown>.CreateProperty<decimal -> unit>
            ("OnValueChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member value(value: Nullable<decimal>) =
        let getter: FixedNumericUpDown -> Nullable<decimal> = fun c -> c.Value
        let setter: FixedNumericUpDown * Nullable<decimal> -> unit = fun (c, v) ->
            // Ignore notifications originating from code
            c.NoNotify <- true
            c.Value <- v
            c.NoNotify <- false

        AttrBuilder<FixedNumericUpDown>.CreateProperty<Nullable<decimal>>
            ("Value", value, ValueSome getter, ValueSome setter, ValueNone)

[<RequireQualifiedAccess>]
module FixedNumericUpDown =
    let create (attrs: IAttr<FixedNumericUpDown> list) : IView<FixedNumericUpDown> =
        ViewBuilder.Create<FixedNumericUpDown>(attrs)
