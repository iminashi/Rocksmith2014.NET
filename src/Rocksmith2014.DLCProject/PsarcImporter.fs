module Rocksmith2014.DLCProject.PsarcImporter

open System.IO
open Rocksmith2014.PSARC
open Rocksmith2014.Common
open Rocksmith2014.SNG
open Rocksmith2014.Conversion
open Rocksmith2014.XML
open Rocksmith2014.DLCProject.Manifest
open System
open System.Text.RegularExpressions

let private getVolume (psarc: PSARC) platform bank = async {
    use mem = MemoryStreamPool.Default.GetStream()
    do! psarc.InflateFile(bank, mem)
    return match SoundBank.readVolume mem platform with
           | Ok vol -> vol
           | Error _ -> 0.0f }

/// Imports a PSARC from the given path into a DLCProject with the project created in the target directory.
let import (psarcPath: string) (targetDirectory: string) = async {
    let platform =
        if Path.GetFileNameWithoutExtension(psarcPath).EndsWith("_p") then PC else Mac

    use psarc = PSARC.ReadFile(psarcPath)
    let psarcContents = psarc.Manifest

    let artFile =
        psarcContents
        |> Seq.find (fun x -> x.EndsWith "256.dds")
    do! psarc.InflateFile(artFile, Path.Combine(targetDirectory, "cover.dds"))

    let showlights =
        psarcContents
        |> Seq.find (fun x -> x.Contains "showlights")
    do! psarc.InflateFile(showlights, Path.Combine(targetDirectory, "arr_showlights.xml"))

    let audioFiles =
        psarcContents
        |> Seq.filter (fun x -> x.EndsWith "wem")

    if Seq.length audioFiles > 2 then failwith "Package contains more than 2 audio files."

    do! audioFiles
        |> Seq.mapi (fun i x -> async {
            do! psarc.InflateFile(x, Path.Combine(targetDirectory, sprintf "%i.wem" i))
        })
        |> Async.Sequential
        |> Async.Ignore

    let audioInfo1 = FileInfo(Path.Combine(targetDirectory, "0.wem"))
    let audioInfo2 = FileInfo(Path.Combine(targetDirectory, "1.wem"))
    if audioInfo1.Length > audioInfo2.Length then
        File.Move(audioInfo1.FullName, Path.Combine(targetDirectory, "audio.wem"), true)
        File.Move(audioInfo2.FullName, Path.Combine(targetDirectory, "audio_preview.wem"), true)
    else
        File.Move(audioInfo2.FullName, Path.Combine(targetDirectory, "audio.wem"), true)
        File.Move(audioInfo1.FullName, Path.Combine(targetDirectory, "audio_preview.wem"), true)

    let! sngs =
        psarcContents
        |> Seq.filter (fun x -> x.EndsWith "sng")
        |> Seq.map (fun file -> async {
            use mem = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile(file, mem)
            let! sng = SNG.fromStream mem platform
            return file, sng })
        |> Async.Sequential

    let! manifests =
        psarcContents
        |> Seq.filter (fun x -> x.EndsWith "json")
        |> Seq.map (fun file -> async {
            use mem = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile(file, mem)
            let! manifest = Manifest.fromJsonStream(mem)
            return file, manifest })
        |> Async.Sequential

    let! customFont = async {
        let font =
            psarcContents
            |> Seq.tryFind (fun x -> x.Contains "assets/ui/lyrics")
        match font with
        | Some font ->
            let fn = Path.Combine(targetDirectory, "lyrics.dds")
            use file = File.Create fn
            do! psarc.InflateFile(font, file)
            return Some fn
        | None -> return None }

    let arrangements =
        sngs
        |> Array.Parallel.map (fun (file, sng) ->
            // Change the file names from "dlckey_name" to "arr_name"
            let targetFile =
                let f = Path.GetFileName file
                Path.Combine(targetDirectory, Path.ChangeExtension("arr" + f.Substring(f.IndexOf '_'), "xml"))
            let attributes =
                manifests
                |> Seq.find (fun (mFile, _) -> Path.GetFileNameWithoutExtension mFile = Path.GetFileNameWithoutExtension file)
                |> (snd >> Manifest.getSingletonAttributes)

            if file.Contains "vocals" then
                let vocals = ConvertVocals.sngToXml sng
                Vocals.Save(targetFile, vocals)

                let hasCustomFont =
                    sng.SymbolsTextures.[0].Font <> "assets\ui\lyrics\lyrics.dds"

                if hasCustomFont then
                    let glyphs = ConvertVocals.extractGlyphData sng
                    glyphs.Save(Path.Combine(targetDirectory, "lyrics.glyphs.xml"))

                { XML = targetFile
                  Japanese = file.Contains "jvocals"
                  CustomFont = if hasCustomFont then customFont else None
                  MasterID = attributes.MasterID_RDV
                  PersistentID = Guid.Parse(attributes.PersistentID) }
                |> Arrangement.Vocals
            else
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
                    match attributes.BassPick |> Option.ofNullable with
                    | Some x when x = 1 -> true
                    | _ -> false

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
                  PersistentID = Guid.Parse(attributes.PersistentID) }
                |> Arrangement.Instrumental)
        |> Array.toList
        |> List.append [ Showlights { XML = Path.Combine(targetDirectory, "arr_showlights.xml") } ]
        |> List.sortBy Arrangement.sorter

    let previewBank, mainBank =
        psarcContents
        |> Seq.filter (fun x -> x.EndsWith "bnk")
        |> Seq.toArray
        |> Array.partition (fun x -> x.Contains "preview")
        |> fun (prev, main) -> Array.head prev, Array.head main

    let! mainVolume = getVolume psarc platform mainBank
    let! previewVolume = getVolume psarc platform previewBank
            
    let tones =
        manifests
        |> Array.choose (fun (_, manifest) -> Option.ofObj (Manifest.getSingletonAttributes manifest).Tones)
        |> Array.collect id
        |> Array.distinctBy (fun x -> x.Key)
        |> Array.toList

    let metaData =
        manifests
        |> Array.find (fun (file, _) -> not <| file.Contains "vocals")
        |> (snd >> Manifest.getSingletonAttributes)

    let! version = async {
        let tkVer =
            psarcContents
            |> Seq.tryFind ((=) "toolkit.version")
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
          AlbumArtFile = Path.Combine(targetDirectory, "cover.dds")
          AudioFile = { Path = Path.Combine(targetDirectory, "audio.wem"); Volume = float mainVolume }
          AudioPreviewFile = { Path = Path.Combine(targetDirectory, "audio_preview.wem"); Volume = float previewVolume }
          Arrangements = arrangements
          Tones = tones }

    let projectFile =
        let fn =
            sprintf "%s_%s" project.ArtistName.Value project.Title.Value
            |> StringValidator.fileName
        Path.Combine(targetDirectory, fn + ".rs2dlc")

    do! DLCProject.save projectFile project

    return project, projectFile }
