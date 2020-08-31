module Rocksmith2014.DLCProject.ToolkitImporter

open System.Xml
open System
open System.IO
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Rocksmith2014.Common.Manifest

let private importArrangement (arr: XmlNode) =
    let d4p1 = "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.XML"

    let xml = arr.Item("SongXml").Item("File", "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.DLCPackage.AggregateGraph").InnerText

    match (arr.Item "ArrangementType").InnerText with
    | "Guitar" | "Bass" ->
        let priority =
            let arrProp = arr.Item("ArrangementPropeties")
            if arrProp.Item("Represent", d4p1).InnerText = "1" then ArrangementPriority.Main
            elif arrProp.Item("BonusArr", d4p1).InnerText = "1" then ArrangementPriority.Bonus
            else ArrangementPriority.Alternative

        let name =
            let n = arr.Item "Name"
            (if isNull n then arr.Item "ArrangementName" else n).InnerText
            |> ArrangementName.Parse

        let isPicked =
            name = ArrangementName.Bass
            &&
            arr.Item("PluckedType").InnerText <> "NotPicked"

        let tuning =
            let t = arr.Item "TuningStrings"
            [| for i in 0..5 -> int16 (t.Item(sprintf "String%i" i, d4p1).InnerText) |]

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
            
        { XML = xml
          Japanese = isJapanese 
          CustomFont = None
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
    let jName =
        songInfo.Item "JapaneseArtistName"
        |> Option.ofObj
        |> Option.bind (fun x -> Option.ofString x.InnerText)

    let jTitle =
        songInfo.Item "JapaneseSongName"
        |> Option.ofObj
        |> Option.bind (fun x -> Option.ofString x.InnerText)

    let audioPath = (docEl.Item "OggPath").InnerText
    let previewPath =
        let fn = Path.GetFileNameWithoutExtension audioPath
        let prev = fn + "_preview." + (Path.GetExtension audioPath)
        if File.Exists prev then prev else String.Empty

    let arrangements =
        (docEl.Item "Arrangements").ChildNodes
        |> Seq.cast<XmlNode>
        |> Seq.map importArrangement
        |> Seq.toList

    let tones =
        let ns = Some "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.DLCPackage.Manifest2014.Tone"
        (docEl.Item "TonesRS2014").ChildNodes
        |> Seq.cast<XmlNode>
        |> Seq.map (Tone.importXml ns)
        |> Seq.toList

    { Version = ((docEl.Item "ToolkitInfo").Item "PackageVersion").InnerText
      DLCKey = (docEl.Item "Name").InnerText
      ArtistName = SortableString.Create((songInfo.Item "Artist").InnerText, (songInfo.Item "ArtistSort").InnerText)
      JapaneseArtistName = jName
      JapaneseTitle = jTitle
      Title = SortableString.Create((songInfo.Item "SongDisplayName").InnerText, (songInfo.Item "SongDisplayNameSort").InnerText)
      AlbumName = SortableString.Create((songInfo.Item "Album").InnerText, (songInfo.Item "AlbumSort").InnerText)
      Year = year
      AlbumArtFile = (docEl.Item "AlbumArtPath").InnerText
      AudioFile = { Path = audioPath; Volume = float (docEl.Item "Volume").InnerText }
      AudioPreviewFile = { Path = previewPath; Volume = float (docEl.Item "PreviewVolume").InnerText }
      Arrangements = arrangements
      Tones = tones }
    |> DLCProject.toAbsolutePaths (Path.GetDirectoryName templatePath) 
