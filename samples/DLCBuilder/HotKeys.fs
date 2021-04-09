module DLCBuilder.HotKeys

open Avalonia.Input

let [<Literal>] private CtrlAlt = KeyModifiers.Control ||| KeyModifiers.Alt
let [<Literal>] private Ctrl = KeyModifiers.Control
let [<Literal>] private None = KeyModifiers.None

let handleEvent dispatch (event: KeyEventArgs) =
    let dispatch msg = dispatch (HotKeyMsg msg)

    match event.KeyModifiers, event.Key with
    | Ctrl, Key.O ->
        dispatch (Msg.OpenFileDialog("selectProjectFile", ProjectFiles, OpenProject))

    | Ctrl, Key.S ->
        dispatch ProjectSaveOrSaveAs

    | CtrlAlt, Key.S ->
        dispatch ProjectSaveAs

    | Ctrl, Key.P ->
        dispatch ImportProfileTones

    | Ctrl, Key.N ->
        dispatch NewProject

    | Ctrl, Key.G ->
        dispatch ShowConfigEditor

    | Ctrl, Key.T ->
        dispatch (Msg.OpenFileDialog("selectImportToolkitTemplate", ToolkitTemplates, ImportToolkitTemplate))

    | Ctrl, Key.A ->
        dispatch (Msg.OpenFileDialog("selectImportPsarc", PSARCFiles, SelectImportPsarcFolder))

    | None, Key.Escape ->
        dispatch CloseOverlay

    | _ -> ()
