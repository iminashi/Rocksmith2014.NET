module Rocksmith2014.DLCProject.PackageBuilder

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject.Manifest.AttributesCreation
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing
open Rocksmith2014.SNG
open Rocksmith2014.PSARC
open Rocksmith2014.Conversion
open Microsoft.Extensions.FileProviders
open System.IO
open System.Reflection
open System.Text
open System

type private BuildData =
    { SNGs: (Arrangement * SNG) list
      CoverArtFiles: (int * string) list
      Author: string
      AppId: string
      Partition: Arrangement -> int * string
      AudioConversionTask: Async<unit> }

type BuildConfig =
    { Platforms: Platform list
      Author: string
      AppId: string
      ApplyImprovements: bool
      AudioConversionTask: Async<unit> }

let private toDisposableList items = new DisposableList<NamedEntry>(items)

let private build (buildData: BuildData) targetFile project platform = async {
    let readFile = Utils.getFileStreamForRead
    let partition = buildData.Partition
    let entry name data = { Name = name; Data = data }
    let key = project.DLCKey.ToLowerInvariant()
    let getManifestName arr =
        let name = partition arr |> snd
        $"manifests/songs_dlc_{key}/{key}_{name}.json" 
    let sngMap = buildData.SNGs |> dict

    use! manifestEntries = async {
        let! entries =
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
        return toDisposableList (List.ofArray entries) }

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

    use! sngEntries = async {
        let! entries =
            buildData.SNGs
            |> List.map (fun (arr, sng) -> async {
                let data = MemoryStreamPool.Default.GetStream()
                do! SNG.savePacked data platform sng
                let name =
                    let part = partition arr |> snd
                    let path = Platform.getPath platform Platform.Path.SNG
                    $"songs/bin/{path}/{key}_{part}.sng"
                return entry name data })
            |> Async.Parallel
        return toDisposableList (List.ofArray entries) }

    use showlightsEntry =
        let slFile = (List.pick Arrangement.pickShowlights project.Arrangements).XML
        entry $"songs/arr/{key}_showlights.xml" (readFile slFile)

    use fontEntry =
        project.Arrangements
        |> List.tryPick (function Vocals { CustomFont = Some _ as font } -> font | _ -> None)
        |> Option.map (fun f -> entry $"assets/ui/lyrics/{key}/lyrics_{key}.dds" (readFile f))
        |> Option.toList
        |> toDisposableList

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
        buildData.CoverArtFiles
        |> List.map (fun (size, file) ->
            entry $"gfxassets/album_art/album_{key}_{size}.dds" (readFile file))
        |> toDisposableList

    use toolkitEntry =
        new MemoryStream(Encoding.UTF8.GetBytes($"Toolkit version: 9.9.9.9\nPackage Author: {buildData.Author}\nPackage Version: {project.Version}\nPackage Comment: Remastered"))
        |> entry "toolkit.version"

    // Wait for the wem conversion to complete, if necessary
    do! buildData.AudioConversionTask

    use audioEntries =
        let createEntries (audioFile: AudioFile) isPreview =
            let filePath =
                if String.endsWith ".wem" audioFile.Path then
                    audioFile.Path
                else
                    Path.ChangeExtension(audioFile.Path, "wem")
            let bankData = MemoryStreamPool.Default.GetStream()
            let audio = readFile filePath
            let bankName = if isPreview then project.DLCKey + "_Preview" else project.DLCKey
            let audioName = SoundBank.generate bankName audio bankData (float32 audioFile.Volume) isPreview platform
            let path = Platform.getPath platform Platform.Path.Audio
            let suffix = if isPreview then "_preview" else ""
            [ entry $"audio/{path}/song_{key}{suffix}.bnk" bankData
              entry $"audio/{path}/{audioName}.wem" audio ]

        createEntries project.AudioFile false
        |> List.append (createEntries project.AudioPreviewFile true)
        |> toDisposableList

    let targetPath = sprintf "%s%s.psarc" targetFile (Platform.getPath platform Platform.Path.PackageSuffix)

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
        yield appIdEntry ]) }

let private setupInstrumental part (inst: Instrumental) improve (xml: InstrumentalArrangement) =
    xml.MetaData.Part <- int16 part

    // Set up correct tone IDs
    for i = 0 to xml.Tones.Changes.Count - 1 do
        xml.Tones.Changes.[i].Id <- byte <| Array.IndexOf(xml.Tones.Names, xml.Tones.Changes.[i].Name)
    
    // Copy the tuning in case it was edited
    Array.Copy(inst.Tuning, xml.MetaData.Tuning.Strings, 6)

    if xml.Version < 8uy then xml.FixHighDensity()

    if improve then ArrangementImprover.applyAll xml

    // TODO: Generate DD levels

    xml

/// Builds packages for the given platforms.
let buildPackages (targetFile: string) (config: BuildConfig) (project: DLCProject) = async {
    let! audioConversionTask = config.AudioConversionTask |> Async.StartChild
    let key = project.DLCKey.ToLowerInvariant()
    let coverArt = DDS.createCoverArtImages project.AlbumArtFile
    let partition = Partitioner.create project

    try
        let sngs =
            project.Arrangements
            |> List.choose (fun arr ->
                match arr with
                | Instrumental inst ->
                    let part = partition arr |> fst
                    let sng =
                        InstrumentalArrangement.Load inst.XML
                        |> setupInstrumental part inst config.ApplyImprovements
                        |> ConvertInstrumental.xmlToSng
                    Some(arr, sng)
                | Vocals v ->
                    let customFont =
                        match v.CustomFont with
                        | Some f ->
                            let glyphs = 
                                Path.ChangeExtension(f, ".glyphs.xml")
                                |> GlyphDefinitions.Load
                            let assetPath = $"assets/ui/lyrics/{key}/lyrics_{key}.dds"
                            FontOption.CustomFont (glyphs, assetPath)
                        | None -> FontOption.DefaultFont
                    let sng =
                        Vocals.Load v.XML
                        |> ConvertVocals.xmlToSng customFont
                    Some(arr, sng)
                | Showlights _ -> None)

        // Check if a show lights arrangement is included
        let project =
            if project.Arrangements |> List.exists (Arrangement.pickShowlights >> Option.isSome) then
                project
            else
                // Insert an automatically generated show lights arrangement
                let projectPath = Path.GetDirectoryName project.AudioFile.Path
                let slFile = Path.Combine(projectPath, "auto_showlights.xml")
                let sl = Showlights { XML = slFile }
                if not <| File.Exists slFile then
                    ShowLightGenerator.generate slFile sngs
                let arrangements = sl::project.Arrangements
                { project with Arrangements = arrangements }

        let data =
            { SNGs = sngs
              CoverArtFiles = coverArt
              Author = config.Author
              AppId = config.AppId
              Partition = partition
              AudioConversionTask = audioConversionTask }

        do! config.Platforms
            |> List.map (build data targetFile project)
            |> Async.Parallel
            |> Async.Ignore

    finally coverArt |> List.iter (snd >> File.Delete) }
    