// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open System
open System.Reactive.Linq

[<Sealed>]
type FixedAutoCompleteBox() =
    inherit AutoCompleteBox()
    let mutable textChangedSub: IDisposable = null
    let mutable validationSub: IDisposable = null
    let mutable changeCallback: string -> unit = ignore
    let mutable validationCallback: string -> bool = fun _ -> true

    override _.StyleKeyOverride = typeof<AutoCompleteBox>

    member val NoNotify = false with get, set

    member val ValidationErrorMessage = "" with get, set

    member this.ValidationCallback
        with get(): string -> bool = validationCallback
        and set(v) =
            if notNull validationSub then validationSub.Dispose()
            validationCallback <- v
            validationSub <-
                this.GetObservable(AutoCompleteBox.TextProperty)
                    .Where(fun _ -> this.ValidationErrorMessage <> "")
                    .Subscribe(fun text ->
                        let errors =
                            if validationCallback text then
                                null
                            else
                                seq { box this.ValidationErrorMessage }

                        this.SetValue(DataValidationErrors.ErrorsProperty, errors)
                        |> ignore)

    member this.OnTextChangedCallback
        with get(): string -> unit = changeCallback
        and set(v) =
            if notNull textChangedSub then textChangedSub.Dispose()
            changeCallback <- v
            textChangedSub <-
                this.GetObservable(AutoCompleteBox.TextProperty)
                    // Skip initial value
                    .Skip(1)
                    .Where(fun _ -> not this.NoNotify)
                    .Subscribe(changeCallback)

    override _.OnDetachedFromLogicalTree(e) =
        if notNull textChangedSub then textChangedSub.Dispose()
        if notNull validationSub then validationSub.Dispose()
        base.OnDetachedFromLogicalTree(e)

    static member onTextChanged fn =
        let getter: FixedAutoCompleteBox -> (string -> unit) = fun c -> c.OnTextChangedCallback
        let setter: FixedAutoCompleteBox * (string -> unit) -> unit = fun (c, f) -> c.OnTextChangedCallback <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<FixedAutoCompleteBox>.CreateProperty<string -> unit>
            ("OnTextChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member validation fn =
        let getter: FixedAutoCompleteBox -> (string -> bool) = fun c -> c.ValidationCallback
        let setter: FixedAutoCompleteBox * (string -> bool) -> unit = fun (c, f) -> c.ValidationCallback <- f

        AttrBuilder<FixedAutoCompleteBox>.CreateProperty<string -> bool>
            ("Validation", fn, ValueSome getter, ValueSome setter, ValueNone)

    static member validationErrorMessage message =
        let getter: FixedAutoCompleteBox -> string = fun c -> c.ValidationErrorMessage
        let setter: FixedAutoCompleteBox * string -> unit = fun (c, v) -> c.ValidationErrorMessage <- v

        AttrBuilder<FixedAutoCompleteBox>.CreateProperty<string>
            ("ValidationErrorMessage", message, ValueSome getter, ValueSome setter, ValueNone)

    static member text(text: string) =
        let getter: FixedAutoCompleteBox -> string = fun c -> c.Text
        let setter: FixedAutoCompleteBox * string -> unit = fun (c, v) ->
            // Ignore notifications originating from code
            c.NoNotify <- true
            c.Text <- v
            c.NoNotify <- false

        AttrBuilder<FixedAutoCompleteBox>.CreateProperty<string>
            ("Text", text, ValueSome getter, ValueSome setter, ValueNone)

[<RequireQualifiedAccess>]
module FixedAutoCompleteBox =
    let create (attrs: IAttr<FixedAutoCompleteBox> list) : IView<FixedAutoCompleteBox> =
        ViewBuilder.Create<FixedAutoCompleteBox>(attrs)
