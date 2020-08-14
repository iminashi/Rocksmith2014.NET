module Dialogs

open System.Collections.Generic
open Avalonia
open Avalonia.Controls
open DLCBuilder

let private window =
    lazy (Application.Current.ApplicationLifetime :?> ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime).MainWindow

let private createFilters name (extensions : string seq) =
    let filter = FileDialogFilter(Extensions = List(extensions), Name = name)
    List(seq { filter })

let audioFileFilters = createFilters "Audio Files" (seq { "wav"; "wem" })
let xmlFileFilter = createFilters "Rocksmith Arrangement Files" (seq { "xml" })
let imgFileFilter = createFilters "Images" (seq { "png"; "jpg" })
let ddsFileFilter = createFilters "DDS Texture Files" (seq { "dds" })

/// Shows an open folder dialog.
let openFolderDialog title directory =
    let dialog = OpenFolderDialog(Title = title, Directory = Option.toObj directory)

    async {
        let! result = dialog.ShowAsync window.Value
        return Option.ofString result }

/// Shows a save file dialog.
let saveFileDialog title filters initialFileName directory =
    let dialog =
        SaveFileDialog(
            Title = title,
            Filters = filters,
            InitialFileName = Option.toObj initialFileName,
            Directory = Option.toObj directory)

    async {
        let! result = dialog.ShowAsync window.Value
        return Option.ofString result }

let private createOpenFileDialog t f d m =
    OpenFileDialog(Title = t, Filters = f, Directory = Option.toObj d, AllowMultiple = m)

/// Shows an open file dialog for selecting a single file.
let openFileDialog title filters directory =
    let dialog = createOpenFileDialog title filters directory false

    async {
        match! dialog.ShowAsync window.Value with
        | [| file |] -> return Some file
        | _ -> return None }

/// Shows an open file dialog that allows selecting multiple files.
let openMultiFileDialog title filters directory =
    let dialog = createOpenFileDialog title filters directory true

    async {
        match! dialog.ShowAsync window.Value with
        | null | [||] -> return None
        | arr -> return Some arr }
