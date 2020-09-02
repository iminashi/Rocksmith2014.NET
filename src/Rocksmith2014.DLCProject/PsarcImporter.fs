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
    mem.Position <- 0L
    return match SoundBank.readVolume mem platform with
           | Ok vol -> vol
           | Error _ -> -12.0f }

let import (psarcFile: string) (targetDirectory: string) = async {
    let platform =
        if Path.GetFileNameWithoutExtension(psarcFile).EndsWith("_p") then PC else Mac

    use psarc = PSARC.ReadFile(psarcFile)
    let artFile =
        psarc.Manifest
        |> Seq.find (fun x -> x.EndsWith "256.dds")
    do! psarc.InflateFile(artFile, Path.Combine(targetDirectory, "cover.dds"))

    let showlights =
        psarc.Manifest
        |> Seq.find (fun x -> x.Contains "showlights")
    do! psarc.InflateFile(showlights, Path.Combine(targetDirectory, "arr_showlights.xml"))

    let audioFiles =
        psarc.Manifest
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
        psarc.Manifest
        |> Seq.filter (fun x -> x.EndsWith "sng")
        |> Seq.map (fun x -> async {
            use mem = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile(x, mem)
            let! sng = SNG.fromStream mem platform
            return {| File = x; SNG = sng |} })
        |> Async.Sequential

    let! manifests =
        psarc.Manifest
        |> Seq.filter (fun x -> x.EndsWith "json")
        |> Seq.map (fun x -> async {
            use mem = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile(x, mem)
            let! manifest = Manifest.fromJsonStream(mem)
            return {| File = x; Manifest = manifest |} })
        |> Async.Sequential

    let! customFont = async {
        let font =
            psarc.Manifest
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
        |> Array.Parallel.map (fun s ->
            // Change the file names from "dlckey_name" to "arr_name"
            let file =
                let f = Path.GetFileName s.File
                "arr" + f.Substring(f.IndexOf '_')
            let targetFile = Path.Combine(targetDirectory, Path.ChangeExtension(file, "xml"))
            let attributes =
                manifests
                |> Seq.find (fun m -> m.File.Contains(Path.GetFileNameWithoutExtension s.File))
                |> fun m -> Manifest.getSingletonAttributes m.Manifest

            if s.File.Contains "vocals" then
                let vocals = ConvertVocals.sngToXml s.SNG
                Vocals.Save(targetFile, vocals)

                let hasCustomFont =
                    s.SNG.SymbolsTextures.[0].Font <> "assets\ui\lyrics\lyrics.dds"

                { XML = targetFile
                  Japanese = s.File.Contains "jvocals"
                  CustomFont = if hasCustomFont then customFont else None
                  MasterID = attributes.MasterID_RDV
                  PersistentID = Guid.Parse(attributes.PersistentID) }
                |> Arrangement.Vocals
            else
                let xml = ConvertInstrumental.sngToXml (Some attributes) s.SNG
                xml.Save targetFile

                let arrProps = Option.get attributes.ArrangementProperties

                let tones =
                    [ Option.ofString attributes.Tone_A
                      Option.ofString attributes.Tone_B
                      Option.ofString attributes.Tone_C
                      Option.ofString attributes.Tone_D ]
                    |> List.choose id

                let scrollSpeed =
                    let max = Math.Min(int attributes.MaxPhraseDifficulty, attributes.DynamicVisualDensity.Length - 1)
                    float attributes.DynamicVisualDensity.[max]

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
                  BassPicked = attributes.BassPick |> Option.ofNullable |> Option.isSome
                  MasterID = attributes.MasterID_RDV
                  PersistentID = Guid.Parse(attributes.PersistentID) }
                |> Arrangement.Instrumental)
        |> Array.toList
        |> List.append [ Showlights { XML = Path.Combine(targetDirectory, "arr_showlights.xml") } ]
        |> List.sortBy Arrangement.sorter

    let previewBank, mainBank =
        psarc.Manifest
        |> Seq.filter (fun x -> x.EndsWith "bnk")
        |> Seq.toArray
        |> Array.partition (fun x -> x.Contains "preview")
        |> fun x -> Array.head (fst x), Array.head (snd x)

    let! mainVolume = getVolume psarc platform mainBank
    let! previewVolume = getVolume psarc platform previewBank
            
    let tones =
        manifests
        |> Array.choose (fun x -> Option.ofObj (Manifest.getSingletonAttributes x.Manifest).Tones)
        |> Array.collect id
        |> Array.distinctBy (fun x -> x.Key)
        |> Array.toList

    let metaData =
        manifests
        |> Array.find (fun x -> not <| x.File.Contains "vocals")
        |> fun x -> x.Manifest |> Manifest.getSingletonAttributes

    let! version = async {
        let tkVer =
            psarc.Manifest
            |> Seq.tryFind ((=) "toolkit.version")
        match tkVer with
        | Some tk ->
            use mem = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile(tk, mem)
            use reader = new StreamReader(mem)
            let text = reader.ReadToEnd()
            let m = Regex.Match(text, "Package Version: (.*)\r\n")
            if m.Success then
                return m.Groups.[1].Captures.[0].Value
            else
                return "1"
        | None -> return "1" }

    return { Version = version
             DLCKey = metaData.DLCKey
             ArtistName = SortableString.Create(metaData.ArtistName, metaData.ArtistNameSort)
             JapaneseArtistName = Option.ofObj metaData.JapaneseArtistName
             JapaneseTitle = Option.ofObj metaData.JapaneseSongName
             Title = SortableString.Create(metaData.SongName, metaData.SongNameSort)
             AlbumName = SortableString.Create(metaData.AlbumName, metaData.AlbumNameSort)
             Year = metaData.SongYear |> Option.ofNullable |> Option.defaultValue 0
             AlbumArtFile = Path.Combine(targetDirectory, "cover.dds")
             AudioFile = { Path = Path.Combine(targetDirectory, "audio.wem"); Volume = float mainVolume }
             AudioPreviewFile = { Path = Path.Combine(targetDirectory, "audio_preview.wem"); Volume = float previewVolume }
             Arrangements = arrangements
             Tones = tones } }
