// fsharplint:disable MemberNames
namespace Avalonia.FuncUI.DSL

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Styling
open System
open System.Reactive.Linq
open System.Collections

type FixedComboBox() =
    inherit ComboBox()
    let mutable sub : IDisposable = null
    let mutable changeCallback : obj -> unit = ignore

    interface IStyleable with member _.StyleKey = typeof<ComboBox>

    member val NoNotify = false with get, set

    member this.OnValueChangedCallback
        with get() : obj -> unit = changeCallback
        and set(v) =
            if notNull sub then sub.Dispose()
            changeCallback <- v
            sub <-
                this.GetObservable(ComboBox.SelectedItemProperty)
                    // Skip initial value
                    .Skip(1)
                    .Where(fun _ -> not this.NoNotify)
                    .Subscribe(changeCallback)

    override _.OnDetachedFromLogicalTree(e) =
        if notNull sub then sub.Dispose()
        base.OnDetachedFromLogicalTree(e)

    static member onSelectedItemChanged<'t when 't :> FixedComboBox> fn =
        let getter : 't -> (obj -> unit) = fun c -> c.OnValueChangedCallback
        let setter : 't * (obj -> unit) -> unit = fun (c, f) -> c.OnValueChangedCallback <- f
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

    // Fix for the selection being lost in the UI when the items are changed.
    static member dataItems<'t when 't :> FixedComboBox>(items: IEnumerable) =
        let getter : 't -> IEnumerable = fun c -> c.Items
        let setter : 't * IEnumerable -> unit = fun (c, v) ->
            let wasSelected = c.SelectedItem
            c.Items <- v
            if notNull wasSelected && items |> Seq.cast<obj> |> Seq.contains wasSelected then
                c.NoNotify <- true
                c.SelectedItem <- wasSelected
                c.NoNotify <- false

        AttrBuilder<'t>.CreateProperty<IEnumerable>("DataItems", items, ValueSome getter, ValueSome setter, ValueNone)

[<RequireQualifiedAccess>]
module FixedComboBox =
    let create (attrs: IAttr<FixedComboBox> list): IView<FixedComboBox> =
        ViewBuilder.Create<FixedComboBox>(attrs)
