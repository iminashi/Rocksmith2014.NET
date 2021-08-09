// fsharplint:disable MemberNames
[<AutoOpen>]
module FuncUIExtensions

open Avalonia.Controls
open Avalonia.Controls.Selection
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Builder
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Media

type MenuItem with
    static member inputGesture<'t when 't :> MenuItem>(value: KeyGesture) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<KeyGesture>(MenuItem.InputGestureProperty, value, ValueNone)

type ListBox with
    static member selection<'i, 't when 't :> ListBox>(value: SelectionModel<'i>) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<SelectionModel<'i>>(ListBox.SelectionProperty, value, ValueNone)

module Panel =
    let create (attrs: IAttr<Panel> list): IView<Panel> =
        ViewBuilder.Create<Panel>(attrs)

module ExperimentalAcrylicBorder =
    let create (attrs: IAttr<ExperimentalAcrylicBorder> list): IView<ExperimentalAcrylicBorder> =
        ViewBuilder.Create<ExperimentalAcrylicBorder>(attrs)

type ExperimentalAcrylicBorder with
    static member material<'t when 't :> ExperimentalAcrylicBorder>(value: ExperimentalAcrylicMaterial) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<ExperimentalAcrylicMaterial>(ExperimentalAcrylicBorder.MaterialProperty, value, ValueNone)

type KeyboardNavigation with
    static member isTabStop<'t>(value: bool) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<bool>(KeyboardNavigation.IsTabStopProperty, value, ValueNone)

type DragDrop with
    static member onDragEnter<'t when 't :> Control> (func: DragEventArgs -> unit, ?subPatchOptions) =
        AttrBuilder<'t>.CreateSubscription<DragEventArgs>
            (DragDrop.DragEnterEvent, func, ?subPatchOptions = subPatchOptions)

    static member onDragLeave<'t when 't :> Control> (func: RoutedEventArgs -> unit, ?subPatchOptions) =
        AttrBuilder<'t>.CreateSubscription<RoutedEventArgs>
            (DragDrop.DragLeaveEvent, func, ?subPatchOptions = subPatchOptions)

    static member onDragOver<'t when 't :> Control> (func: DragEventArgs -> unit, ?subPatchOptions) =
        AttrBuilder<'t>.CreateSubscription<DragEventArgs>
            (DragDrop.DragOverEvent, func, ?subPatchOptions = subPatchOptions)

    static member onDrop<'t when 't :> Control> (func: DragEventArgs -> unit, ?subPatchOptions) =
        AttrBuilder<'t>.CreateSubscription<DragEventArgs>
            (DragDrop.DropEvent, func, ?subPatchOptions = subPatchOptions)

    static member allowDrop<'t when 't :> Control> (allow: bool): IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<bool> (DragDrop.AllowDropProperty, allow, ValueNone)
