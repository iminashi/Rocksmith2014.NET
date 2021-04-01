// fsharplint:disable MemberNames
[<AutoOpen>]
module Extensions

open Avalonia.Controls
open Avalonia.Input
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Builder

type MenuItem with
    static member inputGesture<'t when 't :> MenuItem>(value: KeyGesture) : IAttr<'t> =
        AttrBuilder<'t>.CreateProperty<KeyGesture>(MenuItem.InputGestureProperty, value, ValueNone)
