module Rocksmith2014.DLCProject.ToolkitImporter

open System
open System.IO
open System.Xml
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Rocksmith2014.Common.Manifest

let [<Literal>] AggregateGraphNs = "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.DLCPackage.AggregateGraph"
let [<Literal>] ToneNs = "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.DLCPackage.Manifest2014.Tone"

let private optionalString (node: XmlNode) name =
    node.Item name
    |> Option.ofObj
    |> Option.bind (fun x -> Option.ofString x.InnerText)

/// Returns the contents of an arrangement's name node.
let private getName (arrNode: XmlNode) =
    // The tag is Name in older template files
    let n = arrNode.Item "ArrangementName"
    (if isNull n then arrNode.Item "Name" else n).InnerText

/// Returns the inner text of a child node with the given name.
let private itemText (root: XmlNode) name = root.Item(name).InnerText

/// Imports an instrumental arrangement.
let private importInstrumental (xmlFile: string) (arr: XmlNode) =
    let priority =
        try
            let bonusArr = itemText arr "BonusArr" = "true"

            // "Properties" in the tag name is misspelled
            match Option.ofObj (arr.Item "ArrangementPropeties") with
            | None ->
                // Arrangement properties do not exist in old files
                if bonusArr then ArrangementPriority.Bonus else ArrangementPriority.Main
            | Some arrProp ->
                let tryGetPriority ns =
                    let represent =
                        let r = arr.Item "Represent"
                        // The Represent tag is not present in old files
                        notNull r && r.InnerText = "true"
                    if represent || arrProp.Item("Represent", ns).InnerText = "1" then
                        ArrangementPriority.Main
                    elif bonusArr || arrProp.Item("BonusArr", ns).InnerText = "1" then
                        ArrangementPriority.Bonus
                    else
                        ArrangementPriority.Alternative

                // The XML namespace was renamed at some point.
                try tryGetPriority "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.XML"
                with _ -> tryGetPriority "http://schemas.datacontract.org/2004/07/RocksmithToolkitLib.Xml"
        with _ -> ArrangementPriority.Main

    let name = getName arr |> ArrangementName.Parse

    let tuning =
        arr.Item("TuningStrings").ChildNodes
        |> Seq.cast<XmlNode>
        |> Seq.map (fun x -> int16 x.InnerText)
        |> Seq.toArray

    let tones =
        [ for t in 'A'..'D' -> Option.ofString (itemText arr (sprintf "Tone%c" t)) ]
        |> List.choose id

    { XML = xmlFile
      Name = name
      RouteMask = RouteMask.Parse(itemText arr "RouteMask")
      Priority = priority
      ScrollSpeed = float (itemText arr "ScrollSpeed") / 10.
      BassPicked = name = ArrangementName.Bass && itemText arr "PluckedType" <> "NotPicked"
      Tuning = tuning
      TuningPitch = float (itemText arr "TuningPitch")
      BaseTone = itemText arr "ToneBase"
      Tones = tones
      MasterID = int (itemText arr "MasterId")
      PersistentID = Guid.Parse(itemText arr "Id")
      CustomAudio = None }
    |> Instrumental

/// Imports a vocals arrangement.
let private importVocals (xmlFile: string) (arr: XmlNode) =
    let isJapanese = (getName arr = "JVocals")

    let customFont =
        // In the current version (2.9.2.1) of the Toolkit,
        // the tag name for the custom font texture is LyricsArtPath
        optionalString arr "LyricsArtPath"
        // In an earlier version it was LyricArt
        |> Option.orElseWith (fun () -> optionalString arr "LyricArt")
        |> Option.orElseWith (fun () ->
            // In an earlier version there was the glyph definitions tag
            Option.ofObj (arr.Item "GlyphDefinitions")
            // ...which was misspelled in an earlier version
            |> Option.orElseWith (fun () -> Option.ofObj (arr.Item "GlyphDefinitons")) // sic
            |> Option.bind (fun glyphDefs ->
                if glyphDefs.IsEmpty then
                    None
                else
                    // Converts "path\to\x.glyphs.xml" to "x.dds"
                    Some (Path.ChangeExtension(Path.GetFileNameWithoutExtension glyphDefs.InnerText, "dds"))))
        
    { XML = xmlFile
      Japanese = isJapanese 
      CustomFont = customFont
      MasterID = int (itemText arr "MasterId")
      PersistentID = Guid.Parse(itemText arr "Id") }
    |> Vocals

/// Imports an arrangement from the Toolkit template.
let private importArrangement (arr: XmlNode) =
    let xml =
        arr.Item("SongXml")
           .Item("File", AggregateGraphNs)
           .InnerText

    match itemText arr "ArrangementType" with
    | "Guitar" | "Bass" -> importInstrumental xml arr
    | "Vocal" -> importVocals xml arr
    | "ShowLight" -> Showlights { XML = xml }
    | unknown -> failwith (sprintf "Unknown arrangement type: %s" unknown)

/// Converts a Toolkit template from the given path into a DLCProject.
let import (templatePath: string) =
    let doc = XmlDocument()
    doc.Load(templatePath)
    let docEl = doc.DocumentElement

    if docEl.Name <> "DLCPackageData" then failwith "Not a valid Toolkit template file."

    let songInfo = docEl.Item "SongInfo"
    let year =
        match Int32.TryParse(itemText songInfo "SongYear") with
        | true, year -> year
        | false, _ -> DateTime.Now.Year

    let audioPath = itemText docEl "OggPath"
    let previewPath =
        let fn = Path.GetFileNameWithoutExtension audioPath
        let prevFile = fn + "_preview" + (Path.GetExtension audioPath)
        let prevPath = Path.Combine (Path.GetDirectoryName templatePath, prevFile)
        if File.Exists prevPath then prevFile else String.Empty

    let arrangements =
        docEl.Item("Arrangements").ChildNodes
        |> Seq.cast<XmlNode>
        |> Seq.map importArrangement
        |> Seq.sortBy Arrangement.sorter
        |> Seq.toList

    let tones =
        docEl.Item("TonesRS2014").ChildNodes
        |> Seq.cast<XmlNode>
        |> Seq.map (Tone.importXml (Some ToneNs))
        |> Seq.toList

    let version =
        // There is no ToolkitInfo tag in older template files
        match docEl.Item "ToolkitInfo" with
        | null -> itemText docEl "PackageVersion"
        | tkInfo -> itemText tkInfo "PackageVersion"

    { Version = version
      DLCKey = itemText docEl "Name"
      ArtistName = { Value = itemText songInfo "Artist"; SortValue = itemText songInfo "ArtistSort" }
      JapaneseArtistName = optionalString songInfo "JapaneseArtistName"
      JapaneseTitle = optionalString songInfo "JapaneseSongName"
      Title = { Value = itemText songInfo "SongDisplayName"; SortValue = itemText songInfo "SongDisplayNameSort" }
      AlbumName = { Value = itemText songInfo "Album"; SortValue = itemText songInfo "AlbumSort" }
      Year = year
      AlbumArtFile = itemText docEl "AlbumArtPath"
      AudioFile = { Path = audioPath; Volume = float (itemText docEl "Volume") }
      AudioPreviewFile = { Path = previewPath; Volume = float (itemText docEl "PreviewVolume") }
      AudioPreviewStartTime = None
      PitchShift = None
      Arrangements = arrangements
      Tones = tones }
    |> DLCProject.toAbsolutePaths (Path.GetDirectoryName templatePath) 
