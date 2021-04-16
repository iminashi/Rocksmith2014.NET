module Rocksmith2014.DLCProject.PackageBuilder

open Rocksmith2014.Common
open Rocksmith2014.Common.Platform
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject.Manifest.AttributesCreation
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing
open Rocksmith2014.SNG
open Rocksmith2014.PSARC
open Rocksmith2014.Conversion
open Rocksmith2014.DD
open Microsoft.Extensions.FileProviders
open System.IO
open System.Reflection
open System.Text
open System

type private BuildData =
    { SNGs: (Arrangement * SNG) list
      CoverArtFiles: DisposableList<DDS.TempDDSFile>
      Author: string
      AppId: string
      Partition: Arrangement -> int * string
      AudioConversionTask: Async<unit> }

type BuildConfig =
    { Platforms: Platform list
      Author: string
      AppId: string
      GenerateDD: bool
      DDConfig: GeneratorConfig
      ApplyImprovements: bool
      SaveDebugFiles: bool
      AudioConversionTask: Async<unit>
      ProgressReporter : IProgress<float> option }

let private toDisposableList items = new DisposableList<_>(items)

let private build (buildData: BuildData) progress targetFile project platform = async {
    let readFile = Utils.getFileStreamForRead
    let partition = buildData.Partition
    let entry name data = { Name = name; Data = data }
    let key = project.DLCKey.ToLowerInvariant()
    let getManifestName arr =
        let name = partition arr |> snd
        $"manifests/songs_dlc_{key}/{key}_{name}.json" 
    let sngMap = buildData.SNGs |> readOnlyDict

    use! manifestEntries =
        let attributes arr conv = Some(getManifestName arr, createAttributes project conv)
        project.Arrangements
        |> List.choose (function
            | Instrumental i as arr -> FromInstrumental(i, sngMap.[arr]) |> attributes arr
            | Vocals v as arr -> FromVocals v |> attributes arr
            | Showlights _ -> None)
        |> List.map (fun (name, attr) -> async {
           let data = MemoryStreamPool.Default.GetStream()
           do! Manifest.create attr |> Manifest.toJsonStream data
           return entry name data })
        |> Async.Parallel
        |> Async.map (List.ofArray >> toDisposableList)

    use! headerEntry = async {
        let header = createAttributesHeader project >> Some
        let data = MemoryStreamPool.Default.GetStream()
        do! project.Arrangements
            |> List.choose (function
                | Instrumental i as arr -> FromInstrumental(i, sngMap.[arr]) |> header
                | Vocals v -> FromVocals v |> header
                | Showlights _ -> None)
            |> Manifest.createHeader
            |> Manifest.toJsonStream data
        return entry $"manifests/songs_dlc_{key}/songs_dlc_{key}.hsan" data }

    use! sngEntries =
        buildData.SNGs
        |> List.map (fun (arr, sng) -> async {
            let data = MemoryStreamPool.Default.GetStream()
            do! SNG.savePacked data platform sng
            let name =
                let part = partition arr |> snd
                let path = getPathPart platform Path.SNG
                $"songs/bin/{path}/{key}_{part}.sng"
            return entry name data })
        |> Async.Parallel
        |> Async.map (List.ofArray >> toDisposableList)

    progress()

    use showlightsEntry =
        let slFile = (List.pick Arrangement.pickShowlights project.Arrangements).XML
        entry $"songs/arr/{key}_showlights.xml" (readFile slFile)

    use fontEntry =
        project.Arrangements
        |> List.tryPick (function Vocals { CustomFont = Some _ as font } -> font | _ -> None)
        |> Option.map (fun f -> entry $"assets/ui/lyrics/{key}/lyrics_{key}.dds" (readFile f))
        |> (Option.toList >> toDisposableList)

    use flatModelEntries =
        let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
        let songData = embeddedProvider.GetFileInfo("res/rsenumerable_song.flat").CreateReadStream()
        let rootData = embeddedProvider.GetFileInfo("res/rsenumerable_root.flat").CreateReadStream()
        [ entry "flatmodels/rs/rsenumerable_song.flat" songData
          entry "flatmodels/rs/rsenumerable_root.flat" rootData ]
        |> toDisposableList

    use xBlockEntry =
        let data = MemoryStreamPool.Default.GetStream()
        XBlock.create platform project |> XBlock.serialize data
        entry $"gamexblocks/nsongs/{key}.xblock" data

    use appIdEntry =
        entry "appid.appid" (new MemoryStream(Encoding.ASCII.GetBytes buildData.AppId))

    use graphEntry =
        let data = MemoryStreamPool.Default.GetStream()
        AggregateGraph.create platform project
        |> AggregateGraph.serialize data
        entry $"{key}_aggregategraph.nt" data

    use gfxEntries =
        buildData.CoverArtFiles.Items
        |> List.map (fun dds ->
            entry $"gfxassets/album_art/album_{key}_{dds.Size}.dds" (readFile dds.FileName))
        |> toDisposableList

    use toolkitEntry =
        new MemoryStream(Encoding.UTF8.GetBytes($"Toolkit version: DLC Builder pre-release\nPackage Author: {buildData.Author}\nPackage Version: {project.Version}\nPackage Comment: Remastered"))
        |> entry "toolkit.version"

    progress()

    // Wait for the wem conversion to complete, if necessary
    do! buildData.AudioConversionTask

    let customAudio =
        project.Arrangements
        |> List.choose (function
            | Instrumental { CustomAudio = Some audioFile } as i ->
                Some (partition i |> snd, audioFile)
            | _ ->
                None) 

    use audioEntries =
        let createEntries (audioFile: AudioFile) bankName =
            let filePath = Path.ChangeExtension(audioFile.Path, "wem")
            let bankData = MemoryStreamPool.Default.GetStream()
            let audioData = readFile filePath
            let audioName = SoundBank.generate bankName audioData bankData (float32 audioFile.Volume) platform
            let path = getPathPart platform Path.Audio
            [ entry $"audio/{path}/song_{bankName.ToLowerInvariant()}.bnk" bankData
              entry $"audio/{path}/{audioName}.wem" audioData ]

        createEntries project.AudioFile project.DLCKey
        |> List.append (createEntries project.AudioPreviewFile $"{project.DLCKey}_Preview")
        |> List.append (customAudio |> List.collect (fun (name, audio) -> createEntries audio $"{project.DLCKey}_{name}"))
        |> toDisposableList

    let targetPath = sprintf "%s%s.psarc" targetFile (getPathPart platform Path.PackageSuffix)

    do! PSARC.Create(targetPath, true, [
        yield! sngEntries.Items
        yield showlightsEntry
        yield headerEntry
        yield! manifestEntries.Items
        yield! gfxEntries.Items
        yield xBlockEntry
        yield! flatModelEntries.Items
        yield graphEntry
        yield! audioEntries.Items
        yield! fontEntry.Items
        yield toolkitEntry
        yield appIdEntry ])

    progress() }

let private setupInstrumental part (inst: Instrumental) config =
    let xml = InstrumentalArrangement.Load inst.XML

    xml.MetaData.Part <- int16 part

    // Set up correct tone IDs
    xml.Tones.Changes.ForEach(fun tone -> tone.Id <- byte <| Array.IndexOf(xml.Tones.Names, tone.Name))
    
    // Copy the tuning in case it was edited
    Array.Copy(inst.Tuning, xml.MetaData.Tuning.Strings, 6)

    if xml.Version < 8uy then xml.FixHighDensity()

    if config.ApplyImprovements then
        ArrangementImprover.applyAll xml
    else
        EOFFixes.fixPhraseStartAnchors xml

    if xml.Levels.Count = 1 && config.GenerateDD then
        Generator.generateForArrangement config.DDConfig xml |> ignore

    if config.SaveDebugFiles then
        xml.Save(Path.ChangeExtension(inst.XML, "debug.xml"))

    xml

let private getFontOption (key: string) =
    Option.map (fun fontFile ->
        let glyphs = 
            Path.ChangeExtension(fontFile, ".glyphs.xml")
            |> GlyphDefinitions.Load
        FontOption.CustomFont (glyphs, $"assets/ui/lyrics/{key}/lyrics_{key}.dds"))
    >> Option.defaultValue FontOption.DefaultFont

/// Inserts an automatically generated show lights arrangement into the project.
let private addShowLights sngs project =
    let projectPath = Path.GetDirectoryName project.AudioFile.Path
    let xmlFile = Path.Combine(projectPath, "auto_showlights.xml")
    if not <| File.Exists xmlFile then ShowLightGenerator.generate xmlFile sngs

    { project with Arrangements = (Showlights { XML = xmlFile })::project.Arrangements }

let private createProgressReporter maximum =
    let mutable current = 0
    let lockObj = obj()

    fun () ->
        lock lockObj (fun () ->
            current <- current + 1
            float current / float maximum * 100.)

/// Builds packages for the given platforms.
let buildPackages (targetFile: string) (config: BuildConfig) (project: DLCProject) = async {
    let! audioConversionTask = config.AudioConversionTask |> Async.StartChild
    let key = project.DLCKey.ToLowerInvariant()
    use coverArtFiles = DDS.createCoverArtImages project.AlbumArtFile |> toDisposableList
    let partition = Partitioner.create project

    let progress =
        match config.ProgressReporter with
        | Some progress ->
            let maximumProgress = 3 * config.Platforms.Length + 1
            createProgressReporter maximumProgress >> progress.Report
        | None ->
            ignore

    let sngs =
        project.Arrangements
        |> List.choose (fun arr ->
            match arr with
            | Instrumental inst ->
                let part = partition arr |> fst
                let sng =
                    setupInstrumental part inst config
                    |> ConvertInstrumental.xmlToSng
                Some(arr, sng)
            | Vocals v ->
                let sng =
                    Vocals.Load v.XML
                    |> ConvertVocals.xmlToSng (getFontOption key v.CustomFont)
                Some(arr, sng)
            | Showlights _ -> None)

    progress()

    // Check if a show lights arrangement is included
    let project =
        if project.Arrangements |> List.exists (Arrangement.pickShowlights >> Option.isSome) then
            project
        else
            addShowLights sngs project

    let data =
        { SNGs = sngs
          CoverArtFiles = coverArtFiles
          Author = config.Author
          AppId = config.AppId
          Partition = partition
          AudioConversionTask = audioConversionTask }

    do! config.Platforms
        |> List.map (build data progress targetFile project)
        |> Async.Parallel
        |> Async.Ignore }
