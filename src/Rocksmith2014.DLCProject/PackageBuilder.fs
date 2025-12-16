module Rocksmith2014.DLCProject.PackageBuilder

open System
open System.IO
open System.Reflection
open System.Text
open Microsoft.Extensions.FileProviders
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.Common.Platform
open Rocksmith2014.Conversion
open Rocksmith2014.DD
open Rocksmith2014.DLCProject.Manifest.AttributesCreation
open Rocksmith2014.PSARC
open Rocksmith2014.SNG
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

type private BuildData =
    { SNGs: (Arrangement * SNG) list
      CoverArtFiles: DisposableList<DDS.DDSFile>
      BuilderVersion: string
      Author: string
      AppId: AppId
      AudioConversionTask: Async<unit> }

type IdResetConfig =
    { ProjectDirectory: string
      ConfirmIdRegeneration: ArrangementId list -> Async<bool>
      PostNewIds: Map<ArrangementId, Arrangement> -> unit }

type TargetPathType =
    | WithoutPlatformOrExtension of string
    | WithPlatformAndExtension of string

type BuildConfig =
    { Platforms: Platform list
      BuilderVersion: string
      Author: string
      AppId: AppId
      GenerateDD: bool
      ForcePhraseCreation: bool
      DDConfig: GeneratorConfig
      ApplyImprovements: bool
      SaveDebugFiles: bool
      AudioConversionTask: Async<unit>
      IdResetConfig: IdResetConfig option
      ProgressReporter: IProgress<float> option }

let private getResource (provider: EmbeddedFileProvider) (name: string) =
    provider.GetFileInfo(name).CreateReadStream()

let private toDisposableList items = new DisposableList<_>(items)

let private build (buildData: BuildData) (progress: unit -> unit) (targetPath: TargetPathType) (project: DLCProject) (platform: Platform) = async {
    let readFile = Utils.getFileStreamForRead
    let partition = Partitioner.create project
    let entry name data = { Name = name; Data = data }
    let key = project.DLCKey.ToLowerInvariant()
    let sngMap = buildData.SNGs |> readOnlyDict

    let getManifestName arr =
        let name = partition arr |> snd
        $"manifests/songs_dlc_{key}/{key}_{name}.json"

    use! manifestEntries =
        let attributes arr conv =
            Some(getManifestName arr, createAttributes project conv)

        project.Arrangements
        |> List.choose (function
            | Instrumental i as arr ->
                FromInstrumental(i, sngMap[arr]) |> attributes arr
            | Vocals v as arr ->
                FromVocals v |> attributes arr
            | Showlights _ ->
                None)
        |> List.map (fun (name, attr) ->
            async {
                let data = MemoryStreamPool.Default.GetStream()
                do! Manifest.create attr |> Manifest.toJsonStream data |> Async.AwaitTask
                return entry name data
            })
        |> Async.Parallel
        |> Async.map (List.ofArray >> toDisposableList)

    use! headerEntry =
        async {
            let header = createAttributesHeader project >> Some
            let data = MemoryStreamPool.Default.GetStream()

            do! project.Arrangements
                |> List.choose (function
                    | Instrumental i as arr ->
                        FromInstrumental(i, sngMap[arr]) |> header
                    | Vocals v ->
                        FromVocals v |> header
                    | Showlights _ ->
                        None)
                |> Manifest.createHeader
                |> Manifest.toJsonStream data
                |> Async.AwaitTask
            return entry $"manifests/songs_dlc_{key}/songs_dlc_{key}.hsan" data
        }

    use! sngEntries =
        buildData.SNGs
        |> List.map (fun (arr, sng) ->
            async {
                let data = MemoryStreamPool.Default.GetStream()
                do! SNG.savePacked data platform sng

                let name =
                    let part = partition arr |> snd
                    let path = getPathPart platform Path.SNG
                    $"songs/bin/{path}/{key}_{part}.sng"

                return entry name data
            })
        |> Async.Parallel
        |> Async.map (List.ofArray >> toDisposableList)

    progress ()

    use showlightsEntry =
        let slFile = (List.pick Arrangement.pickShowlights project.Arrangements).XmlPath
        entry $"songs/arr/{key}_showlights.xml" (readFile slFile)

    use fontEntries =
        project.Arrangements
        |> List.choose (function
            | Vocals ({ CustomFont = Some font } as v) ->
                Some(v, font)
            | _ ->
                None)
        |> List.map (fun (v, f) ->
            let name = Utils.getCustomFontName v.Japanese key
            entry $"assets/ui/lyrics/{key}/{name}.dds" (readFile f))
        |> toDisposableList

    use flatModelEntries =
        let embedded = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
        let songData = getResource embedded "res/rsenumerable_song.flat"
        let rootData = getResource embedded "res/rsenumerable_root.flat"

        [ entry "flatmodels/rs/rsenumerable_song.flat" songData
          entry "flatmodels/rs/rsenumerable_root.flat" rootData ]
        |> toDisposableList

    use xBlockEntry =
        let data = MemoryStreamPool.Default.GetStream()

        XBlock.create platform project
        |> XBlock.serialize data

        entry $"gamexblocks/nsongs/{key}.xblock" data

    use appIdEntry =
        entry "appid.appid" (new MemoryStream(Encoding.ASCII.GetBytes(AppId.toString buildData.AppId)))

    use graphEntry =
        let data = MemoryStreamPool.Default.GetStream()

        AggregateGraph.create platform project
        |> AggregateGraph.serialize data

        entry $"{key}_aggregategraph.nt" data

    use gfxEntries =
        buildData.CoverArtFiles.Items
        |> List.map (fun dds ->
            entry $"gfxassets/album_art/album_{key}_{dds.Size}.dds" (readFile dds.Path))
        |> toDisposableList

    use toolkitEntry =
        new MemoryStream(Encoding.UTF8.GetBytes($"Toolkit version: {buildData.BuilderVersion}\nPackage Author: {buildData.Author}\nPackage Version: {project.Version}\nPackage Comment: Remastered"))
        |> entry "toolkit.version"

    progress ()

    // Wait for the wem conversion to complete, if necessary
    do! buildData.AudioConversionTask

    let customAudio =
        project.Arrangements
        |> List.choose (function
            | Instrumental { CustomAudio = Some audioFile } as inst ->
                Some(partition inst |> snd, audioFile)
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
        |> List.append (
            customAudio
            |> List.collect (fun (name, audio) -> createEntries audio $"{project.DLCKey}_{name}")
        )
        |> toDisposableList

    let path =
        match targetPath with
        | WithoutPlatformOrExtension path ->
            $"{path}{getPathPart platform Path.PackageSuffix}.psarc"
        | WithPlatformAndExtension path ->
            path

    do! PSARC.Create(path, true, [
        yield! sngEntries.Items
        yield showlightsEntry
        yield headerEntry
        yield! manifestEntries.Items
        yield! gfxEntries.Items
        yield xBlockEntry
        yield! flatModelEntries.Items
        yield graphEntry
        yield! audioEntries.Items
        yield! fontEntries.Items
        yield toolkitEntry
        yield appIdEntry ]) |> Async.AwaitTask

    progress ()

    // Return path of the created package
    return path }

let private setupInstrumental (part: int) (inst: Instrumental) (config: BuildConfig) =
    let xml = InstrumentalArrangement.Load(inst.XmlPath)

    xml.MetaData.Part <- int16 part

    // Set up correct tone IDs
    xml.Tones.Changes.ForEach(fun tone -> tone.Id <- byte <| Array.IndexOf(xml.Tones.Names, tone.Name))

    // Copy the tuning in case it was edited
    Array.Copy(inst.Tuning, xml.MetaData.Tuning.Strings, 6)

    // Fix "high density"
    if xml.Version < 8uy then xml.FixHighDensity()

    // Generate phrases automatically
    if config.ForcePhraseCreation || (xml.PhraseIterations.Count <= 3 && xml.Sections.Count <= 3) then
        PhraseGenerator.generate xml

    // Apply improvements
    if config.ApplyImprovements then
        ArrangementImprover.applyAll xml
    else
        ArrangementImprover.applyMinimum xml

    // Generate DD
    if xml.Levels.Count = 1 && config.GenerateDD then
        try
            Generator.generateForArrangement config.DDConfig xml |> ignore
        with e ->
            raise <| Exception($"Error generating DD:\n{Utils.distinctExceptionMessages e}", e)

    if config.SaveDebugFiles then
        xml.Save(Path.ChangeExtension(inst.XmlPath, "debug.xml"))

    xml

let private getFontOption (dlcKey: string) (isJapanese: bool) =
    Option.map (fun fontFile ->
        let glyphs =
            Path.ChangeExtension(fontFile, ".glyphs.xml")
            |> GlyphDefinitions.Load

        let name = Utils.getCustomFontName isJapanese dlcKey
        FontOption.CustomFont(glyphs, $"assets/ui/lyrics/{dlcKey}/{name}.dds"))
    >> Option.defaultValue FontOption.DefaultFont

/// Inserts an automatically generated show lights arrangement into the project.
let private addShowLights sngs project =
    let projectPath =
        Path.GetDirectoryName(Arrangement.getFile project.Arrangements.Head)

    let xmlFile =
        Path.Combine(projectPath, "auto_showlights.xml")

    if not <| File.Exists(xmlFile) then
        ShowLightGenerator.generateFile xmlFile sngs

    let showlights = Showlights { Id = ArrangementId.New; XmlPath = xmlFile }
    let arrangments = showlights :: project.Arrangements

    { project with Arrangements = arrangments }

let private checkArrangementIdRegeneration sngs project config =
    async {
        match config.IdResetConfig with
        | None ->
            return Map.empty
        | Some resetConfig ->
            let idsToReplace =
                PhraseLevelComparer.compareToExisting resetConfig.ProjectDirectory sngs

            PhraseLevelComparer.saveLevels resetConfig.ProjectDirectory sngs

            if idsToReplace.IsEmpty then
                return Map.empty
            else
                match! resetConfig.ConfirmIdRegeneration(idsToReplace) with
                | false ->
                    return Map.empty
                | true ->
                    let replacements =
                        project.Arrangements
                        |> List.choose (function
                            | Instrumental inst as arr when idsToReplace |> List.contains inst.Id ->
                                Some(inst.Id, Arrangement.generateIds arr)
                            | _ ->
                                None)
                        |> Map.ofList

                    resetConfig.PostNewIds(replacements)

                    return replacements
    }

let private applyReplacements replacements project sngs =
    let update = function
        | Instrumental inst as arr ->
            replacements
            |> Map.tryFind inst.Id
            |> Option.defaultValue arr
        | other ->
            other

    let updatedArrangements = List.map update project.Arrangements
    let updatedProject = { project with Arrangements = updatedArrangements }

    let updatedSngs =
        sngs
        |> List.map (fun (arr, sng) -> update arr, sng)

    updatedProject, updatedSngs

let private createProgressReporter maximum =
    let mutable current = 0
    let lockObj = obj()

    fun () ->
        lock lockObj (fun () ->
            current <- current + 1
            float current / float maximum * 100.)

/// Builds packages for the given platforms.
/// Returns the paths to the created packages and the tone keys for each arrangement.
let buildPackages (targetPath: TargetPathType) (config: BuildConfig) (project: DLCProject) : Async<string array * Map<ArrangementId, string list>> =
    async {
        match targetPath with
        | WithPlatformAndExtension _ when config.Platforms.Length > 1 ->
            failwith "Build config is for multiple platforms, but path given specifies platform!"
        | _ ->
            ()

        let! audioConversionTask =
            config.AudioConversionTask
            |> Async.StartChild

        use coverArtFiles =
            DDS.createCoverArtImages project.AlbumArtFile
            |> toDisposableList

        let partition = Partitioner.create project

        let progress =
            match config.ProgressReporter with
            | Some progress ->
                let maximumProgress = 3 * config.Platforms.Length + 1
                createProgressReporter maximumProgress >> progress.Report
            | None ->
                ignore

        let sngsWithTones =
            project.Arrangements
            |> List.choose (fun arr ->
                try
                    match arr with
                    | Instrumental inst ->
                        let part = partition arr |> fst
                        let arrangement = setupInstrumental part inst config
                        let sng = ConvertInstrumental.xmlToSng arrangement

                        Some(arr, sng, Some arrangement.Tones.Names)
                    | Vocals v ->
                        let dlcKey = project.DLCKey.ToLowerInvariant()

                        let sng =
                            Vocals.Load(v.XmlPath)
                            |> ConvertVocals.xmlToSng (getFontOption dlcKey v.Japanese v.CustomFont)

                        Some(arr, sng, None)
                    | Showlights _ ->
                        None
                with e ->
                    let failedFile = Arrangement.getFile arr |> Path.GetFileName
                    raise <| Exception($"Converting file {failedFile} failed.\n\n{Utils.distinctExceptionMessages e}", e))

        let sngs =
            sngsWithTones
            |> List.map (fun (arr, sng, _) -> arr, sng)

        let toneKeysMap =
            sngsWithTones
            |> List.choose (fun (arr, _, tonesOpt) ->
                tonesOpt
                |> Option.bind (fun tones ->
                    tones
                    |> Array.choose Option.ofString
                    |> Array.toList
                    |> function
                        | [] ->
                            None
                        | toneKeysList ->
                            Some (Arrangement.getId arr, toneKeysList)))
            |> Map.ofList

        // Check if the arrangement IDs should be regenerated
        let! replacements = checkArrangementIdRegeneration sngs project config
        let project, sngs =
            if replacements.IsEmpty then
                project, sngs
            else
                applyReplacements replacements project sngs

        progress ()

        // Check if a showlights arrangement is included
        let project =
            if project.Arrangements |> List.exists (Arrangement.pickShowlights >> Option.isSome) then
                project
            else
                addShowLights sngs project

        let data =
            { SNGs = sngs
              CoverArtFiles = coverArtFiles
              BuilderVersion = config.BuilderVersion
              Author = config.Author
              AppId = config.AppId
              AudioConversionTask = audioConversionTask }

        let! packagePaths =
            config.Platforms
            |> List.map (build data progress targetPath project)
            |> Async.Parallel

        return packagePaths, toneKeysMap
    }
