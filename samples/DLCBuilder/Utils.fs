module DLCBuilder.Utils

open Pfim
open System
open System.Diagnostics
open System.IO
open System.Runtime.InteropServices
open Avalonia.Platform
open Avalonia.Media.Imaging
open Avalonia
open Rocksmith2014.PSARC
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DD
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.PackageBuilder
open Rocksmith2014.XML.Processing
open Rocksmith2014.XML
open Rocksmith2014.Audio

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

/// Checks the project's arrangements for issues.
let checkArrangements (project: DLCProject) (progress: IProgress<float>) =
    let length = float project.Arrangements.Length

    project.Arrangements
    |> List.mapi (fun i arr ->
        let result =
            match arr with
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

/// Adds the given tones into the project.
let addTones (state: State) (tones: Tone list) =
    let tones = List.map addDescriptors tones
    { state with Project = { state.Project with Tones = tones @ state.Project.Tones }
                 Overlay = NoOverlay }

let [<Literal>] private CherubRock = "248750"
let packageBuildProgress = Progress<float>()

/// Creates a build configuration data structure.
let createBuildConfig buildType config project platforms =
    let convTask =
        DLCProject.getFilesThatNeedConverting project
        |> Seq.map (Wwise.convertToWem config.WwiseConsolePath)
        |> Async.Parallel
        |> Async.Ignore

    let phraseSearch =
        match config.DDPhraseSearchEnabled with
        | true -> WithThreshold config.DDPhraseSearchThreshold
        | false -> SearchDisabled

    let appId =
        match buildType, config.CustomAppId with
        | Test, Some customId -> customId
        | _ -> CherubRock

    { Platforms = platforms
      BuilderVersion = $"DLC Builder {AppVersion.versionString}"
      Author = config.CharterName
      AppId = appId
      GenerateDD = config.GenerateDD || buildType = Release
      DDConfig = { PhraseSearch = phraseSearch; LevelCountGeneration = config.DDLevelCountGeneration }
      ApplyImprovements = config.ApplyImprovements
      SaveDebugFiles = config.SaveDebugFiles && buildType <> Release
      AudioConversionTask = convTask
      ProgressReporter = Some (packageBuildProgress :> IProgress<float>) }

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

/// Returns true if a build or a wem conversion is not in progress.
let notBuilding state =
    state.RunningTasks
    |> Set.intersect (Set([ BuildPackage; WemConversion ]))
    |> Set.isEmpty

/// Returns true if the project can be built.
let canBuild state =
    notBuilding state
    && (not <| state.RunningTasks.Contains PsarcImport)
    && state.Project.Arrangements.Length > 0
    && String.notEmpty state.Project.AudioFile.Path

/// Returns true for tasks that report progress.
let taskHasProgress = function
    | BuildPackage | PsarcImport | PsarcUnpack | ArrangementCheck ->
        true
    | _ ->
        false

/// Adds a new long running task to the state.
let addTask newTask state =
    let message =
        match taskHasProgress newTask with
        | true -> TaskWithProgress(newTask, 0.)
        | false -> TaskWithoutProgress(newTask)

    { state with RunningTasks = state.RunningTasks |> Set.add newTask
                 StatusMessages = message::state.StatusMessages }

/// Removes the completed task from the state.
let removeTask completedTask state =
    let messages =
        state.StatusMessages
        |> List.filter (function
            | TaskWithProgress (task, _)
            | TaskWithoutProgress task when task = completedTask ->
                false
            | _ ->
                true)

    { state with RunningTasks = state.RunningTasks |> Set.remove completedTask
                 StatusMessages = messages }

/// Returns a throttled auto save message.
let autoSave =
    let mutable id = 0L

    fun () ->
        id <- id + 1L
        let thisId = id
        async {
            do! Async.Sleep 1000
            if thisId = id then
                return Some AutoSaveProject
            else
                return None }

/// Starts the given URL using the operating system shell.
let openLink url =
    ProcessStartInfo(url, UseShellExecute = true)
    |> Process.Start
    |> ignore

/// Returns true if the arrangement validation may be executed.
let canRunValidation state =
    state.Project.Arrangements.Length > 0
    &&
    not (state.RunningTasks |> Set.contains ArrangementCheck)
