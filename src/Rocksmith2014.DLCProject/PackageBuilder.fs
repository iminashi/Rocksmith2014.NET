module Rocksmith2014.PackageBuilder

open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.Manifest
open Rocksmith2014.DLCProject.Manifest.AttributesCreation
open Rocksmith2014.XML
open Rocksmith2014.SNG
open Rocksmith2014.PSARC
open Rocksmith2014.Conversion
open Microsoft.Extensions.FileProviders
open System.IO
open System.Reflection
open System.Text
open System

let private generateShowLights (targetFile: string) =
    // TODO: Actually generate show lights
    let sl = ResizeArray<ShowLight>()
    sl.Add(ShowLight(10_000, 25uy))
    sl.Add(ShowLight(10_000, 42uy))
    ShowLights.Save(targetFile, sl)
    PSARC.Utils.getFileStreamForRead targetFile

let private build (platform: Platform) (targetFile: string) (sngs: (Arrangement * SNG) list) (project: DLCProject) = async {
    let key = project.DLCKey.ToLowerInvariant()
    let projectPath = Path.GetDirectoryName project.AudioFile.Path
    let partition = Partitioner.create project
    let getName arr =
        let name = partition arr |> snd
        sprintf "manifests/songs_dlc_%s/%s_%s.json" key key name
    let sngMap = sngs |> dict

    let! manifestEntries =
        project.Arrangements
        |> List.choose (function
            | Instrumental i as arr ->
                Some(getName arr, createAttributes project (FromInstrumental(i, sngMap.[arr])))
            | Vocals v as arr ->
                Some(getName arr, createAttributes project (FromVocals v))
            | Showlights _ -> None)
       |> List.map (fun m -> async {
           let data = MemoryStreamPool.Default.GetStream()
           do! m |> snd |> List.singleton |> Manifest.create |> Manifest.toJsonStream data
           return { Name = fst m; Data = data } })
       |> Async.Parallel

    let! headerEntry = async {
        let header =
            project.Arrangements
            |> List.choose (function
                | Instrumental i as arr ->
                    Some (createAttributesHeader project (FromInstrumental(i, sngMap.[arr])))
                | Vocals v ->
                    Some (createAttributesHeader project (FromVocals v))
                | Showlights _ -> None)
            |> Manifest.createHeader
        let data = MemoryStreamPool.Default.GetStream()
        do! Manifest.toJsonStream data header
        return { Name = sprintf "manifests/songs_dlc_%s/songs_dlc_%s.hsan" key key; Data = data } }

    let! sngEntries =
        sngs
        |> List.map (fun (arr, sng) -> async {
            let data = MemoryStreamPool.Default.GetStream()
            do! SNG.savePacked data platform sng
            let name =
                let part = partition arr |> snd
                sprintf "songs/bin/%s/%s_%s.sng" (Platform.getPath platform 1) key part
            return { Name = name; Data = data }
        })
        |> Async.Parallel

    let slEntry =
        let sl =
            project.Arrangements
            |> List.tryPick (function Showlights s -> Some s.XML | _ -> None)
        let data =
            match sl with
            | Some sl -> PSARC.Utils.getFileStreamForRead sl
            | None ->
                let slFile = Path.Combine(projectPath, "auto_showlights.xml")
                if File.Exists slFile then
                    PSARC.Utils.getFileStreamForRead slFile
                else
                    generateShowLights slFile
        { Name = sprintf "songs/arr/%s_showlights.xml" key
          Data = data }

    let fontEntry =
        let font =
            project.Arrangements
            |> List.tryPick (fun a ->
                match a with
                | Vocals { CustomFont = Some _ as font } -> font
                | _ -> None)
        match font with
        | None -> []
        | Some f -> [ { Name = sprintf "assets/ui/lyrics/%s/lyrics_%s.dds" key key
                        Data = PSARC.Utils.getFileStreamForRead f } ]

    let flatModelEntries =
        let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
        let songData = embeddedProvider.GetFileInfo("res/rsenumerable_song.flat").CreateReadStream()
        let rootData = embeddedProvider.GetFileInfo("res/rsenumerable_root.flat").CreateReadStream()
        [ { Name = "flatmodels/rs/rsenumerable_song.flat"; Data = songData }
          { Name = "flatmodels/rs/rsenumerable_root.flat"; Data = rootData } ]

    let xBlockEntry =
        let data = MemoryStreamPool.Default.GetStream()
        XBlock.create platform project
        |> XBlock.serialize data
        { Name = sprintf "gamexblocks/nsongs/%s.xblock" key; Data = data }

    let appIdEntry =
        let data = MemoryStreamPool.Default.GetStream()
        use writer = new StreamWriter(data, Encoding.ASCII, 7, true)
        writer.Write("221680")
        { Name = "appid.appid"; Data = data }

    let graphEntry =
        let data = MemoryStreamPool.Default.GetStream()
        AggregateGraph.create platform project
        |> AggregateGraph.serialize data
        { Name = sprintf "%s_aggregategraph.nt" key; Data = data }

    let audioEntries =
        let createEntries (audioFile: AudioFile) isPreview =
            let path =
                if audioFile.Path.EndsWith(".wem", StringComparison.OrdinalIgnoreCase) then
                    audioFile.Path
                else
                    Path.ChangeExtension(audioFile.Path, "wem")
            let bankData = MemoryStreamPool.Default.GetStream()
            let audio = PSARC.Utils.getFileStreamForRead path
            let bankName = if isPreview then project.DLCKey + "_Preview" else project.DLCKey
            let audioName = SoundBank.generate bankName audio bankData (float32 audioFile.Volume) isPreview platform
            [ { Name = sprintf "audio/%s/song_%s%s.bnk" (Platform.getPath platform 0) key (if isPreview then "_preview" else "")
                Data = bankData }
              { Name = sprintf "audio/%s/%s.wem" (Platform.getPath platform 0) audioName
                Data = audio } ]

        createEntries project.AudioFile false
        @
        createEntries project.AudioPreviewFile true

    let gfxEntries =
        [ { Name = sprintf "gfxassets/album_art/album_%s_64.dds" key
            Data = PSARC.Utils.getFileStreamForRead (Path.Combine(projectPath, "cover_64.dds")) }
          { Name = sprintf "gfxassets/album_art/album_%s_128.dds" key
            Data = PSARC.Utils.getFileStreamForRead (Path.Combine(projectPath, "cover_128.dds")) }
          { Name = sprintf "gfxassets/album_art/album_%s_256.dds" key
            Data = PSARC.Utils.getFileStreamForRead (Path.Combine(projectPath, "cover_256.dds")) }]

    use psarcFile =
        let fn = targetFile + (Platform.getPath platform 2) + ".psarc"
        File.Create(fn)

    do! PSARC.Create(psarcFile, true,
                    (fun entries ->
                        entries.AddRange manifestEntries
                        entries.AddRange sngEntries
                        entries.AddRange audioEntries
                        entries.AddRange gfxEntries
                        entries.Add headerEntry
                        entries.Add slEntry
                        entries.Add xBlockEntry
                        entries.Add graphEntry
                        entries.AddRange fontEntry
                        entries.AddRange flatModelEntries
                        entries.Add appIdEntry)
                    ) }

let buildPackages (targetFile: string) (platforms: Platform list) (project: DLCProject) = async {
    let key = project.DLCKey.ToLowerInvariant()

    DDS.createCoverArtImages (Path.GetDirectoryName project.AlbumArtFile) project.AlbumArtFile

    let sngs =
        project.Arrangements
        |> List.choose (fun arr ->
            match arr with
            | Instrumental i ->
                let sng =
                    InstrumentalArrangement.Load i.XML
                    |> ConvertInstrumental.xmlToSng
                Some(arr, sng)
            | Vocals v ->
                let customFont =
                    match v.CustomFont with
                    | Some f ->
                        let glyphs = 
                            (Path.GetFileNameWithoutExtension f + "_glyphs.xml")
                            |> GlyphDefinitions.Load
                        let assetPath =
                            sprintf "assets/ui/lyrics/%s/lyrics_%s.dds" key key
                        FontOption.CustomFont (glyphs, assetPath)
                    | None -> FontOption.DefaultFont
                let sng =
                    Vocals.Load v.XML
                    |> ConvertVocals.xmlToSng customFont
                Some(arr, sng)
            | Showlights _ -> None)

    do! platforms
        |> List.map (fun plat -> build plat targetFile sngs project)
        |> Async.Parallel
        |> Async.Ignore }
    