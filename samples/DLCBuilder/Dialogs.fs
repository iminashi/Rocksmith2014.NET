module DLCBuilder.Dialogs

open System
open System.Collections.Generic
open Avalonia
open Avalonia.Controls
open Avalonia.Threading
open Rocksmith2014.Common

let private window =
    lazy (Application.Current.ApplicationLifetime :?> ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime).MainWindow

let private createFilters name (extensions: string seq) =
    let filter = FileDialogFilter(Extensions = List(extensions), Name = name)
    List(seq { filter })

let private createFileFilters filter =
    let extensions =
        match filter with
        | AudioFiles ->
            [ "wav"; "ogg"; "wem" ]
        | RocksmithXMLFiles ->
            [ "xml" ]
        | ImageFiles ->
            [ "png"; "jpg"; "dds" ]
        | DDSFiles ->
            [ "dds" ]
        | ProfileFiles ->
            [ ]
        | ProjectFiles ->
            [ "rs2dlc" ]
        | PSARCFiles ->
            [ "psarc"]
        | ToolkitTemplates ->
            [ "dlc.xml" ]
        | ToneImportFiles ->
            [ "tone2014.xml"; "tone2014.json"; "psarc" ]
        | ToneExportFiles ->
            [ "tone2014.xml"; "tone2014.json" ]
        | WwiseConsoleApplication ->
            [ if OperatingSystem.IsWindows() then "exe" else "sh" ]

    let name =
        match filter with
        | WwiseConsoleApplication ->
            let ext = if OperatingSystem.IsWindows() then "exe" else "sh"
            $"WwiseConsole.{ext}"
        | other ->
            other |> string |> translate

    createFilters name extensions

/// Shows an open folder dialog.
let openFolderDialog title directory = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string>(fun () ->
            let dialog = OpenFolderDialog(Title = title, Directory = Option.toObj directory)
            dialog.ShowAsync window.Value)

    return Option.ofString result }

/// Shows a save file dialog.
let saveFileDialog title filter initialFileName directory = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string>(fun () ->
            let dialog =
                SaveFileDialog(
                    Title = title,
                    Filters = createFileFilters filter,
                    InitialFileName = Option.toObj initialFileName,
                    Directory = Option.toObj directory)
            dialog.ShowAsync window.Value)

    return Option.ofString result }

let private createOpenFileDialog t f d m =
    OpenFileDialog(Title = t, Filters = createFileFilters f, Directory = Option.toObj d, AllowMultiple = m)

/// Shows an open file dialog for selecting a single file.
let openFileDialog title filter directory = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string[]>(fun () ->
            let dialog = createOpenFileDialog title filter directory false
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
