module Rocksmith2014.DLCProject.PsarcImporter

open Rocksmith2014.PSARC
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.SNG
open System.IO
open System.Text.RegularExpressions
open PsarcImportUtils

/// Imports a PSARC from the given path into a DLCProject with the project created in the target directory.
let import (psarcPath: string) (targetDirectory: string) = async {
    let platform = Platform.fromPackageFileName psarcPath
    let toTargetPath filename = Path.Combine(targetDirectory, filename)

    use psarc = PSARC.ReadFile psarcPath
    let psarcContents = psarc.Manifest

    let dlcKey =
        match psarcContents |> filterFilesWithExtension "xblock" with
        | [ xblock ] -> Path.GetFileNameWithoutExtension xblock
        | [] -> failwith "The package does not contain an xblock file."
        | _ -> failwith "The package contains more than one xblock file\nSong packs cannot be imported."

    let artFile = List.find (String.endsWith "256.dds") psarcContents
    do! psarc.InflateFile(artFile, toTargetPath "cover.dds")

    let showlights = List.find (String.contains "showlights") psarcContents
    do! psarc.InflateFile(showlights, toTargetPath "arr_showlights.xml")

    let! sngs =
        psarcContents
        |> filterFilesWithExtension "sng"
        |> List.map (fun file -> async {
            use mem = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile(file, mem)
            let! sng = SNG.fromStream mem platform
            return file, sng })
        |> Async.Sequential

    let! fileAttributes =
        psarcContents
        |> filterFilesWithExtension "json"
        |> List.map (fun file -> async {
            use mem = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile(file, mem)
            let! manifest = Manifest.fromJsonStream mem
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
        |> filterFilesWithExtension "bnk"
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
        |> filterFilesWithExtension "wem"
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
        match List.contains "toolkit.version" psarcContents with
        | false ->
            return "1"
        | true ->
            use mem = MemoryStreamPool.Default.GetStream()
            do! psarc.InflateFile("toolkit.version", mem)
            let text = using (new StreamReader(mem)) (fun reader -> reader.ReadToEnd())
            match Regex.Match(text, "Package Version: ([^\r\n]+)\r?\n") with
            | m when m.Success -> return m.Groups.[1].Captures.[0].Value
            | _ -> return "1" }

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
          AudioPreviewStartTime = None
          Arrangements = arrangements
          Tones = tones |> List.map Tone.fromDto }

    let projectFile =
        sprintf "%s_%s" project.ArtistName.SortValue project.Title.SortValue
        |> StringValidator.fileName
        |> sprintf "%s.rs2dlc"
        |> toTargetPath

    do! DLCProject.save projectFile project

    return project, projectFile }
