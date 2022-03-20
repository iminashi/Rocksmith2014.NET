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

type PathIcon with
    static member data<'t>(value: Geometry) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<Geometry>(PathIcon.DataProperty, value, ValueNone)
        
module PathIcon =
    let create (attrs: IAttr<PathIcon> list) : IView<PathIcon> =
        ViewBuilder.Create<PathIcon>(attrs)

module Panel =
    let create (attrs: IAttr<Panel> list) : IView<Panel> =
        ViewBuilder.Create<Panel>(attrs)

type KeyboardNavigation with
    static member isTabStop<'t when 't :> Control>(value: bool) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<bool>(KeyboardNavigation.IsTabStopProperty, value, ValueNone)

    static member tabNavigation<'t when 't :> Control>(value: KeyboardNavigationMode) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<KeyboardNavigationMode>
            (KeyboardNavigation.TabNavigationProperty, value, ValueNone)

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

    static member allowDrop<'t when 't :> Control> (allow: bool) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<bool> (DragDrop.AllowDropProperty, allow, ValueNone)
