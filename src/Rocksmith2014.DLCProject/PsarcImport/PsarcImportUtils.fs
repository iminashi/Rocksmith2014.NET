module Rocksmith2014.DLCProject.PsarcImportUtils

open System
open System.IO
open System.Text.RegularExpressions
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.Conversion
open Rocksmith2014.PSARC
open Rocksmith2014.XML
open Rocksmith2014.DLCProject.PsarcImportTypes

let [<Literal>] private DefaultFontPath = @"assets\ui\lyrics\lyrics.dds"

/// Reads the volume and file ID from the PSARC for the sound bank with the given name.
let getVolumeAndFileId (psarc: PSARC) (platform: Platform) (bankName: string) =
    backgroundTask {
        use! stream = psarc.GetEntryStream(bankName)

        let volume =
            match SoundBank.readVolume stream platform with
            | Ok vol -> vol
            | Error _ -> 0.0f

        let fileId =
            match SoundBank.readFileId stream platform with
            | Ok vol -> vol
            | Error err -> failwith err

        return volume, fileId
    }

/// Active pattern for detecting an arrangement type from a filename.
let (|VocalsFile|JVocalsFile|InstrumentalFile|) = function
    | Contains "jvocals" -> JVocalsFile
    | Contains "vocals" -> VocalsFile
    | _ -> InstrumentalFile

/// Creates a target wem filename from a sound bank name.
/// Example: "song_dlckey_xxx.bnk" -> "dlckey_xxx.wem"
let createTargetAudioFilename (bankName: string) =
    Path.GetFileNameWithoutExtension(bankName)
        .Substring("song_".Length)
    |> sprintf "%s.wem"

let filterFilesWithExtension extension = List.filter (String.endsWith extension)

/// Gets the filename for a custom font.
/// ".../lyrics_dlckey.dds" -> "lyrics"
/// ".../lyrics_something_dlckey.dds" -> "lyrics_something"
let getFontFilename (path: string) =
    let fn = Path.GetFileNameWithoutExtension(path)
    fn.Substring(0, fn.LastIndexOf('_'))

/// Imports a vocals SNG into a vocals arrangement.
let importVocals (targetDirectory: string) (targetFile: string) (attributes: Attributes) (sng: Rocksmith2014.SNG.SNG) (isJapanese: bool) =
    let vocals = ConvertVocals.sngToXml sng
    Vocals.Save(targetFile, vocals)

    let customFont =
        match sng.SymbolsTextures[0].Font with
        | DefaultFontPath ->
            None
        | fontPath ->
            let filename = getFontFilename fontPath
            let glyphs = ConvertVocals.extractGlyphData sng
            glyphs.Save(Path.Combine(targetDirectory, $"{filename}.glyphs.xml"))
            Some(Path.Combine(targetDirectory, $"{filename}.dds"))

    { Id = ArrangementId.New
      XmlPath = targetFile
      Japanese = isJapanese
      CustomFont = customFont
      MasterId = attributes.MasterID_RDV
      PersistentId = Guid.Parse(attributes.PersistentID) }
    |> Arrangement.Vocals,
    ImportedData.Vocals vocals

/// Imports an instrumental SNG into an instrumental arrangement.
let importInstrumental (audioFiles: AudioFile array) (dlcKey: string) (targetPath: string) (attributes: Attributes) (sng: Rocksmith2014.SNG.SNG) =
    let xml = ConvertInstrumental.sngToXml (Some attributes) sng
    xml.Save(targetPath)

    let arrProps = Option.get attributes.ArrangementProperties

    let tones =
        [ attributes.Tone_A
          attributes.Tone_B
          attributes.Tone_C
          attributes.Tone_D ]
        |> List.choose Option.ofString

    let scrollSpeed =
        let max = Math.Min(int attributes.MaxPhraseDifficulty, attributes.DynamicVisualDensity.Length - 1)
        Math.Round(float attributes.DynamicVisualDensity[max], 1, MidpointRounding.AwayFromZero)

    let customAudio =
        if attributes.SongBank = $"song_{dlcKey}.bnk" then
            None
        else
            let targetFilename = createTargetAudioFilename attributes.SongBank
            audioFiles
            |> Array.tryFind (fun audio -> String.contains targetFilename audio.Path)

    { Id = ArrangementId.New
      XmlPath = targetPath
      Name = ArrangementName.Parse(attributes.ArrangementName)
      Priority =
        if arrProps.represent = 1uy then
            ArrangementPriority.Main
        elif arrProps.bonusArr = 1uy then
            ArrangementPriority.Bonus
        else
            ArrangementPriority.Alternative
      Tuning = (Option.get attributes.Tuning).ToArray()
      TuningPitch = Utils.centsToTuningPitch(attributes.CentOffset.GetValueOrDefault())
      RouteMask =
        if arrProps.pathBass = 1uy then
            RouteMask.Bass
        elif arrProps.pathLead = 1uy then
            RouteMask.Lead
        else
            RouteMask.Rhythm
      ScrollSpeed = scrollSpeed
      BaseTone = attributes.Tone_Base
      Tones = tones
      BassPicked = arrProps.bassPick = 1uy
      MasterId = attributes.MasterID_RDV
      PersistentId = Guid.Parse(attributes.PersistentID)
      CustomAudio = customAudio
      ArrangementProperties = None }
    |> Arrangement.Instrumental,
    ImportedData.Instrumental xml

/// Converts a tone from a DTO, ensuring that the imported tone has descriptors.
let toneFromDto (dto: ToneDto) =
    let tone = Tone.fromDto dto

    if tone.ToneDescriptors.Length = 0 then
        { tone with
            ToneDescriptors =
                ToneDescriptor.getDescriptionsOrDefault tone.Key
                |> Array.map (fun x -> x.UIName) }
    else
        tone

/// Parses a value from Toolkit.version text.
let parseToolkitMetadata attr map defaultValue text =
    match Regex.Match(text, $"{attr}: ([^\r\n]+)\r?\n") with
    | m when m.Success ->
        map m.Groups[1].Captures[0].Value
    | _ ->
        defaultValue

/// Parses a value (starting with "Package") from Toolkit.version text.
let parseToolkitPackageMetadata attr = parseToolkitMetadata $"Package {attr}"

/// Prefixes the version string with "Toolkit" if it starts with a four part version number.
let prefixWithToolkit (versionOpt: string option) =
    versionOpt
    |> Option.map (fun version ->
        if Regex.IsMatch(version, "\d+\.\d+\.\d+\.\d+") then
            $"Toolkit {version}"
        else
            version)

let private getFileContents (psarc: PSARC) (pathInPsarc: string) =
    backgroundTask {
        use! stream = psarc.GetEntryStream(pathInPsarc)
        return using (new StreamReader(stream)) (fun reader -> reader.ReadToEnd())
    }

/// Returns the file contents as string if the file is found in the PSARC.
let tryGetFileContents (pathInPsarc: string) (psarc: PSARC) =
    async {
        match List.contains pathInPsarc psarc.Manifest with
        | false ->
            return None
        | true ->
            let! text = getFileContents psarc pathInPsarc |> Async.AwaitTask
            return Some text
    }
