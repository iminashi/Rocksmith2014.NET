module DLCBuilder.Dialogs

open System.Collections.Generic
open Avalonia
open Avalonia.Controls
open Avalonia.Threading
open Rocksmith2014.Common

let private window =
    lazy (Application.Current.ApplicationLifetime :?> ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime).MainWindow

let private createFilters name (extensions : string seq) =
    let filter = FileDialogFilter(Extensions = List(extensions), Name = name)
    List(seq { filter })

let audioFileFilters () = createFilters (translate "audioFiles") (seq { "wav"; "ogg"; "wem" })
let xmlFileFilter () = createFilters (translate "rocksmithArrangementFiles") (seq { "xml" })
let imgFileFilter () = createFilters (translate "imageFiles") (seq { "png"; "jpg"; "dds" })
let ddsFileFilter () = createFilters (translate "ddsTextureFiles") (seq { "dds" })
let profileFilter () = createFilters (translate "profileFiles") (seq { "*" })
let projectFilter () = createFilters (translate "projectFiles") (seq { "rs2dlc" })
let psarcFilter () = createFilters (translate "psarcFiles") (seq { "psarc" })
let toolkitFilter () = createFilters (translate "toolkitFiles") (seq { "dlc.xml" })
let toneImportFilter () = createFilters (translate "toneImportFiles") (seq { "tone2014.xml"; "psarc" })
let wwiseConsoleAppFilter (platform: Platform) () =
    let fileExt =
        match platform with
        | PC -> "exe"
        | Mac -> "sh"
    createFilters ($"WwiseConsole.{fileExt}") (seq { fileExt })

/// Shows an open folder dialog.
let openFolderDialog title directory = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string>(fun () ->
            let dialog = OpenFolderDialog(Title = title, Directory = Option.toObj directory)
            dialog.ShowAsync window.Value)

    return Option.ofString result }

/// Shows a save file dialog.
let saveFileDialog title filters initialFileName directory = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string>(fun () ->
            let dialog =
                SaveFileDialog(
                    Title = title,
                    Filters = filters,
                    InitialFileName = Option.toObj initialFileName,
                    Directory = Option.toObj directory)
            dialog.ShowAsync window.Value)

    return Option.ofString result }

let private createOpenFileDialog t f d m =
    OpenFileDialog(Title = t, Filters = f, Directory = Option.toObj d, AllowMultiple = m)

/// Shows an open file dialog for selecting a single file.
let openFileDialog title filters directory = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string[]>(fun () ->
            let dialog = createOpenFileDialog title filters directory false
            dialog.ShowAsync window.Value)
    match result with
    | [| file |] -> return Some file
    | _ -> return None }

/// Shows an open file dialog that allows selecting multiple files.
let openMultiFileDialog title filters directory = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string[]>(fun () ->
            let dialog = createOpenFileDialog title filters directory true
            dialog.ShowAsync window.Value)
    match result with
    | null | [||] -> return None
    | arr -> return Some arr }
