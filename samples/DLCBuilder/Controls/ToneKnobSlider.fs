// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Styling
open System
open System.Reactive.Linq
open DLCBuilder.ToneGear
open Rocksmith2014.Common

module private Default =
    let Knob =
        { Name = String.Empty
          Key = String.Empty
          UnitType = "number"
          MinValue = 1f
          MaxValue = 100f
          ValueStep = 1f
          DefaultValue = 50f
          EnumValues = None }

type ToneKnobSlider() as this =
    inherit Slider()
    let mutable gearKnob = Default.Knob
    let mutable sub : IDisposable = null
    let mutable changeCallback : string * float32 -> unit = ignore

    do this.IsSnapToTickEnabled <- true

    interface IStyleable with member _.StyleKey = typeof<Slider>

    member val NoNotify = false with get, set

    member this.Knob
        with get() = gearKnob
        and set(knob) =
            gearKnob <- knob
            this.TickFrequency <- float knob.ValueStep
            this.SmallChange <- float knob.ValueStep
            this.Minimum <- float knob.MinValue
            this.Maximum <- float knob.MaxValue

    member this.OnValueChangedCallback
        with get() : string * float32 -> unit = changeCallback
        and set(v) =
            if notNull sub then sub.Dispose()
            changeCallback <- v
            sub <-
                this.GetObservable(Slider.ValueProperty)
                    // Skip initial value
                    .Skip(1)
                    .Where(fun _ -> not this.NoNotify)
                    .Select(fun value -> this.Knob.Key, float32 value)
                    .Subscribe(changeCallback)

    override _.OnDetachedFromLogicalTree(e) =
        if notNull sub then sub.Dispose()
        base.OnDetachedFromLogicalTree(e)

    static member onKnobValueChanged<'t when 't :> ToneKnobSlider> fn =
        let getter : 't -> (string * float32 -> unit) = fun c -> c.OnValueChangedCallback
        let setter : ('t * (string * float32 -> unit)) -> unit = fun (c, f) -> c.OnValueChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<'t>.CreateProperty<string * float32 -> unit>("OnKnobValueChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member knob<'t when 't :> ToneKnobSlider>(knob: GearKnob) =
        let getter : 't -> GearKnob = (fun c -> c.Knob)
        let setter : 't * GearKnob -> unit = (fun (c, v) -> c.Knob <- v)

        AttrBuilder<'t>.CreateProperty<GearKnob>("Knob", knob, ValueSome getter, ValueSome setter, ValueNone)

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
