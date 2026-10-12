// fsharplint:disable MemberNames
[<AutoOpen>]
module FuncUIExtensions

open Avalonia.Controls
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Builder
open Avalonia.FuncUI.DSL
open Avalonia.Input

type KeyboardNavigation with
    static member tabNavigation<'t when 't :> Control>(value: KeyboardNavigationMode) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<KeyboardNavigationMode>
            (KeyboardNavigation.TabNavigationProperty, value, ValueNone)

type CheckBox with
    static member onIsCheckedChanged((f: bool -> unit), ?sbo: SubPatchOptions) =
        Primitives.ToggleButton.onIsCheckedChanged((fun args ->
            match args.Source with
            | :? CheckBox as cb ->
                if cb.IsChecked.HasValue then
                    f cb.IsChecked.Value
            | _ -> ()),
            ?subPatchOptions = sbo)
