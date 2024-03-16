module DLCBuilder.Dialogs

open Avalonia.Controls
open Avalonia.Platform.Storage
open Avalonia.Threading
open Elmish
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
    | WavOggFlac
    | ToolkitTemplate
    | ToneImport
    | ToneExport
    | WwiseConsoleApplication
    | Executable

let private createFilters (name: string) (extensions: string list) =
    // TODO
    let mimeType = "application/octet-stream"
    FilePickerFileType(name, Patterns = extensions, MimeTypes = [| mimeType |])
    |> Array.singleton

let private wwiseConsoleExtension =
    PlatformSpecific.Value(mac = "*.sh", windows = "*.exe", linux = "*.exe")

let private createFileFilters filter =
    let extensions =
        match filter with
        | FileFilter.WavOggFlac ->
            [ "*.wav"; "*.ogg"; "*.flac" ]
        | FileFilter.Audio ->
            [ "*.wav"; "*.ogg"; "*.wem"; "*.flac" ]
        | FileFilter.Wem ->
            [ "*.wem" ]
        | FileFilter.XML ->
            [ "*.xml" ]
        | FileFilter.Image ->
            [ "*.png"; "*.jpg"; "*.jpeg"; "*.dds" ]
        | FileFilter.DDS ->
            [ "*.dds" ]
        | FileFilter.Profile ->
            [ "*.*" ]
        | FileFilter.Project ->
            [ "*.rs2dlc" ]
        | FileFilter.PSARC ->
            [ "*.psarc" ]
        | FileFilter.ToolkitTemplate ->
            [ "*.dlc.xml" ]
        | FileFilter.ToneImport ->
            [ "*.tone2014.xml"; "*.tone2014.json"; "*.psarc" ]
        | FileFilter.ToneExport ->
            [ "*.tone2014.xml"; "*.tone2014.json" ]
        | FileFilter.WwiseConsoleApplication ->
            [ wwiseConsoleExtension ]
        | FileFilter.Executable ->
            [ "*.exe" ] // TODO: Windows-only

    let name =
        match filter with
        | FileFilter.WwiseConsoleApplication ->
            $"WwiseConsole.{wwiseConsoleExtension}"
        | other ->
            sprintf "%AFiles" other
            |> translate

    createFilters name extensions

let private getInitialDirectoryString (window: Window) (pathOpt: string option) =
    task {
        match pathOpt with
        | Some path ->
            return! window.StorageProvider.TryGetFolderFromPathAsync(path)
        | None ->
            return null
    }

/// Shows an open folder dialog.
let private openFolderDialog (window: Window) (title: string) (directory: string option) msg =
    task {
        let! initialDirectory = getInitialDirectoryString window directory

        let! result =
            Dispatcher.UIThread.InvokeAsync<IReadOnlyList<_>>(fun () ->
                let options = FolderPickerOpenOptions(
                    Title = title,
                    SuggestedStartLocation = initialDirectory,
                    AllowMultiple = false
                )

                window.StorageProvider.OpenFolderPickerAsync(options)
            )

        if result.Count = 1 then
            return Some (msg (result[0].TryGetLocalPath()))
        else
            return None
    }

/// Shows a save file dialog.
let private saveFileDialog (window: Window) (title: string) (filter: FileFilter) (initialFileName: string option) (directory: string option) msg =
    task {
        let! initialDirectory = getInitialDirectoryString window directory

        let! result =
            Dispatcher.UIThread.InvokeAsync<IStorageFile>(fun () ->
                let options = FilePickerSaveOptions(
                    Title = title,
                    FileTypeChoices = createFileFilters filter,
                    SuggestedFileName = Option.toObj initialFileName,
                    SuggestedStartLocation = initialDirectory
                )

                window.StorageProvider.SaveFilePickerAsync(options)
            )

        return Option.ofObj result |> Option.map (fun x -> x.TryGetLocalPath() |> msg)
    }

let private createOpenFileDialogOptions win title filter dir multi =
    task {
        let! initialDirectory = getInitialDirectoryString win dir
        return FilePickerOpenOptions(
            Title = title,
            FileTypeFilter = createFileFilters filter,
            SuggestedStartLocation = initialDirectory,
            AllowMultiple = multi)
    }

/// Shows an open file dialog for selecting a single file.
let private openFileDialog (window: Window) (title: string) (filter: FileFilter) (directory: string option) msg =
    task {
        let! options = createOpenFileDialogOptions window title filter directory false

        let! result =
            Dispatcher.UIThread.InvokeAsync<IReadOnlyList<_>>(fun () -> window.StorageProvider.OpenFilePickerAsync(options))

        if result.Count = 1 then
            return Some (msg (result[0].TryGetLocalPath()))
        else
            return None
    }

/// Shows an open file dialog that allows selecting multiple files.
let private openMultiFileDialog (window: Window) (title: string) (filter: FileFilter) (directory: string option) msg =
    task {
        let! options = createOpenFileDialogOptions window title filter directory true

        let! result =
            Dispatcher.UIThread.InvokeAsync<IReadOnlyList<_>>(fun () -> window.StorageProvider.OpenFilePickerAsync(options))

        if result.Count = 0 then
            return None
        else
            return
                result
                |> Seq.map (fun x -> x.TryGetLocalPath())
                |> Seq.toArray
                |> msg
                |> Some
    }

let private translateTitle dialogType =
    let locString =
        match dialogType with
        | Dialog.PsarcImportTargetFolder _ -> "PsarcImportTargetFolderDialogTitle"
        | Dialog.PsarcUnpackTargetFolder _ -> "PsarcUnpackTargetFolderDialogTitle"
        | Dialog.AudioFile _ -> "AudioFileDialogTitle"
        | Dialog.ExportTone _ -> "ExportToneDialogTitle"
        | Dialog.PsarcPackTargetFile _ -> "PsarcPackTargetFileDialogTitle"
        | other -> $"{other}DialogTitle"

    translate locString

/// Shows the given dialog type.
let showDialog window dialogType state =
    let title = translateTitle dialogType

    let openMultiFileDialog = openMultiFileDialog window
    let openFileDialog = openFileDialog window
    let openFolderDialog = openFolderDialog window
    let saveFileDialog = saveFileDialog window

    // No initial directory
    let ofd filter msg = openFileDialog title filter None msg

    let projectDirectory = state.OpenProjectFile |> Option.map Path.GetDirectoryName

    let dialog =
        match dialogType with
        | Dialog.SaveJapaneseLyrics ->
            let msg = JapaneseLyricsCreator.SaveLyricsToFile >> LyricsCreatorMsg
            saveFileDialog title FileFilter.XML (Some "PART JVOCALS_RS2.xml") projectDirectory msg

        | Dialog.OpenProject ->
            ofd FileFilter.Project OpenProject

        | Dialog.ToolkitImport ->
            if state.RunningTasks.Contains(PsarcImport) then
                task { return None }
            else
                ofd FileFilter.ToolkitTemplate ImportToolkitTemplate

        | Dialog.PsarcImportQuick ->
            if state.RunningTasks.Contains(PsarcImport) then
                task { return None }
            else
                ofd FileFilter.PSARC ImportPsarcQuick

        | Dialog.PsarcImport ->
            if state.RunningTasks.Contains(PsarcImport) then
                task { return None }
            else
                ofd FileFilter.PSARC (Dialog.PsarcImportTargetFolder >> ShowDialog)

        | Dialog.PsarcImportTargetFolder psarcPath ->
            let initialDir = Path.GetDirectoryName(psarcPath) |> Some
            openFolderDialog title initialDir (fun folder -> ImportPsarc(psarcPath, folder))

        | Dialog.PsarcUnpack ->
            if state.RunningTasks.Contains(PsarcUnpack) then
                task { return None }
            else
                openMultiFileDialog title FileFilter.PSARC None (Dialog.PsarcUnpackTargetFolder >> ShowDialog)

        | Dialog.PsarcUnpackTargetFolder psarcPaths ->
            let initialDir =
                psarcPaths
                |> Array.tryHead
                |> Option.map Path.GetDirectoryName

            openFolderDialog title initialDir (fun folder -> UnpackPSARC(psarcPaths, folder) |> ToolsMsg)

        | Dialog.PsarcPackDirectory ->
            openFolderDialog title None (Dialog.PsarcPackTargetFile >> ShowDialog)

        | Dialog.PsarcPackTargetFile directory ->
            let initialFileName =
                directory.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                |> Array.last
                |> sprintf "%s.psarc"
                |> Some

            let initialDirectory = Directory.GetParent(directory).FullName |> Some
            let msg targetFile = PackDirectoryIntoPSARC(directory, targetFile) |> ToolsMsg

            saveFileDialog title FileFilter.PSARC initialFileName initialDirectory msg

        | Dialog.WemFiles ->
            openMultiFileDialog title FileFilter.Wem None (ConvertWemToOgg >> ToolsMsg)

        | Dialog.AudioFileConversion ->
            openMultiFileDialog title FileFilter.WavOggFlac None (ConvertAudioToWem >> ToolsMsg)

        | Dialog.RemoveDD ->
            openMultiFileDialog title FileFilter.XML None (RemoveDD >> ToolsMsg)

        | Dialog.TestFolder ->
            let initialDir = state.Config.TestFolderPath |> Option.ofString
            openFolderDialog title initialDir (SetTestFolderPath >> EditConfig)

        | Dialog.DlcFolder ->
            let initialDir = state.Config.DlcFolderPath |> Option.ofString
            openFolderDialog title initialDir (SetDlcFolderPath >> EditConfig)

        | Dialog.ProfileFile ->
            let initialDir =
                state.Config.ProfilePath
                |> Option.ofString
                |> Option.map Path.GetDirectoryName

            openFileDialog title FileFilter.Profile initialDir (SetProfilePath >> EditConfig)

        | Dialog.FontGeneratorPath ->
            openFileDialog title FileFilter.Executable None (SetFontGeneratorPath >> EditConfig)

        | Dialog.AddArrangements ->
            openMultiFileDialog title FileFilter.XML projectDirectory AddArrangements

        | Dialog.ToneImport ->
            ofd FileFilter.ToneImport ImportTonesFromFile

        | Dialog.ToneInject ->
            openMultiFileDialog title FileFilter.ToneImport None (InjectTonesIntoProfile >> ToolsMsg)

        | Dialog.WwiseConsole ->
            let initialDir = state.Config.WwiseConsolePath |> Option.map Path.GetDirectoryName
            openFileDialog title FileFilter.WwiseConsoleApplication initialDir (SetWwiseConsolePath >> EditConfig)

        | Dialog.CoverArt ->
            openFileDialog title FileFilter.Image projectDirectory (SetAlbumArt >> EditProject)

        | Dialog.AudioFile isCustom ->
            let msg =
                match isCustom with
                | true -> Some >> SetCustomAudioPath >> EditInstrumental
                | false -> SetAudioFile

            openFileDialog title FileFilter.Audio projectDirectory msg

        | Dialog.PreviewFile ->
            openFileDialog title FileFilter.Audio projectDirectory SetPreviewAudioFile

        | Dialog.CustomFont ->
            openFileDialog title FileFilter.DDS projectDirectory (Some >> SetCustomFont >> EditVocals)

        | Dialog.ExportTone tone ->
            let initialFileName = Some $"{tone.Name}.tone2014.xml"
            saveFileDialog title FileFilter.ToneExport initialFileName None (fun path -> ExportTone(tone, path))

        | Dialog.SaveProjectAs ->
            let project = state.Project
            let openProjectPath = state.OpenProjectFile
            let initialFileName =
                openProjectPath
                |> Option.map Path.GetFileName
                |> Option.orElseWith (fun () -> StateUtils.createProjectFilename project |> Some)

            let initialDir =
                openProjectPath
                |> Option.orElse (project.Arrangements |> List.tryHead |> Option.map Arrangement.getFile)
                |> Option.orElse (project.AudioFile.Path |> Option.ofString)
                |> Option.orElse (project.AlbumArtFile |> Option.ofString)
                |> Option.map Path.GetDirectoryName

            saveFileDialog title FileFilter.Project initialFileName initialDir SaveProject

    Cmd.OfAsync.optionalResult (dialog |> Async.AwaitTask)
