module DLCBuilder.HotKeys

open Avalonia.Input
open System

let [<Literal>] private CtrlAltMac = KeyModifiers.Meta ||| KeyModifiers.Alt
let [<Literal>] private CtrlAltWin = KeyModifiers.Control ||| KeyModifiers.Alt
let private isMac = OperatingSystem.IsMacOS()

let (|Ctrl|CtrlAlt|None|Other|) keyModifier =
    match keyModifier with
    | CtrlAltMac when isMac -> CtrlAlt
    | CtrlAltWin when not isMac -> CtrlAlt
    | KeyModifiers.Meta when isMac -> Ctrl
    | KeyModifiers.Control when not isMac -> Ctrl
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
        dispatch (ShowOverlay (ConfigEditor FocusedSetting.None))

    | Ctrl, Key.J ->
        dispatch ShowJapaneseLyricsCreator

    | Ctrl, Key.T ->
        dispatch ShowToneCollection

    | Ctrl, Key.I ->
        dispatch (ShowDialog Dialog.ToolkitImport)

    | Ctrl, Key.A ->
        dispatch (ShowDialog Dialog.PsarcImport)

    | Ctrl, Key.B ->
        dispatch (Build Test)

    | Ctrl, Key.R ->
        dispatch (Build Release)

    | Ctrl, Key.V ->
        dispatch CheckArrangements

    | Ctrl, Key.OemPlus
    | Ctrl, Key.Add ->
        dispatch (ShowDialog Dialog.AddArrangements)

    | None, Key.Escape ->
        dispatch (CloseOverlay OverlayCloseMethod.EscapeKey)

    | _ ->
        ()
