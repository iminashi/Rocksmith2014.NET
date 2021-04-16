module Rocksmith2014.DLCProject.PsarcImportUtils

open Rocksmith2014.PSARC
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.Conversion
open Rocksmith2014.XML
open System.IO
open System

/// Reads the volume and file ID from the PSARC for the sound bank with the given name.
let getVolumeAndFileId (psarc: PSARC) platform bankName = async {
    use mem = MemoryStreamPool.Default.GetStream()
    do! psarc.InflateFile(bankName, mem)
    let volume =
        match SoundBank.readVolume mem platform with
        | Ok vol -> vol
        | Error _ -> 0.0f

    let fileId =
        match SoundBank.readFileId mem platform with
        | Ok vol -> vol
        | Error err -> failwith err

    return volume, fileId }

/// Active pattern for detecting arrangement type from a filename.
let (|VocalsFile|JVocalsFile|InstrumentalFile|) = function
    | Contains "jvocals" -> JVocalsFile
    | Contains "vocals" -> VocalsFile
    | _ -> InstrumentalFile

/// Creates a target wem filename from a sound bank name.
/// Example: "song_dlckey_xxx.bnk" -> "dlckey_xxx.wem"
let createTargetAudioFilename (bankName: string) =
    Path.GetFileNameWithoutExtension(bankName).Substring("song_".Length)
    |> sprintf "%s.wem"

let filterFilesWithExtension extension = List.filter (String.endsWith extension)

/// Imports a vocals SNG into a vocals arrangement.
let importVocals targetDirectory targetFile customFont (attributes: Attributes) sng isJapanese =
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
let importInstrumental (audioFiles: AudioFile array) (dlcKey: string) targetFile (attributes: Attributes) sng =
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
      TuningPitch = Utils.centsToTuningPitch(attributes.CentOffset.GetValueOrDefault())
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
