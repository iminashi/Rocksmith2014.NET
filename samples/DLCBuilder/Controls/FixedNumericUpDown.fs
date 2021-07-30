// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Styling
open System
open System.Reactive.Linq

type FixedNumericUpDown() =
    inherit NumericUpDown()
    let mutable sub : IDisposable = null
    let mutable changeCallback : float -> unit = ignore

    interface IStyleable with member _.StyleKey = typeof<NumericUpDown>

    member val NoNotify = false with get, set

    member this.OnValueChangedCallback
        with get() : float -> unit = changeCallback
        and set(v) =
            if not <| isNull sub then sub.Dispose()
            changeCallback <- v
            sub <-
                this.GetObservable(NumericUpDown.ValueProperty)
                    // Skip initial value
                    .Skip(1)
                    .Where(fun _ -> not this.NoNotify)
                    .Subscribe(changeCallback)

    override _.OnDetachedFromVisualTree(e) =
        if not <| isNull sub then sub.Dispose()
        base.OnDetachedFromVisualTree(e)

    static member onValueChanged<'t when 't :> FixedNumericUpDown> fn =
        let getter : 't -> (float -> unit) = fun c -> c.OnValueChangedCallback
        let setter : ('t * (float -> unit)) -> unit = fun (c, f) -> c.OnValueChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<'t>.CreateProperty<float -> unit>("OnValueChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member value<'t when 't :> FixedNumericUpDown>(value: float) =
        let getter : 't -> float = fun c -> c.Value
        let setter : 't * float -> unit = fun (c, v) ->
            // Ignore notifications originating from code
            c.NoNotify <- true
            c.Value <- v
            c.NoNotify <- false

        AttrBuilder<'t>.CreateProperty<float>("SelectedItem", value, ValueSome getter, ValueSome setter, ValueNone)

[<AutoOpen>]
module FixedNumericUpDown =
    let create (attrs: IAttr<FixedNumericUpDown> list): IView<FixedNumericUpDown> =
        ViewBuilder.Create<FixedNumericUpDown>(attrs)
