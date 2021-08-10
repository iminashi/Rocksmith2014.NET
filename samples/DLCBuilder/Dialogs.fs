module DLCBuilder.Dialogs

open Avalonia.Controls
open Avalonia.Threading
open Elmish
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open System.Collections.Generic
open System.IO

[<RequireQualifiedAccess>]
type FileFilter =
    | Audio
    | XML
    | Image
    | DDS
    | Profile
    | Project
    | PSARC
    | Wem
    | WavOgg
    | ToolkitTemplate
    | ToneImport
    | ToneExport
    | WwiseConsoleApplication

let private createFilters name (extensions: string seq) =
    let filter = FileDialogFilter(Extensions = List(extensions), Name = name)
    List(seq { filter })

let private createFileFilters filter =
    let extensions =
        match filter with
        | FileFilter.WavOgg ->
            [ "wav"; "ogg" ]
        | FileFilter.Audio ->
            [ "wav"; "ogg"; "wem" ]
        | FileFilter.Wem ->
            [ "wem" ]
        | FileFilter.XML ->
            [ "xml" ]
        | FileFilter.Image ->
            [ "png"; "jpg"; "dds" ]
        | FileFilter.DDS ->
            [ "dds" ]
        | FileFilter.Profile ->
            [ ]
        | FileFilter.Project ->
            [ "rs2dlc" ]
        | FileFilter.PSARC ->
            [ "psarc"]
        | FileFilter.ToolkitTemplate ->
            [ "dlc.xml" ]
        | FileFilter.ToneImport ->
            [ "tone2014.xml"; "tone2014.json"; "psarc" ]
        | FileFilter.ToneExport ->
            [ "tone2014.xml"; "tone2014.json" ]
        | FileFilter.WwiseConsoleApplication ->
            [ if OperatingSystem.IsWindows() then "exe" else "sh" ]

    let name =
        match filter with
        | FileFilter.WwiseConsoleApplication ->
            let ext = if OperatingSystem.IsWindows() then "exe" else "sh"
            $"WwiseConsole.{ext}"
        | other ->
            sprintf "%AFiles" other
            |> translate

    createFilters name extensions

/// Shows an open folder dialog.
let private openFolderDialog window title directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string>(fun () ->
            OpenFolderDialog(Title = title, Directory = Option.toObj directory)
                .ShowAsync window)

    return Option.ofString result
           |> Option.map msg }

/// Shows a save file dialog.
let private saveFileDialog window title filter initialFileName directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string>(fun () ->
            SaveFileDialog(Title = title,
                           Filters = createFileFilters filter,
                           InitialFileName = Option.toObj initialFileName,
                           Directory = Option.toObj directory)
                .ShowAsync window)

    return Option.ofString result
           |> Option.map msg }

let private createOpenFileDialog t f d m =
    OpenFileDialog(Title = t, Filters = createFileFilters f, Directory = Option.toObj d, AllowMultiple = m)

/// Shows an open file dialog for selecting a single file.
let private openFileDialog window title filter directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string[]>(fun () ->
            (createOpenFileDialog title filter directory false).ShowAsync window)

    return Option.ofObj result
           |> Option.bind Array.tryExactlyOne
           |> Option.map msg }

/// Shows an open file dialog that allows selecting multiple files.
let private openMultiFileDialog window title filters directory msg = async {
    let! result =
        Dispatcher.UIThread.InvokeAsync<string[]>(fun () ->
            (createOpenFileDialog title filters directory true).ShowAsync window)

    return
        Option.ofArray result
        |> Option.map msg }

let private translateTitle dialogType =
    let locString =
        match dialogType with
        | Dialog.PsarcImportTargetFolder _ -> "PsarcImportTargetFolderDialogTitle"
        | Dialog.AudioFile _ -> "AudioFileDialogTitle"
        | Dialog.ExportTone _ -> "ExportToneDialogTitle"
        | Dialog.PsarcPackTargetFile _ -> "PsarcPackTargetFileDialogTitle"
        | other -> $"{other}DialogTitle"

    translate locString

let private getProjectDirectory state =
    state.OpenProjectFile |> Option.map Path.GetDirectoryName

/// Shows the given dialog type.
let showDialog window dialogType state =
    let title = translateTitle dialogType

    let openMultiFileDialog = openMultiFileDialog window
    let openFileDialog = openFileDialog window
    let openFolderDialog = openFolderDialog window
    let saveFileDialog = saveFileDialog window

    // No initial directory
    let ofd filter msg = openFileDialog title filter None msg

    let dialog =
        match dialogType with
        | Dialog.OpenProject ->
            ofd FileFilter.Project OpenProject

        | Dialog.ToolkitImport ->
            if state.RunningTasks.Contains PsarcImport then
                async { return None }
            else
                ofd FileFilter.ToolkitTemplate ImportToolkitTemplate

        | Dialog.PsarcImport ->
            if state.RunningTasks.Contains PsarcImport then
                async { return None }
            else
                ofd FileFilter.PSARC (Dialog.PsarcImportTargetFolder >> ShowDialog)

        | Dialog.PsarcImportTargetFolder psarcPath ->
            openFolderDialog title None (fun folder -> ImportPsarc(psarcPath, folder))

        | Dialog.PsarcUnpack ->
            ofd FileFilter.PSARC (UnpackPSARC >> ToolsMsg)

        | Dialog.PsarcPackDirectory ->
            openFolderDialog title None (Dialog.PsarcPackTargetFile >> ShowDialog)

        | Dialog.PsarcPackTargetFile directory ->
            let initialFileName =
                directory.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                |> Array.last
                |> sprintf "%s.psarc"
                |> Some

            let initialDirectory = Directory.GetParent(directory).FullName |> Some

            saveFileDialog title FileFilter.PSARC initialFileName initialDirectory (fun targetFile -> PackDirectoryIntoPSARC(directory, targetFile) |> ToolsMsg)

        | Dialog.WemFiles ->
            openMultiFileDialog title FileFilter.Wem None (ConvertWemToOgg >> ToolsMsg)

        | Dialog.AudioFileConversion ->
            openMultiFileDialog title FileFilter.WavOgg None (ConvertAudioToWem >> ToolsMsg)

        | Dialog.RemoveDD ->
            openMultiFileDialog title FileFilter.XML None (RemoveDD >> ToolsMsg)

        | Dialog.TestFolder ->
            let initialDir = state.Config.TestFolderPath |> Option.ofString
            openFolderDialog title initialDir (SetTestFolderPath >> EditConfig)

        | Dialog.ProjectFolder ->
            openFolderDialog title None (SetProjectsFolderPath >> EditConfig)

        | Dialog.ProfileFile ->
            let initialDir = state.Config.ProfilePath |> Option.ofString |> Option.map Path.GetDirectoryName
            openFileDialog title FileFilter.Profile initialDir (SetProfilePath >> EditConfig)

        | Dialog.AddArrangements ->
            let initialDir = getProjectDirectory state
            openMultiFileDialog title FileFilter.XML initialDir AddArrangements

        | Dialog.ToneImport ->
            ofd FileFilter.ToneImport ImportTonesFromFile

        | Dialog.ToneInject ->
            openMultiFileDialog title FileFilter.ToneImport None (InjectTonesIntoProfile >> ToolsMsg)

        | Dialog.WwiseConsole ->
            let initialDir = state.Config.WwiseConsolePath |> Option.map Path.GetDirectoryName
            openFileDialog title FileFilter.WwiseConsoleApplication initialDir (SetWwiseConsolePath >> EditConfig)

        | Dialog.CoverArt ->
            let initialDir = getProjectDirectory state
            openFileDialog title FileFilter.Image initialDir (SetAlbumArt >> EditProject)

        | Dialog.AudioFile isCustom ->
            let msg =
                match isCustom with
                | true -> Some >> SetCustomAudioPath >> EditInstrumental
                | false -> SetAudioFile
            let initialDir = getProjectDirectory state
            openFileDialog title FileFilter.Audio initialDir msg

        | Dialog.CustomFont ->
            let initialDir = getProjectDirectory state
            openFileDialog title FileFilter.DDS initialDir (Some >> SetCustomFont >> EditVocals)

        | Dialog.ExportTone tone ->
            let initialFileName = Some $"{tone.Name}.tone2014.xml"
            saveFileDialog title FileFilter.ToneExport initialFileName None (fun path -> ExportTone(tone, path))

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

            saveFileDialog title FileFilter.Project initialFileName initialDir SaveProject

    Cmd.OfAsync.optionalResult dialog
