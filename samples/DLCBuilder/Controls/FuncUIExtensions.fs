// fsharplint:disable MemberNames
[<AutoOpen>]
module FuncUIExtensions

open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Builder
open Avalonia.Input

module Panel =
    let create (attrs: IAttr<Panel> list) : IView<Panel> =
        ViewBuilder.Create<Panel>(attrs)

type KeyboardNavigation with
    static member isTabStop<'t when 't :> Control>(value: bool) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<bool>(KeyboardNavigation.IsTabStopProperty, value, ValueNone)

    static member tabNavigation<'t when 't :> Control>(value: KeyboardNavigationMode) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<KeyboardNavigationMode>
            (KeyboardNavigation.TabNavigationProperty, value, ValueNone)
