// fsharplint:disable MemberNames
[<AutoOpen>]
module FuncUIExtensions

open Avalonia.Controls
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Builder
open Avalonia.Input

type KeyboardNavigation with
    static member tabNavigation<'t when 't :> Control>(value: KeyboardNavigationMode) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<KeyboardNavigationMode>
            (KeyboardNavigation.TabNavigationProperty, value, ValueNone)
