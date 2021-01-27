module Rocksmith2014.DLCProject.PsarcImporter

open Rocksmith2014.PSARC
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.SNG
open Rocksmith2014.Conversion
open Rocksmith2014.XML
open System
open System.IO
open System.Text.RegularExpressions

let private getVolumeAndFileId (psarc: PSARC) platform bank = async {
    use mem = MemoryStreamPool.Default.GetStream()
    do! psarc.InflateFile(bank, mem)
    let volume =
        match SoundBank.readVolume mem platform with
        | Ok vol -> vol
        | Error _ -> 0.0f

    let fileId =
        match SoundBank.readFileId mem platform with
        | Ok vol -> vol
        | Error err -> failwith err
        
    return volume, fileId }

let private (|VocalsFile|JVocalsFile|InstrumentalFile|) = function
    | Contains "jvocals" -> JVocalsFile
    | Contains "vocals" -> VocalsFile
    | _ -> InstrumentalFile

/// Creates a target wem filename from a sound bank name.
/// Example: "song_dlckey_xxx.bnk" -> "dlckey_xxx.wem"
let private createTargetAudioFilename (bankName: string) =
    Path.GetFileNameWithoutExtension(bankName).Substring("song_".Length)
    |> sprintf "%s.wem"

/// Imports a vocals SNG into a vocals arrangement.
let private importVocals targetDirectory targetFile customFont (attributes: Attributes) sng isJapanese =
    let vocals = ConvertVocals.sngToXml sng
    Vocals.Save(targetFile, vocals)

    let hasCustomFont =
        sng.SymbolsTextures.[0].Font <> "assets\ui\lyrics\lyrics.dds"

    if hasCustomFont then
        let glyphs = ConvertVocals.extractGlyphData sng
        glyphs.Save(Path.Combine(targetDirectory, "lyrics.glyphs.xml"))

    { XML = targetFile
      Japanese = isJapanese
      CustomFont = if hasCustomFont then customFont else None
      MasterID = attributes.MasterID_RDV
      PersistentID = Guid.Parse(attributes.PersistentID) }
    |> Arrangement.Vocals

/// Imports an instrumental SNG into an instrumental arrangement.
let private importInstrumental (audioFiles: AudioFile array) (dlcKey: string) targetFile (attributes: Attributes) sng =
    let xml = ConvertInstrumental.sngToXml (Some attributes) sng
    xml.Save targetFile

    let arrProps = Option.get attributes.ArrangementProperties

    let tones =
        [ attributes.Tone_A; attributes.Tone_B; attributes.Tone_C; attributes.Tone_D ]
        |> List.choose Option.ofString

    let scrollSpeed =
        let max = Math.Min(int attributes.MaxPhraseDifficulty, attributes.DynamicVisualDensity.Length - 1)
        float attributes.DynamicVisualDensity.[max]

    let bassPicked =
        match Option.ofNullable attributes.BassPick with
        | Some x when x = 1 -> true
        | _ -> false

    let customAudio =
        if attributes.SongBank = $"song_{dlcKey}.bnk" then
            None
        else
            let targetFilename = createTargetAudioFilename attributes.SongBank
            audioFiles
            |> Array.tryFind (fun audio -> String.contains targetFilename audio.Path)

    { XML = targetFile
      Name = ArrangementName.Parse attributes.ArrangementName
      Priority =
        if arrProps.represent = 1uy then ArrangementPriority.Main
        elif arrProps.bonusArr = 1uy then ArrangementPriority.Bonus
        else ArrangementPriority.Alternative
      Tuning = (Option.get attributes.Tuning).ToArray()
      TuningPitch = Utils.centsToTuningPitch(float attributes.CentOffset)
      RouteMask =
        if arrProps.pathBass = 1uy then RouteMask.Bass
        elif arrProps.pathLead = 1uy then RouteMask.Lead
        else RouteMask.Rhythm
      ScrollSpeed = scrollSpeed
      BaseTone = attributes.Tone_Base
      Tones = tones
      BassPicked = bassPicked
      MasterID = attributes.MasterID_RDV
      PersistentID = Guid.Parse(attributes.PersistentID)
      CustomAudio = customAudio }
    |> Arrangement.Instrumental

/// Imports a PSARC from the given path into a DLCProject with the project created in the target directory.
let import (psarcPath: string) (targetDirectory: string) = async {
    let platform =
        if Path.GetFileNameWithoutExtension(psarcPath).EndsWith("_p") then PC else Mac

    let toTargetPath filename = Path.Combine(targetDirectory, filename)

    use psarc = PSARC.ReadFile psarcPath
    let psarcContents = psarc.Manifest

    let dlcKey =
        match List.filter (String.endsWith "xblock") psarcContents with
        | [ xblock ] -> Path.GetFileNameWithoutExtension xblock
        | [] -> failwith "The package does not contain an xblock file."
        | _ -> failwith "The package contains more than one xblock file\nSong packs cannot be imported."

    let artFile = List.find (String.endsWith "256.dds") psarcContents
    do! psarc.InflateFile(artFile, toTargetPath "cover.dds")

    let showlights = List.find (String.contains "showlights") psarcContents
    do! psarc.InflateFile(showlights, toTargetPath "arr_showlights.xml")

    let! sngs =
        psarcContents
        |> List.filter (String.endsWith "sng")
        |> List.map (fun file -> async {
            use mem = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile(file, mem)
            let! sng = SNG.fromStream mem platform
            return file, sng })
        |> Async.Sequential

    let! fileAttributes =
        psarcContents
        |> List.filter (String.endsWith "json")
        |> List.map (fun file -> async {
            use mem = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile(file, mem)
            let! manifest = Manifest.fromJsonStream(mem)
            return file, Manifest.getSingletonAttributes manifest })
        |> Async.Sequential

    let! customFont = async {
        match List.tryFind (String.contains "assets/ui/lyrics") psarcContents with
        | Some font ->
            let targetPath = toTargetPath "lyrics.dds"
            use file = File.Create targetPath
            do! psarc.InflateFile(font, file)
            return Some targetPath
        | None -> return None }

    let! targetAudioFilesById =
        psarcContents
        |> List.filter (String.endsWith "bnk")
        |> List.map (fun bankName -> async {
            let! volume, id = getVolumeAndFileId psarc platform bankName
            let targetFilename = createTargetAudioFilename bankName
            return string id, { Path = toTargetPath targetFilename; Volume = float volume } })
        |> Async.Sequential

    let targetAudioFiles = targetAudioFilesById |> Array.map snd
    let mainAudio = targetAudioFiles |> Array.find (fun audio -> String.endsWith $"{dlcKey}.wem" audio.Path)
    let previewAudio = targetAudioFiles |> Array.find (fun audio -> String.endsWith $"{dlcKey}_preview.wem" audio.Path)

    // Extract audio files
    do! psarcContents
        |> List.filter (String.endsWith "wem")
        |> List.map (fun pathInPsarc ->
            let targetAudioFile =
                targetAudioFilesById
                |> Array.find (fun (id, _) -> String.contains id pathInPsarc)
                |> snd
                    
            psarc.InflateFile(pathInPsarc, targetAudioFile.Path))
        |> Async.Sequential
        |> Async.Ignore

    let arrangements =
        sngs
        |> Array.Parallel.map (fun (file, sng) ->
            // Change the filenames from "dlckey_name" to "arr_name"
            let targetFile =
                let f = Path.GetFileName file
                toTargetPath <| Path.ChangeExtension("arr" + f.Substring(f.IndexOf '_'), "xml")
            let attributes =
                fileAttributes
                |> Array.find (fun (mFile, _) -> Path.GetFileNameWithoutExtension mFile = Path.GetFileNameWithoutExtension file)
                |> snd

            let importVocals' = importVocals targetDirectory targetFile customFont attributes sng

            match file with
            | JVocalsFile -> importVocals' true
            | VocalsFile -> importVocals' false
            | InstrumentalFile -> importInstrumental targetAudioFiles dlcKey targetFile attributes sng)
        |> Array.toList
        |> List.append [ Showlights { XML = toTargetPath "arr_showlights.xml" } ]
        |> List.sortBy Arrangement.sorter
           
    let tones =
        fileAttributes
        |> Array.choose (fun (_, attr) -> Option.ofObj attr.Tones)
        |> Array.collect id
        |> Array.distinctBy (fun x -> x.Key)
        |> Array.toList

    let metaData =
        fileAttributes
        |> Array.find (fun (file, _) -> not <| file.Contains "vocals")
        |> snd

    let! version = async {
        let tkVer = List.tryFind ((=) "toolkit.version") psarcContents

        match tkVer with
        | Some tk ->
            use mem = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile(tk, mem)
            let text = using (new StreamReader(mem)) (fun reader -> reader.ReadToEnd())
            let m = Regex.Match(text, "Package Version: ([^\r\n]+)\r?\n")
            if m.Success then
                return m.Groups.[1].Captures.[0].Value
            else
                return "1"
        | None -> return "1" }

    let project =
        { Version = version
          DLCKey = metaData.DLCKey
          ArtistName = { Value = metaData.ArtistName; SortValue = metaData.ArtistNameSort }
          JapaneseArtistName = Option.ofObj metaData.JapaneseArtistName |> Option.bind Option.ofString
          JapaneseTitle = Option.ofObj metaData.JapaneseSongName |> Option.bind Option.ofString
          Title = { Value = metaData.SongName; SortValue = metaData.SongNameSort }
          AlbumName = { Value = metaData.AlbumName; SortValue = metaData.AlbumNameSort }
          Year = metaData.SongYear |> Option.ofNullable |> Option.defaultValue 0
          AlbumArtFile = toTargetPath "cover.dds"
          AudioFile = mainAudio
          AudioPreviewFile = previewAudio
          Arrangements = arrangements
          Tones = tones }

    let projectFile =
        sprintf "%s_%s" project.ArtistName.SortValue project.Title.SortValue
        |> StringValidator.fileName
        |> sprintf "%s.rs2dlc"
        |> toTargetPath

    do! DLCProject.save projectFile project

    return project, projectFile }
