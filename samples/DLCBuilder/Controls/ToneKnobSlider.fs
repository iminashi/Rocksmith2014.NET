// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Styling
open System
open System.Reactive.Linq

type ToneKnobSlider() =
    inherit Slider()
    let mutable sub : IDisposable = null
    let mutable changeCallback : string * float32 -> unit = ignore

    interface IStyleable with member _.StyleKey = typeof<Slider>

    member val NoNotify = false with get, set

    member val KnobKey = "" with get, set

    member this.OnValueChangedCallback
        with get() : string * float32 -> unit = changeCallback
        and set(v) =
            if not <| isNull sub then sub.Dispose()
            changeCallback <- v
            sub <-
                this.GetObservable(Slider.ValueProperty)
                    // Skip initial value
                    .Skip(1)
                    .Where(fun _ -> not this.NoNotify)
                    .Select(fun value -> this.KnobKey, float32 value)
                    .Subscribe(changeCallback)

    override _.OnDetachedFromLogicalTree(e) =
        if not <| isNull sub then sub.Dispose()
        base.OnDetachedFromLogicalTree(e)

    static member onKnobValueChanged<'t when 't :> ToneKnobSlider> fn =
        let getter : 't -> (string * float32 -> unit) = fun c -> c.OnValueChangedCallback
        let setter : ('t * (string * float32 -> unit)) -> unit = fun (c, f) -> c.OnValueChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<'t>.CreateProperty<string * float32 -> unit>("OnKnobValueChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member knobKey<'t when 't :> ToneKnobSlider>(knobKey: string) =
        let getter : 't -> string = (fun c -> c.KnobKey)
        let setter : 't * string -> unit = (fun (c, v) -> c.KnobKey <- v)

        AttrBuilder<'t>.CreateProperty<string>("KnobKey", knobKey, ValueSome getter, ValueSome setter, ValueNone)

    static member value<'t when 't :> ToneKnobSlider>(value: double) =
        let getter : 't -> double = fun c -> c.Value
        let setter : 't * double -> unit = fun (c, v) ->
            // Ignore notifications originating from code
            c.NoNotify <- true
            c.Value <- v
            c.NoNotify <- false

        AttrBuilder<'t>.CreateProperty<double>("Value", value, ValueSome getter, ValueSome setter, ValueNone)

[<AutoOpen>]
module ToneKnobSlider =
    let create (attrs: IAttr<ToneKnobSlider> list): IView<ToneKnobSlider> =
        ViewBuilder.Create<ToneKnobSlider>(attrs)
