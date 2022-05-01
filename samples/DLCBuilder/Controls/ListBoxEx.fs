// fsharplint:disable MemberNames
namespace DLCBuilder

open Avalonia.Controls
open Avalonia.Controls.Primitives
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.Types
open Avalonia.Input
open System

(* Created as a workaround to the issues with using the ListBox with FuncUI:
    - Having identical items causes an infinite update loop.
    - Cannot change the item template after it is set.

   Also adds functionality to move or delete the selected item. *)

[<Sealed>]
type ListBoxEx() =
    inherit UserControl()

    let mutable selectionChangedHandler: int -> unit = ignore
    let mutable itemMovedHandler: MoveDirection -> unit = ignore
    let mutable itemDeletedHandler: unit -> unit = ignore
    let mutable selected = -1
    let st = StackPanel()

    do
        base.Focusable <- true
        base.Content <-
            ScrollViewer(
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = st
            )

    override _.OnKeyDown(e) =
        e.Handled <- true

        match e.KeyModifiers, e.Key with
        // Select the first item with the space key when nothing is selected.
        | KeyModifiers.None, Key.Space when selected = -1 && st.Children.Count > 0 ->
            selected <- 0
            selectionChangedHandler selected

        | KeyModifiers.None, Key.Delete when selected <> -1 ->
            itemDeletedHandler ()

        | KeyModifiers.Alt, (Key.Up | Key.Down as key) when selected <> -1 ->
            itemMovedHandler (if key = Key.Up then Up else Down)

        | KeyModifiers.None, (Key.Up | Key.Down as key) when st.Children.Count > 0 ->
            let change = if key = Key.Up then -1 else 1
            let oldSelection = selected
            selected <- Math.Clamp(selected + change, 0, st.Children.Count - 1)

            if oldSelection <> selected then
                selectionChangedHandler selected
                st.Children[selected].BringIntoView()

        | _ ->
            e.Handled <- false

    member _.Children
        with get() = st.Children

    member _.SelectedIndex
        with get() = selected
        and set(v) = selected <- v

    member _.OnSelectedIndexChanged
        with get() = selectionChangedHandler
        and set(v) = selectionChangedHandler <- v

    member _.OnItemMoved
        with get() = itemMovedHandler
        and set(v) = itemMovedHandler <- v

    member _.OnItemDeleted
        with get() = itemDeletedHandler
        and set(v) = itemDeletedHandler <- v

    static member children(value: IView list) =
        let getter: ListBoxEx-> obj = (fun x -> x.Children :> obj)

        AttrBuilder<ListBoxEx>.CreateContentMultiple("Children", ValueSome getter, ValueNone, value)

    static member selectedIndex(index: int) =
        let getter (c: ListBoxEx) = c.SelectedIndex
        let setter: ListBoxEx * int -> unit = fun (c, v) -> c.SelectedIndex <- v

        AttrBuilder<ListBoxEx>.CreateProperty<int>
            ("SelectedIndex", index, ValueSome getter, ValueSome setter, ValueNone)

    static member onSelectedIndexChanged (fn: int -> unit) =
        let getter (c: ListBoxEx) = c.OnSelectedIndexChanged
        let setter: ListBoxEx * (int -> unit) -> unit = fun (c, f) -> c.OnSelectedIndexChanged <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<ListBoxEx>.CreateProperty<int -> unit>
            ("SelectedIndexChanged", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member onItemMoved (fn: MoveDirection -> unit) =
        let getter (c: ListBoxEx) = c.OnItemMoved
        let setter: ListBoxEx * (MoveDirection -> unit) -> unit = fun (c, f) -> c.OnItemMoved <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<ListBoxEx>.CreateProperty<MoveDirection -> unit>
            ("ItemMoved", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

    static member onItemDeleted (fn: unit -> unit) =
        let getter (c: ListBoxEx) = c.OnItemDeleted
        let setter: ListBoxEx * (unit -> unit) -> unit = fun (c, f) -> c.OnItemDeleted <- f
        // Keep the same callback once set
        let comparer _ = true

        AttrBuilder<ListBoxEx>.CreateProperty<unit -> unit>
            ("ItemDeleted", fn, ValueSome getter, ValueSome setter, ValueSome comparer)

[<RequireQualifiedAccess>]
module ListBoxEx =
    let create (attrs: IAttr<ListBoxEx> list) : IView<ListBoxEx> =
        ViewBuilder.Create<ListBoxEx>(attrs)
