module DLCBuilder.Utils

open Pfim
open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
open Avalonia
open Avalonia.Media.Imaging
open Avalonia.Platform
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Rocksmith2014.PSARC
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

/// Converts a Pfim DDS bitmap into an Avalonia bitmap.
let private avaloniaBitmapFromDDS (fileName: string) =
    use image = Pfim.FromFile fileName
    let pxFormat, data, stride =
        match image.Format with
        | ImageFormat.R5g6b5 ->
            PixelFormat.Rgb565, image.Data, image.Stride
        | ImageFormat.Rgb24 ->
            let pixels = image.DataLen / 3
            let newDataLen = pixels * 4
            let newData = Array.zeroCreate<byte> newDataLen
            for i = 0 to pixels - 1 do
                newData.[i * 4] <- image.Data.[i * 3]
                newData.[i * 4 + 1] <- image.Data.[i * 3 + 1]
                newData.[i * 4 + 2] <- image.Data.[i * 3 + 2]
                newData.[i * 4 + 3] <- 255uy

            let stride = image.Width * 4
            PixelFormat.Bgra8888, newData, stride
        | _ ->
            PixelFormat.Bgra8888, image.Data, image.Stride
    let pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned)
    let addr = pinnedArray.AddrOfPinnedObject()
    let bm = new Bitmap(pxFormat, AlphaFormat.Unpremul, addr, PixelSize(image.Width, image.Height), Vector(96., 96.), stride)
    pinnedArray.Free()
    bm

/// Loads a bitmap from the given path.
let loadBitmap (path: string) =
    match path with
    | EndsWith "dds" ->
        avaloniaBitmapFromDDS path
    | _ ->
        new Bitmap(path)

/// Disposes the old cover art and loads a new one from the given path.
let changeCoverArt (coverArt: Bitmap option) newPath =
    coverArt |> Option.iter (fun old -> old.Dispose())
    File.tryMap loadBitmap newPath

/// Imports tones from a PSARC file.
let importTonesFromPSARC (psarcPath: string) = async {
    use psarc = PSARC.ReadFile psarcPath
    let! jsons =
        psarc.Manifest
        |> Seq.filter (String.endsWith "json")
        |> Seq.map psarc.GetEntryStream
        |> Async.Sequential

    let! manifests =
        jsons
        |> Array.map (fun data -> async {
            try
                try
                    let! manifest = Manifest.fromJsonStream data
                    return Some (Manifest.getSingletonAttributes manifest)
                finally
                    data.Dispose()
            with _ ->
                return None })
        |> Async.Parallel

    return
        manifests
        |> Array.choose (Option.bind (fun a -> Option.ofObj a.Tones))
        |> Array.concat
        |> Array.distinctBy (fun x -> x.Key)
        |> Array.map Tone.fromDto }

/// Creates the path for the preview audio from the main audio path.
let previewPathFromMainAudio (audioPath: string) =
    let dir = Path.GetDirectoryName audioPath
    let fn = Path.GetFileNameWithoutExtension audioPath
    let ext = Path.GetExtension audioPath
    Path.Combine(dir, $"{fn}_preview{ext}")

let checkArrangement arrangement =
    match arrangement with
    | Instrumental inst ->
        InstrumentalArrangement.Load inst.XML
        |> ArrangementChecker.runAllChecks
    | Vocals { CustomFont = font; XML = xml } ->
        Vocals.Load xml
        |> ArrangementChecker.checkVocals font.IsSome
    | Showlights sl ->
        ShowLights.Load sl.XML
        |> ArrangementChecker.checkShowlights
        |> Option.toList

/// Checks the project's arrangements for issues.
let checkArrangements (project: DLCProject) (progress: IProgress<float>) =
    let length = float project.Arrangements.Length

    project.Arrangements
    |> List.mapi (fun i arr ->
        let result = checkArrangement arr
        progress.Report(float (i + 1) / length * 100.)
        Arrangement.getFile arr ,result)
    |> Map.ofList

/// Adds descriptors to tones that have none.
let addDescriptors (tone: Tone) =
    let descs =
        tone.ToneDescriptors
        |> Option.ofArray
        |> Option.defaultWith (fun () ->
            ToneDescriptor.getDescriptionsOrDefault tone.Name
            |> Array.map (fun x -> x.UIName))

    { tone with ToneDescriptors = descs; SortOrder = None; NameSeparator = " - " }

/// Converts the project's audio and preview audio files to wem.
let convertAudio cliPath project =
    [| project.AudioFile.Path; project.AudioPreviewFile.Path |]
    |> Array.map (Wwise.convertToWem cliPath)
    |> Async.Parallel
    |> Async.Ignore

/// Removes the item at the index from the array and shifts the subsequent items towards index zero by one.
let removeAndShift (index: int) array =
    let arr = Array.copy array
    for i = index to arr.Length - 2 do
        arr.[i] <- arr.[i + 1]
    arr.[arr.Length - 1] <- None
    arr

/// Adds the path's default tone for arrangements whose base tones have no definitions.
let addDefaultTonesIfNeeded (project: DLCProject) =
    let neededTones =
        project.Arrangements
        |> List.choose (function
            | Instrumental i when not (project.Tones |> List.exists (fun t -> t.Key = i.BaseTone)) ->
                Some (i.BaseTone, i.RouteMask)
            | _ ->
                None)
        |> List.distinctBy fst
        |> List.map (fun (key, routeMask) ->
            let tone =
                match routeMask with
                | RouteMask.Lead -> DefaultTones.Lead.Value
                | RouteMask.Bass -> DefaultTones.Bass.Value
                | _ -> DefaultTones.Rhythm.Value
            { tone with Key = key })

    { project with Tones = neededTones @ project.Tones }

/// Adds metadata to the project if the metadata option is Some.
let addMetadata (md: MetaData option) charterName project =
    match md with
    | Some md ->
        { project with
            DLCKey = DLCKey.create charterName md.ArtistName md.Title
            ArtistName = SortableString.Create md.ArtistName // Ignore the sort value from the XML
            Title = SortableString.Create(md.Title, md.TitleSort)
            AlbumName = SortableString.Create(md.AlbumName, md.AlbumNameSort)
            Year = md.AlbumYear }
    | None ->
        project

/// Starts the given path or URL using the operating system shell.
let openWithShell pathOrUrl =
    ProcessStartInfo(pathOrUrl, UseShellExecute = true)
    |> Process.Start
    |> ignore

/// Removes the item at the given index from the list.
/// Returns an index that is within the new list or -1 if it is empty.
let removeSelected initialList index =
    let list = List.removeAt index initialList
    let newSelectedIndex = min index (list.Length - 1)
    list, newSelectedIndex

/// Removes DD from the arrangements in the project.
let removeDD project =
    project.Arrangements
    |> List.choose Arrangement.pickInstrumental
    |> List.map (fun inst -> async {
        let arr = InstrumentalArrangement.Load inst.XML
        do! arr.RemoveDD false
        arr.Save inst.XML })
    |> Async.Sequential
    |> Async.Ignore

module FocusHelper =
    open Avalonia.Controls
    open Avalonia.Input

    let private window =
        lazy (Application.Current.ApplicationLifetime :?> ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime).MainWindow

    let mutable elementThatHadFocus : IInputElement = null

    let storeFocusedElement () =
        elementThatHadFocus <- FocusManager.Instance.Current
        // Blur the focus from the element
        window.Value.Focus()

    let restoreFocus () =
        if notNull elementThatHadFocus then 
            elementThatHadFocus.Focus()
