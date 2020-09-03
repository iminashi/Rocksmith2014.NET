module Rocksmith2014.DLCProject.ToolkitImporter

open System
open System.IO
open System.Xml
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Rocksmith2014.Common.Manifest

let private optionalString (node: XmlNode) name =
    node.Item name
    |> Option.ofObj
    |> Option.bind (fun x -> Option.ofString x.InnerText)

let private importArrangement (arr: XmlNode) =
    let xml = arr.Item("SongXml").Item("File", "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.DLCPackage.AggregateGraph").InnerText

    match (arr.Item "ArrangementType").InnerText with
    | "Guitar" | "Bass" ->
        let priority =
            try
                let arrProp = arr.Item("ArrangementPropeties")
                if isNull arrProp then
                    if arr.Item("BonusArr").InnerText = "true" then ArrangementPriority.Bonus
                    else ArrangementPriority.Main
                else
                    let tryGetPriority ns =
                        if arrProp.Item("Represent", ns).InnerText = "1" then ArrangementPriority.Main
                        elif arrProp.Item("BonusArr", ns).InnerText = "1" || arr.Item("BonusArr").InnerText = "true" then ArrangementPriority.Bonus
                        else ArrangementPriority.Alternative

                    // The XML namespace was renamed at some point.
                    try tryGetPriority "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.XML"
                    with _ -> tryGetPriority "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.Xml"
            with _ -> ArrangementPriority.Main

        let name =
            let n = arr.Item "Name"
            (if isNull n then arr.Item "ArrangementName" else n).InnerText
            |> ArrangementName.Parse

        let isPicked =
            name = ArrangementName.Bass
            &&
            arr.Item("PluckedType").InnerText <> "NotPicked"

        let tuning =
            arr.Item("TuningStrings").ChildNodes
            |> Seq.cast<XmlNode>
            |> Seq.map (fun x -> int16 x.InnerText)
            |> Seq.toArray

        let tones =
            [ for t in 'A'..'D' -> Option.ofString (arr.Item(sprintf "Tone%c" t).InnerText) ]
            |> List.choose id

        { XML = xml
          Name = name
          RouteMask = RouteMask.Parse(arr.Item("RouteMask").InnerText)
          Priority = priority
          ScrollSpeed = float (arr.Item("ScrollSpeed").InnerText) / 10.
          BassPicked = isPicked
          Tuning = tuning
          TuningPitch = float (arr.Item("TuningPitch").InnerText)
          BaseTone = arr.Item("ToneBase").InnerText
          Tones = tones
          MasterID = int (arr.Item("MasterId").InnerText)
          PersistentID = Guid.Parse(arr.Item("Id").InnerText) }
        |> Instrumental

    | "Vocal" ->
        let isJapanese =
            let n = arr.Item("Name")
            (if isNull n then arr.Item("ArrangementName") else n).InnerText = "JVocals"

        let customFont =
            let lyricsArtPath = arr.Item "LyricsArtPath"
            if not <| isNull lyricsArtPath then
                Option.ofString lyricsArtPath.InnerText
            else
                let lyricArt = arr.Item "LyricArt"
                if not <| isNull lyricArt then
                    Option.ofString lyricArt.InnerText  
                else
                    let glyphDefs =
                        let a = arr.Item "GlyphDefinitons" // sic
                        if isNull a then arr.Item "GlyphDefinitions" else a
                    if isNull glyphDefs || glyphDefs.IsEmpty then
                        None
                    else
                        // x.glyphs.xml -> x.dds
                        Some (Path.GetFileNameWithoutExtension (Path.GetFileNameWithoutExtension glyphDefs.InnerText) + ".dds")                 
            
        { XML = xml
          Japanese = isJapanese 
          CustomFont = customFont
          MasterID = int (arr.Item("MasterId").InnerText)
          PersistentID = Guid.Parse(arr.Item("Id").InnerText) }
        |> Vocals

    | "ShowLight" -> Showlights { XML = xml }

    | unknown -> failwith (sprintf "Unknown arrangement type: %s" unknown)

let import (templatePath: string) =
    let doc = XmlDocument()
    doc.Load(templatePath)
    let docEl = doc.DocumentElement

    if docEl.Name <> "DLCPackageData" then failwith "Not a valid Toolkit template file."

    let songInfo = docEl.Item "SongInfo"
    let year =
        match Int32.TryParse((songInfo.Item "SongYear").InnerText) with
        | true, y -> y
        | false, _ -> DateTime.Now.Year

    let audioPath = (docEl.Item "OggPath").InnerText
    let previewPath =
        let fn = Path.GetFileNameWithoutExtension audioPath
        let prevFile = fn + "_preview" + (Path.GetExtension audioPath)
        let prevPath = Path.Combine (Path.GetDirectoryName(templatePath), prevFile)
        if File.Exists prevPath then prevFile else String.Empty

    let arrangements =
        docEl.Item("Arrangements").ChildNodes
        |> Seq.cast<XmlNode>
        |> Seq.map importArrangement
        |> Seq.sortBy Arrangement.sorter
        |> Seq.toList

    let tones =
        let ns = Some "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.DLCPackage.Manifest2014.Tone"
        docEl.Item("TonesRS2014").ChildNodes
        |> Seq.cast<XmlNode>
        |> Seq.map (Tone.importXml ns)
        |> Seq.toList

    let version =
        let tkInfo = docEl.Item "ToolkitInfo"
        if isNull tkInfo then docEl.Item("PackageVersion").InnerText
        else tkInfo.Item("PackageVersion").InnerText

    { Version = version
      DLCKey = docEl.Item("Name").InnerText
      ArtistName = { Value = songInfo.Item("Artist").InnerText; SortValue = songInfo.Item("ArtistSort").InnerText }
      JapaneseArtistName = optionalString songInfo "JapaneseArtistName"
      JapaneseTitle = optionalString songInfo "JapaneseSongName"
      Title = { Value = songInfo.Item("SongDisplayName").InnerText; SortValue = songInfo.Item("SongDisplayNameSort").InnerText }
      AlbumName = { Value = songInfo.Item("Album").InnerText; SortValue = songInfo.Item("AlbumSort").InnerText }
      Year = year
      AlbumArtFile = docEl.Item("AlbumArtPath").InnerText
      AudioFile = { Path = audioPath; Volume = float (docEl.Item "Volume").InnerText }
      AudioPreviewFile = { Path = previewPath; Volume = float (docEl.Item "PreviewVolume").InnerText }
      Arrangements = arrangements
      Tones = tones }
    |> DLCProject.toAbsolutePaths (Path.GetDirectoryName templatePath) 
