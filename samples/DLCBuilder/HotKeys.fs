module DLCBuilder.HotKeys

open Avalonia.Input
open System

let [<Literal>] private CtrlAltMac = KeyModifiers.Meta ||| KeyModifiers.Alt
let [<Literal>] private CtrlAltWin = KeyModifiers.Control ||| KeyModifiers.Alt

let (|Ctrl|CtrlAlt|None|Other|) keyModifier =
    if OperatingSystem.IsMacOS() then
        match keyModifier with
        | CtrlAltMac -> CtrlAlt
        | KeyModifiers.Meta -> Ctrl
        | KeyModifiers.None -> None
        | _ -> Other
    else
        match keyModifier with
        | CtrlAltWin -> CtrlAlt
        | KeyModifiers.Control -> Ctrl
        | KeyModifiers.None -> None
        | _ -> Other

let handleEvent dispatch (event: KeyEventArgs) =
    let dispatch msg = dispatch (HotKeyMsg msg)

    match event.KeyModifiers, event.Key with
    | Ctrl, Key.O ->
        dispatch (ShowDialog Dialog.OpenProject)

    | Ctrl, Key.S ->
        dispatch ProjectSaveOrSaveAs

    | CtrlAlt, Key.S ->
        dispatch SaveProjectAs

    | Ctrl, Key.P ->
        dispatch ImportProfileTones

    | Ctrl, Key.N ->
        dispatch NewProject

    | Ctrl, Key.G ->
        dispatch ShowConfigEditor

    | Ctrl, Key.T ->
        dispatch (ShowDialog Dialog.ToolkitImport)

    | Ctrl, Key.A ->
        dispatch (ShowDialog Dialog.PsarcImport)

    | None, Key.Escape ->
        dispatch CloseOverlay

    | _ -> ()
