// fsharplint:disable MemberNames
[<AutoOpen>]
module FuncUIExtensions

open Avalonia.Controls
open Avalonia.Input
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Builder
open Avalonia.Controls.Selection

type MenuItem with
    static member inputGesture<'t when 't :> MenuItem>(value: KeyGesture) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<KeyGesture>(MenuItem.InputGestureProperty, value, ValueNone)

type ListBox with
    static member selection<'i, 't when 't :> ListBox>(value: SelectionModel<'i>) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<SelectionModel<'i>>(ListBox.SelectionProperty, value, ValueNone)
