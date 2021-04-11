module DLCBuilder.Dialogs

open System
open System.Collections.Generic
open System.IO
open Avalonia
open Avalonia.Controls
open Avalonia.Threading
open Elmish
open Rocksmith2014.Common
open Rocksmith2014.DLCProject

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
let private openFolderDialog title directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string>(fun () ->
            let dialog = OpenFolderDialog(Title = translate title, Directory = Option.toObj directory)
            dialog.ShowAsync window.Value)

    match result with
    | null | "" -> return Ignore
    | path -> return msg path }

/// Shows a save file dialog.
let private saveFileDialog title filter initialFileName directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string>(fun () ->
            let dialog =
                SaveFileDialog(
                    Title = translate title,
                    Filters = createFileFilters filter,
                    InitialFileName = Option.toObj initialFileName,
                    Directory = Option.toObj directory)
            dialog.ShowAsync window.Value)

    match result with
    | null | "" -> return Ignore
    | path -> return msg path }

let private createOpenFileDialog t f d m =
    OpenFileDialog(Title = translate t, Filters = createFileFilters f, Directory = Option.toObj d, AllowMultiple = m)

/// Shows an open file dialog for selecting a single file.
let private openFileDialog title filter directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string[]>(fun () ->
            let dialog = createOpenFileDialog title filter directory false
            dialog.ShowAsync window.Value)
    match result with
    | [| file |] -> return msg file
    | _ -> return Ignore }

/// Shows an open file dialog that allows selecting multiple files.
let private openMultiFileDialog title filters directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string[]>(fun () ->
            let dialog = createOpenFileDialog title filters directory true
            dialog.ShowAsync window.Value)
    match result with
    | null | [||] -> return Ignore
    | files -> return msg files }
   
/// Shows the given dialog type.
let showDialog dialogType state =
    // No initial directory
    let ofd title filter msg = openFileDialog title filter None msg

    let dialog = 
        match dialogType with
        | Dialog.OpenProject ->
            ofd "selectProjectFile" ProjectFiles OpenProject

        | Dialog.ToolkitImport ->
            ofd "selectImportToolkitTemplate" ToolkitTemplates ImportToolkitTemplate

        | Dialog.PsarcImport ->
            ofd "selectImportPsarc" PSARCFiles (Dialog.PsarcImportTargetFolder >> ShowDialog)

        | Dialog.PsarcImportTargetFolder psarcPath ->
            openFolderDialog "selectPsarcExtractFolder" None (fun folder -> ImportPsarc(psarcPath, folder))

        | Dialog.PsarcUnpack ->
            ofd "selectUnpackPsarc" PSARCFiles (UnpackPSARC >> ToolsMsg)

        | Dialog.RemoveDD ->
            openMultiFileDialog "selectRemoveDDXML" RocksmithXMLFiles None (RemoveDD >> ToolsMsg)

        | Dialog.TestFolder ->
            openFolderDialog "selectTestFolder" None (SetTestFolderPath >> EditConfig)

        | Dialog.ProjectFolder ->
            openFolderDialog "selectProjectFolder" None (SetProjectsFolderPath >> EditConfig)

        | Dialog.ProfileFile ->
            ofd "selectProfile" ProfileFiles (SetProfilePath >> EditConfig)

        | Dialog.AddArrangements ->
            openMultiFileDialog "selectArrangement" RocksmithXMLFiles None AddArrangements

        | Dialog.ToneImport ->
            ofd "selectImportToneFile" ToneImportFiles ImportTonesFromFile

        | Dialog.WwiseConsole ->
            ofd "selectWwiseConsolePath" WwiseConsoleApplication (SetWwiseConsolePath >> EditConfig)
            
        | Dialog.CoverArt ->
            ofd "selectCoverArt" ImageFiles SetCoverArt

        | Dialog.AudioFile isCustom ->
            let msg =
                match isCustom with
                | true -> Some >> SetCustomAudioPath >> EditInstrumental
                | false -> SetAudioFile
            ofd "selectAudioFile" AudioFiles msg

        | Dialog.CustomFont ->
            ofd "selectCustomFont" DDSFiles (Some >> SetCustomFont >> EditVocals)

        | Dialog.ExportTone tone ->
            let initialFileName = Some $"{tone.Name}.tone2014.xml"
            saveFileDialog "exportToneAs" ToneExportFiles initialFileName None (fun path -> ExportTone(tone, path))

        | Dialog.SaveProjectAs ->
            let initialFileName =
                state.OpenProjectFile
                |> Option.map Path.GetFileName
                |> Option.orElseWith (fun () ->
                    sprintf "%s_%s" state.Project.ArtistName.SortValue state.Project.Title.SortValue
                    |> StringValidator.fileName
                    |> sprintf "%s.rs2dlc"
                    |> Some)

            let initialDir =
                state.OpenProjectFile
                |> Option.map Path.GetDirectoryName
                |> Option.orElse (Option.ofString state.Config.ProjectsFolderPath)

            saveFileDialog "saveProjectAsDialog" ProjectFiles initialFileName initialDir SaveProject

    state, Cmd.OfAsync.result dialog
