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

[<Sealed>]
type ToneKnobSlider() =
    inherit Slider()
    let mutable gearKnob = Default.Knob
    let mutable sub: IDisposable = null
    let mutable changeCallback: string * float32 -> unit = ignore

    do base.IsSnapToTickEnabled <- true

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
        with get(): string * float32 -> unit = changeCallback
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

    static member onKnobValueChanged fn =
        let getter (c: ToneKnobSlider) = c.OnValueChangedCallback
        let setter: ToneKnobSlider * (string * float32 -> unit) -> unit = fun (c, f) -> c.OnValueChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<ToneKnobSlider>.CreateProperty<string * float32 -> unit>
            ("OnKnobValueChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member knob(knob: GearKnob) =
        let getter (c: ToneKnobSlider) = c.Knob
        let setter: ToneKnobSlider * GearKnob -> unit = fun (c, v) ->
            c.NoNotify <- true
            c.Knob <- v
            c.NoNotify <- false

        AttrBuilder<ToneKnobSlider>.CreateProperty<GearKnob>
            ("Knob", knob, ValueSome getter, ValueSome setter, ValueNone)

    static member value(value: double) =
        let getter (c: ToneKnobSlider) = c.Value
        let setter: ToneKnobSlider * double -> unit = fun (c, v) ->
            // Ignore notifications originating from code
            c.NoNotify <- true
            c.Value <- v
            c.NoNotify <- false

        AttrBuilder<ToneKnobSlider>.CreateProperty<double>
            ("Value", value, ValueSome getter, ValueSome setter, ValueNone)

[<RequireQualifiedAccess>]
module ToneKnobSlider =
    let create (attrs: IAttr<ToneKnobSlider> list) : IView<ToneKnobSlider> =
        ViewBuilder.Create<ToneKnobSlider>(attrs)
