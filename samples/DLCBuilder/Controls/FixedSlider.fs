// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Styling
open System
open System.Reactive.Linq

type FixedSlider() =
    inherit Slider()
    let mutable sub : IDisposable = null
    let mutable changeCallback : double -> unit = ignore

    interface IStyleable with member _.StyleKey = typeof<Slider>

    member val NoNotify = false with get, set

    member this.OnValueChangedCallback
        with get() : double -> unit = changeCallback
        and set(v) =
            if not <| isNull sub then sub.Dispose()
            changeCallback <- v
            sub <-
                this.GetObservable(Slider.ValueProperty)
                    // Skip initial value
                    .Skip(1)
                    .Where(fun _ -> not this.NoNotify)
                    .Subscribe(changeCallback)

    override _.OnDetachedFromLogicalTree(e) =
        if not <| isNull sub then sub.Dispose()
        base.OnDetachedFromLogicalTree(e)

    static member onValueChanged<'t when 't :> FixedSlider> fn =
        let getter : 't -> (double -> unit) = fun c -> c.OnValueChangedCallback
        let setter : ('t * (double -> unit)) -> unit = fun (c, f) -> c.OnValueChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<'t>.CreateProperty<double -> unit>("OnValueChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member value<'t when 't :> FixedSlider>(value: double) =
        let getter : 't -> double = fun c -> c.Value
        let setter : 't * double -> unit = fun (c, v) ->
            // Ignore notifications originating from code
            c.NoNotify <- true
            c.Value <- v
            c.NoNotify <- false

        AttrBuilder<'t>.CreateProperty<double>("Value", value, ValueSome getter, ValueSome setter, ValueNone)

[<AutoOpen>]
module FixedSlider =
    let create (attrs: IAttr<FixedSlider> list): IView<FixedSlider> =
        ViewBuilder.Create<FixedSlider>(attrs)
