﻿// fsharplint:disable MemberNames
[<AutoOpen>]
module FuncUIExtensions

open Avalonia.Controls
open Avalonia.Input
open Avalonia.FuncUI.Types
open Avalonia.FuncUI.Builder
open Avalonia.Controls.Selection
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