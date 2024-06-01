module Rocksmith2014.DLCProject.PsarcImporter

open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.PSARC
open Rocksmith2014.SNG
open System
open System.IO
open PsarcImportUtils
open PsarcImportTypes

/// Imports a PSARC from the given path into a DLCProject with the project created in the target directory.
let import progress (psarcPath: string) (targetDirectory: string) =
    backgroundTask {
        let platform = Platform.fromPackageFileName psarcPath
        let toTargetPath filename = Path.Combine(targetDirectory, filename)

        use psarc = PSARC.OpenFile(psarcPath)
        let psarcContents = psarc.Manifest

        let dlcKey =
            match psarcContents |> filterFilesWithExtension "xblock" with
            | [ xblock ] ->
                Path.GetFileNameWithoutExtension(xblock)
            | [] ->
                failwith "The package does not contain an xblock file."
            | _ ->
                failwith "The package contains more than one xblock file.\nSong packs cannot be imported."

        let artFile = List.find (String.endsWith "256.dds") psarcContents
        do! psarc.InflateFile(artFile, toTargetPath "cover.dds")

        let showLightsPath = toTargetPath "arr_showlights_RS2.xml"
        let showlights = List.find (String.contains "showlights") psarcContents
        do! psarc.InflateFile(showlights, showLightsPath)

        let! sngs =
            psarcContents
            |> filterFilesWithExtension "sng"
            |> List.map (fun file ->
                async {
                    use! stream = psarc.GetEntryStream(file) |> Async.AwaitTask
                    let! sng = SNG.fromStream stream platform
                    return file, sng
                })
            |> Async.Sequential

        let! fileAttributes =
            psarcContents
            |> filterFilesWithExtension "json"
            |> List.map (fun file ->
                async {
                    use! stream = psarc.GetEntryStream(file) |> Async.AwaitTask
                    let! manifest = (Manifest.fromJsonStream stream).AsTask() |> Async.AwaitTask
                    return file, Manifest.getSingletonAttributes manifest
                })
            |> Async.Sequential

        // Extract any custom font files
        do! psarcContents
            |> List.filter (String.contains "assets/ui/lyrics")
            |> List.map (fun psarcPath ->
                async {
                    let targetFilename = getFontFilename psarcPath
                    let targetPath = toTargetPath $"{targetFilename}.dds"
                    do! psarc.InflateFile(psarcPath, targetPath) |> Async.AwaitTask
                })
            |> Async.Sequential
            |> Async.Ignore

        progress ()

        let! targetAudioFilesById =
            psarcContents
            |> filterFilesWithExtension "bnk"
            |> List.map (fun bankName ->
                async {
                    let! volume, id = getVolumeAndFileId psarc platform bankName |> Async.AwaitTask
                    let targetFilename = createTargetAudioFilename bankName

                    let audio =
                        { Path = toTargetPath targetFilename
                          Volume = Math.Round(float volume, 1) }

                    return string id, audio
                })
            |> Async.Sequential

        let targetAudioFiles = targetAudioFilesById |> Array.map snd

        let mainAudio =
            targetAudioFiles
            |> Array.find (fun audio -> String.endsWith $"{dlcKey}.wem" audio.Path)

        let previewAudio =
            targetAudioFiles
            |> Array.find (fun audio -> String.endsWith $"{dlcKey}_preview.wem" audio.Path)

        // Extract audio files
        do! targetAudioFilesById
            |> Array.map (fun (id, targetFile) ->
                async {
                    match psarcContents |> List.tryFind (String.contains id) with
                    | Some psarcPath ->
                        do! psarc.InflateFile(psarcPath, targetFile.Path) |> Async.AwaitTask
                    | None ->
                        ()
                })
            |> Async.Sequential
            |> Async.Ignore

        progress ()

        let arrangementsWithSng =
            sngs
            |> Array.Parallel.map (fun (file, sng) ->
                // Change the filenames from "../../dlckey_{NAME}.sng" to "arr_{NAME}_RS2.xml"
                let targetFile =
                    let withoutPath = Path.GetFileName(file)
                    let withoutDlcKey = withoutPath.Substring(withoutPath.IndexOf('_'))
                    let withoutExtension = withoutDlcKey.Remove(withoutDlcKey.Length - 4)
                    toTargetPath $"arr{withoutExtension}_RS2.xml"

                let attributes =
                    fileAttributes
                    |> Array.find (fun (mFile, _) ->
                        Path.GetFileNameWithoutExtension(mFile) = Path.GetFileNameWithoutExtension(file))
                    |> snd

                let importVocals' = importVocals targetDirectory targetFile attributes sng

                let importedData =
                    match file with
                    | JVocalsFile ->
                        importVocals' true
                    | VocalsFile ->
                        importVocals' false
                    | InstrumentalFile ->
                        importInstrumental targetAudioFiles dlcKey targetFile attributes sng

                sng, importedData)
            |> Array.toList

        let arrangements =
            arrangementsWithSng
            |> List.map snd
            |> List.add (Showlights { Id = ArrangementId.New; XmlPath = showLightsPath }, ImportedData.ShowLights)
            |> List.sortBy (fst >> Arrangement.sorter)

        // Save phrase levels
        arrangementsWithSng
        |> List.map (fun (sng, (arr, _)) -> arr, sng)
        |> PhraseLevelComparer.saveLevels targetDirectory

        let tones =
            fileAttributes
            |> Array.choose (fun (_, attr) -> Option.ofObj attr.Tones)
            |> Array.concat
            // Filter out null values and tones without amps
            |> Array.filter (fun x -> notNull x && notNull x.GearList.Amp)
            |> Array.distinctBy (fun x -> x.Key)
            |> Array.toList
            |> List.map toneFromDto

        let metaData =
            fileAttributes
            |> Array.tryPick (fun (file, attr) -> if file.Contains("vocals") then None else Some attr)
            // No instrumental arrangements in PSARC, should not happen
            |> Option.defaultWith (fun () -> fileAttributes[0] |> snd)

        let! version, author, toolkitVersion =
            tryGetFileContents "toolkit.version" psarc
            |> Async.map (function
                | None ->
                    "1", None, None
                | Some text ->
                    let version = text |> parseToolkitPackageMetadata "Version" id "1"
                    let author = text |> parseToolkitPackageMetadata "Author" Some None
                    let toolkitVersion = text |> parseToolkitMetadata "Toolkit version" Some None |> prefixWithToolkit
                    version, author, toolkitVersion)

        let! appId =
            tryGetFileContents "appid.appid" psarc
            |> Async.map (Option.bind AppId.tryCustom)

        let project =
            { Version = version
              DLCKey = StringValidator.dlcKey metaData.DLCKey
              ArtistName =
                { Value = metaData.ArtistName
                  SortValue = metaData.ArtistNameSort }
              JapaneseArtistName = Option.ofString metaData.JapaneseArtistName
              JapaneseTitle = Option.ofString metaData.JapaneseSongName
              Title =
                { Value = metaData.SongName
                  SortValue = metaData.SongNameSort }
              AlbumName =
                { Value = metaData.AlbumName
                  SortValue = metaData.AlbumNameSort }
              Year = metaData.SongYear |> Option.ofNullable |> Option.defaultValue 0
              AlbumArtFile = toTargetPath "cover.dds"
              AudioFile = mainAudio
              AudioFileLength = None
              AudioPreviewFile = previewAudio
              AudioPreviewStartTime = None
              PitchShift = None
              IgnoredIssues = Set.empty
              Arrangements = arrangements |> List.map fst
              Tones = tones
              Author = author }

        let projectFile =
            sprintf "%s_%s" project.ArtistName.SortValue project.Title.SortValue
            |> StringValidator.fileName
            |> sprintf "%s.rs2dlc"
            |> toTargetPath

        do! DLCProject.save projectFile project

        progress ()

        return
            { GeneratedProject = project
              ProjectPath = projectFile
              AppId = appId
              BuildToolVersion = toolkitVersion
              ArrangementData = arrangements }
    }
