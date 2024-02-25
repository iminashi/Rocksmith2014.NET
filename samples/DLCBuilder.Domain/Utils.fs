module DLCBuilder.Utils

open System
open System.Diagnostics
open System.IO
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Rocksmith2014.PSARC
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

let createExceptionInfoString (ex: exn) =
    let exnInfo (e: exn) =
        $"{e.GetType().Name}: {e.Message}\n{e.StackTrace}"

    match ex.InnerException with
    | null ->
        exnInfo ex
    | innerEx ->
        $"{exnInfo ex}\n\nInner exception:\n{exnInfo innerEx}"

/// Imports tones from a PSARC file.
let importTonesFromPSARC (psarcPath: string) =
    async {
        use psarc = PSARC.OpenFile(psarcPath)

        let! jsons =
            psarc.Manifest
            |> Seq.filter (String.endsWith "json")
            |> Seq.map (fun j -> async { return! psarc.GetEntryStream(j) |> Async.AwaitTask })
            |> Async.Sequential

        let! manifests =
            jsons
            |> Array.map (fun data ->
                async {
                    try
                        try
                            let! manifest = (Manifest.fromJsonStream data).AsTask() |> Async.AwaitTask
                            return Some(Manifest.getSingletonAttributes manifest)
                        finally
                            data.Dispose()
                    with _ ->
                        return None
                })
            |> Async.Parallel

        return
            manifests
            |> Array.choose (Option.bind (fun a -> Option.ofObj a.Tones))
            |> Array.concat
            |> Array.distinctBy (fun x -> x.Key)
            |> Array.filter (fun x -> notNull x.GearList.Amp)
            |> Array.map Tone.fromDto
    }

/// Creates the path for the preview audio from the main audio path.
let previewPathFromMainAudio (audioPath: string) =
    let dir = Path.GetDirectoryName(audioPath)
    let fn = Path.GetFileNameWithoutExtension(audioPath)
    let ext = Path.GetExtension(audioPath)
    Path.Combine(dir, $"{fn}_preview{ext}")

/// Checks an arrangement for issues.
let checkArrangement arrangement =
    match arrangement with
    | Instrumental inst ->
        InstrumentalArrangement.Load(inst.XmlPath)
        |> ArrangementChecker.checkInstrumental
    | Vocals { CustomFont = font; XmlPath = xml } ->
        Vocals.Load(xml)
        |> ArrangementChecker.checkVocals font.IsSome
    | Showlights sl ->
        ShowLights.Load(sl.XmlPath)
        |> ArrangementChecker.checkShowlights
        |> Option.toList

/// Checks the project's arrangements for issues.
let checkArrangements (project: DLCProject) (progress: IProgress<float>) =
    let length = float project.Arrangements.Length

    project.Arrangements
    |> List.mapi (fun i arr ->
        let result = checkArrangement arr
        progress.Report(float (i + 1) / length * 100.)
        Arrangement.getId arr, result)
    |> Map.ofList

/// Adds descriptors to tones that have none.
let addDescriptors (tone: Tone) =
    let descs =
        tone.ToneDescriptors
        |> Option.ofArray
        |> Option.defaultWith (fun () ->
            ToneDescriptor.getDescriptionsOrDefault tone.Name
            |> Array.map (fun x -> x.UIName))

    { tone with
        ToneDescriptors = descs
        SortOrder = None
        NameSeparator = " - " }

/// Converts the project's audio and preview audio files to wem.
let convertAudio cliPath project =
    async {
        let files = [| project.AudioFile.Path; project.AudioPreviewFile.Path |]
        do! files
            |> Array.map (Wwise.convertToWem cliPath)
            |> Async.Parallel
            |> Async.Ignore

        return files
    }

/// Removes the item at the index from the array and shifts the subsequent items towards index zero by one.
let removeAndShift (index: int) array =
    let arr = Array.copy array
    for i = index to arr.Length - 2 do
        arr[i] <- arr[i + 1]
    arr[arr.Length - 1] <- None
    arr

/// Adds the path's default tone for arrangements whose base tones have no definitions.
let addDefaultTonesIfNeeded (project: DLCProject) =
    let neededTones =
        project.Arrangements
        |> List.choose (function
            | Instrumental i when not (project.Tones |> List.exists (fun t -> t.Key = i.BaseTone)) ->
                Some(i.BaseTone, i.RouteMask)
            | _ ->
                None)
        |> List.distinctBy fst
        |> List.map (fun (key, routeMask) ->
            let tone =
                match routeMask with
                | RouteMask.Lead -> DefaultTones.Lead.Value
                | RouteMask.Bass -> DefaultTones.Bass.Value
                | _ -> DefaultTones.Rhythm.Value

            { tone with Key = key })

    { project with Tones = neededTones @ project.Tones }

/// Adds metadata to the project for missing metadata fields.
let addMetadata (md: MetaData) (charterName: string) (project: DLCProject) =
    let artistNameNotSet = project.ArtistName.IsEmpty
    let titleNotSet = project.Title.IsEmpty
    let shouldUpdateYear =
        md.AlbumYear > 0
        && artistNameNotSet
        && titleNotSet
        && project.Year = DLCProject.Empty.Year

    { project with
        DLCKey =
            if String.IsNullOrWhiteSpace(project.DLCKey) then
                DLCKey.create charterName md.ArtistName md.Title
            else
                project.DLCKey
        ArtistName =
            if artistNameNotSet then
                // Ignore the sort value from the XML
                SortableString.Create(md.ArtistName)
            else
                project.ArtistName
        Title =
            if titleNotSet then
                SortableString.Create(md.Title, md.TitleSort)
            else
                project.Title
        AlbumName =
            if project.AlbumName.IsEmpty then
                SortableString.Create(md.AlbumName, md.AlbumNameSort)
            else
                project.AlbumName
        Year =
            if shouldUpdateYear then md.AlbumYear else project.Year }

/// Starts the given path or URL using the operating system shell.
let openWithShell pathOrUrl =
    ProcessStartInfo(pathOrUrl, UseShellExecute = true)
    |> Process.Start
    |> ignore

/// Removes the item at the given index from the list.
/// Returns an index that is within the new list or -1 if it is empty.
let removeSelected initialList index =
    let list = List.removeAt index initialList
    let newSelectedIndex = min index (list.Length - 1)
    list, newSelectedIndex

/// Removes DD from the arrangements.
let removeDD (instrumentals: (string * InstrumentalArrangement) list) =
    instrumentals
    |> List.map (fun (path, inst) ->
        async {
            do! inst.RemoveDD(false) |> Async.AwaitTask
            inst.Save(path)
        })
    |> Async.Sequential
    |> Async.Ignore

/// Moves the item in the list with the given index up or down.
let moveSelected dir selectedIndex (list: List<_>) =
    match selectedIndex with
    | -1 ->
        list, selectedIndex
    | index ->
        let selected = list[index]
        let change = match dir with Up -> -1 | Down -> 1
        let insertPos = index + change
        if insertPos >= 0 && insertPos < list.Length then
            List.removeAt index list
            |> List.insertAt insertPos selected, insertPos
        else
            list, selectedIndex

/// Converts the projects audio files to wav or ogg files.
let convertProjectAudioFromWem conv project =
    let convert =
        match conv with
        | ToOgg -> Conversion.wemToOgg
        | ToWav -> Conversion.wemToWav

    project
    |> DLCProject.getAudioFiles
    |> Seq.iter (fun { Path = path } -> convert path)

/// Determines the path to the preview file from the main audio.
let determinePreviewPath audioFilePath =
    let previewPath = previewPathFromMainAudio audioFilePath

    let alternativePath =
        match previewPath with
        | HasExtension ".wav" ->
            Path.ChangeExtension(previewPath, "ogg")
        | _ ->
            Path.ChangeExtension(previewPath, "wav")

    if File.Exists(previewPath) then
        previewPath
    elif File.Exists(alternativePath) then
        alternativePath
    else
        String.Empty

let createLyricsString (lyrics: Vocal seq) =
    let skipLast (v: Vocal) = v.Lyric.Substring(0, v.Lyric.Length - 1)
    let revJoin (sep: char) (list: string list) = String.Join(sep, List.rev list)
    let revConcat = List.rev >> String.Concat

    (([], [], []), lyrics)
    ||> Seq.fold (fun (word, line, lines) elem ->
        match elem.Lyric with
        | EndsWith "-" ->
            skipLast elem :: word, line, lines
        | EndsWith "+" ->
            let w = skipLast elem :: word
            let l = revConcat w :: line
            [], [], revJoin ' ' l :: lines
        | _ ->
            let w = elem.Lyric :: word
            [], revConcat w :: line, lines)
    |> (fun (_, line, res) ->
        let res = if not line.IsEmpty then revJoin ' ' line :: res else res
        revJoin '\n' res)

let startProcess path args =
    let startInfo = ProcessStartInfo(FileName = path, Arguments = args)
    use p = new Process(StartInfo = startInfo)
    p.Start() |> ignore

let isNumberGreaterThanZero (input: string) =
    let parsed, number = Int32.TryParse(input)
    parsed && number > 0
