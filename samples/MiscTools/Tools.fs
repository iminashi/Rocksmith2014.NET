module TestApp.Tools

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Threading
open Rocksmith2014.Audio
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.SNG
open Rocksmith2014.Conversion
open Rocksmith2014.PSARC
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.Manifest
open Rocksmith2014.DLCProject.DDS
open System.Threading.Tasks
open System.IO
open System
open Elmish

let project =
    { Version = "1.0"
      DLCKey = "dummy"
      ArtistName = SortableString.Create "Artist"
      JapaneseArtistName = None
      JapaneseTitle = None
      Title = SortableString.Create "Title"
      AlbumName = SortableString.Create "Album"
      Year = 1999
      AlbumArtFile = "cover.dds"
      AudioFile = { Path = "audio.wem"; Volume = 12. }
      AudioPreviewFile = { Path = "preview.wem"; Volume = 12. }
      AudioPreviewStartTime = None
      PitchShift = None
      Arrangements = []
      Tones = [] }

let private window =
    lazy ((Application.Current.ApplicationLifetime :?> ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime).MainWindow)

let createFilters (extensions: string seq) name =
    let filter = FileDialogFilter(Extensions = ResizeArray(extensions), Name = name)
    ResizeArray(seq { filter })

let sngFilters = createFilters (seq { "sng" }) "SNG Files"
let xmlFilters = createFilters (seq { "xml" }) "XML Files"
let psarcFilters = createFilters (seq { "psarc" }) "PSARC Files"
let wemFilters = createFilters (seq { "wem" }) "Wwise Audio Files"
let bnkFilters = createFilters (seq { "bnk" }) "Sound Bank Files"

let openFileDialogSingle title filters dispatch =
    Dispatcher.UIThread.InvokeAsync(fun () ->
        OpenFileDialog(Title = title, AllowMultiple = false, Filters = filters)
           .ShowAsync(window.Force())
           .ContinueWith(fun (t: Task<string[]>) ->
               match t.Result with
               | [| file |] -> dispatch file
               | _ -> ())
    ) |> ignore

let openFileDialogMulti title filters dispatch =
    Dispatcher.UIThread.InvokeAsync(fun () ->
        OpenFileDialog(Title = title, AllowMultiple = true, Filters = filters)
           .ShowAsync(window.Force())
           .ContinueWith(fun (t: Task<string[]>) ->
               match t.Result with
               | null | [||] -> ()
               | files -> dispatch files)
    ) |> ignore

let openFolderDialog title dispatch =
    Dispatcher.UIThread.InvokeAsync(fun () ->
        OpenFolderDialog(Title = title)
           .ShowAsync(window.Force())
           .ContinueWith(fun (t: Task<string>) ->
               match t.Result with
               | null -> ()
               | file -> dispatch file)
    ) |> ignore

let ofdSng = openFileDialogSingle "Select File" sngFilters
let ofdXml = openFileDialogSingle "Select File" xmlFilters
let ofdPsarc = openFileDialogSingle "Select File" psarcFilters
let ofdPsarcs = openFileDialogMulti "Select Files" psarcFilters
let ofdWem = openFileDialogSingle "Select File" wemFilters
let ofdBnk = openFileDialogSingle "Select File" bnkFilters
let ofdAll = openFileDialogSingle "Select File" null
let ofdMultiXml = openFileDialogMulti "Select Files" xmlFilters
let ofod = openFolderDialog "Select Folder"

type State = { Status:string; Platform:Platform; ConvertAudio: bool; ConvertSNG : bool }

let init () = { Status = ""; Platform = PC; ConvertAudio = true; ConvertSNG = false }, Cmd.none

type Msg =
    | UnpackSNGFile of file:string
    | PackSNGFile of file:string
    | ConvertVocalsSNGtoXML of file:string
    | ConvertVocalsXMLtoSNG of file:string
    | ConvertInstrumentalSNGtoXML of file:string
    | ConvertInstrumentalXMLtoSNG of file:string
    | BatchConvertToSng of files:string array
    | UnpackPSARC of file:string
    | PackDirectoryPSARC of path:string
    | ConvertPCtoMac of path:string
    | CreateManifest of file:string
    | ExtractSNGtoXML of files:string array
    | ChangePlatform of Platform
    | SetConvertAudio of bool
    | SetConvertSNG of bool
    | ConvertToDDS of file:string
    | GenerateSoundBank of file:string
    | ReadVolume of file:string
    | DecryptProfile of file:string
    | CheckXml of file:string
    | Error of ex:Exception

let convertFileToSng platform (fileName: string) =
    let target = Path.ChangeExtension(fileName, "sng")
    if fileName.Contains("vocal", StringComparison.OrdinalIgnoreCase) ||
       fileName.Contains("lyric", StringComparison.OrdinalIgnoreCase) then
        ConvertVocals.xmlFileToSng fileName target None platform
    else
        ConvertInstrumental.xmlFileToSng fileName target platform

let update (msg: Msg) (state: State) : State * Cmd<Msg> =
    try
        match msg with
        | UnpackSNGFile file ->
            let t () = SNG.unpackFile file state.Platform
            state, Cmd.OfAsync.attempt t () Error

        | PackSNGFile file ->
            let t () = async {
                use plain = File.OpenRead file
                let targetPath = file.Replace("_unpacked", "")
                use target = File.Create targetPath
                do! SNG.pack plain target state.Platform }
            state, Cmd.OfAsync.attempt t () Error

        | ConvertVocalsSNGtoXML file ->
            let target = Path.ChangeExtension(file, "xml")
            let t () = ConvertVocals.sngFileToXml file target state.Platform
            state, Cmd.OfAsync.attempt t () Error

        | ConvertVocalsXMLtoSNG file ->
            let target = Path.ChangeExtension(file, "sng")
            let t () = ConvertVocals.xmlFileToSng file target None state.Platform
            state, Cmd.OfAsync.attempt t () Error

        | ConvertInstrumentalSNGtoXML file ->
            let targetFile = Path.ChangeExtension(file, "xml")
            let t () = ConvertInstrumental.sngFileToXml file targetFile state.Platform
            state, Cmd.OfAsync.attempt t () Error

        | ConvertInstrumentalXMLtoSNG file ->
            let targetFile = Path.ChangeExtension(file, "sng")
            let t () = ConvertInstrumental.xmlFileToSng file targetFile state.Platform
            state, Cmd.OfAsync.attempt t () Error

        | BatchConvertToSng files ->
            let t () = 
                files
                |> Array.map (convertFileToSng state.Platform)
                |> Async.Parallel
                |> Async.Ignore
            state, Cmd.OfAsync.attempt t () Error

        | UnpackPSARC file ->
            let targetDirectory = Path.Combine(Path.GetDirectoryName file, Path.GetFileNameWithoutExtension file)
            Directory.CreateDirectory targetDirectory |> ignore
            let t () = async {
                let platform = Platform.fromPackageFileName file
                use psarc = PSARC.ReadFile file
                do! psarc.ExtractFiles targetDirectory

                if state.ConvertAudio then
                    Directory.EnumerateFiles(targetDirectory, "*.wem", SearchOption.AllDirectories) 
                    |> Seq.iter Conversion.wemToOgg

                if state.ConvertSNG then
                    let sngPaths =
                        Directory.EnumerateFiles(targetDirectory, "*.sng", SearchOption.AllDirectories) 
                        |> List.ofSeq

                    let manifestPaths =
                        Directory.EnumerateFiles(targetDirectory, "*.json", SearchOption.AllDirectories) 
                        |> List.ofSeq

                    do! sngPaths
                        |> List.map (fun path -> async {
                            let arrPath = Path.Combine(targetDirectory, "songs", "arr")
                            let targetPath = Path.Combine(arrPath, Path.ChangeExtension(Path.GetFileName path, "xml"))
                            let! sng = SNG.readPackedFile path platform
                            if path.Contains "vocals" then
                                let vocals = ConvertVocals.sngToXml sng
                                Vocals.Save(targetPath, vocals)
                            else
                                let attributes =
                                    manifestPaths
                                    |> List.tryFind (fun m -> Path.GetFileNameWithoutExtension m = Path.GetFileNameWithoutExtension path)
                                    |> Option.map (fun m ->
                                        async {
                                            let! manifest = Manifest.fromJsonFile m
                                            return Manifest.getSingletonAttributes manifest }
                                        |> Async.RunSynchronously)
                                let xml = ConvertInstrumental.sngToXml attributes sng
                                xml.Save targetPath })
                        |> Async.Parallel
                        |> Async.Ignore }
            state, Cmd.OfAsync.attempt t () Error

        | PackDirectoryPSARC path ->
            let t () = PSARC.PackDirectory(path, path + ".psarc", true)

            state, Cmd.OfAsync.attempt t () Error

        | ConvertPCtoMac file ->
            if not <| String.endsWith "_p.psarc" file then
                { state with Status = "Filename has to end in _p.psarc." }, Cmd.none
            else
                let t () = async {
                    let targetFile = file.Replace("_p.psarc", "_m.psarc")
                    File.Copy(file, targetFile)
                    use psarc = PSARC.ReadFile targetFile
                    do! PlatformConverter.pcToMac psarc }

                state, Cmd.OfAsync.attempt t () Error

        | CreateManifest file ->
            let arrangement =
                { XML = file
                  Name = ArrangementName.Lead
                  RouteMask = RouteMask.Lead
                  Priority = ArrangementPriority.Main
                  TuningPitch = 440.
                  Tuning = [||]
                  BaseTone = String.Empty
                  Tones = []
                  ScrollSpeed = 1.3
                  BassPicked = false
                  MasterID = 12345
                  PersistentID = Guid.NewGuid()
                  CustomAudio = None }

            let project = { project with Arrangements = [ Instrumental arrangement ] }

            let sng =
                InstrumentalArrangement.Load file
                |> ConvertInstrumental.xmlToSng
            let attr = AttributesCreation.createAttributes project (AttributesCreation.FromInstrumental (arrangement, sng))
            let t () = async {
                use target = File.Create(Path.ChangeExtension(file, "json"))
                do! Manifest.create attr
                    |> Manifest.toJsonStream target }

            state, Cmd.OfAsync.attempt t () Error

        | ExtractSNGtoXML files -> 
            let t () = async {
                for file in files do
                    let targetDirectory = Path.Combine(Path.GetDirectoryName(file), "xml")
                    Directory.CreateDirectory(targetDirectory) |> ignore

                    let platform = Platform.fromPackageFileName file
                    use psarc = PSARC.ReadFile file

                    let! sngs =
                        psarc.Manifest
                        |> List.filter (String.endsWith "sng")
                        |> List.map (fun x -> async {
                            use! stream = psarc.GetEntryStream x
                            let! sng = SNG.fromStream stream platform
                            return {| File = x; SNG = sng |} })
                        |> Async.Sequential

                    let! manifests =
                        psarc.Manifest
                        |> List.filter (String.endsWith "json")
                        |> List.map (fun x -> async {
                            use! stream = psarc.GetEntryStream x
                            let! manifest = Manifest.fromJsonStream stream
                            return {| File = x; Manifest = manifest |} })
                        |> Async.Sequential

                    sngs
                    |> Array.Parallel.iter (fun s ->
                        let targetFile = Path.Combine(targetDirectory, Path.ChangeExtension(Path.GetFileName s.File, "xml"))
                        if s.File.Contains "vocals" then
                            let vocals = ConvertVocals.sngToXml s.SNG
                            Vocals.Save(targetFile, vocals)
                        else
                            let attributes =
                                manifests
                                |> Seq.tryFind (fun m -> Path.GetFileNameWithoutExtension m.File = Path.GetFileNameWithoutExtension s.File)
                                |> Option.map (fun m -> Manifest.getSingletonAttributes m.Manifest)
                            let xml = ConvertInstrumental.sngToXml attributes s.SNG
                            xml.Save targetFile)
                }
            state, Cmd.OfAsync.attempt t () Error

        | ConvertToDDS file ->
            let targetPath = Path.ChangeExtension(file, "dds")
            use target = File.Create targetPath
            let options = { Resize = Resize(256,256); Compression = DXT1 }
            convertToDDS file target options
            state, Cmd.none

        | GenerateSoundBank file ->
            let target = Path.ChangeExtension(file, "bnk")
            use targetFile = File.Create target
            use audio = File.OpenRead file
            SoundBank.generate "test" audio targetFile -2.5f PC |> ignore
            state, Cmd.none

        | ReadVolume fileName ->
            let message =
                use file = File.OpenRead fileName
                match SoundBank.readVolume file PC with
                | Result.Error err -> err
                | Result.Ok vol -> sprintf "Volume: %f dB" vol

            { state with Status = message }, Cmd.none

        | DecryptProfile file ->
            let t () = async {
                use profFile = File.OpenRead file
                use targetFile = File.Create(file + ".json")
                do! Profile.decrypt profFile targetFile |> Async.Ignore }
            state, Cmd.OfAsync.attempt t () Error

        | ChangePlatform platform ->
            { state with Platform = platform }, Cmd.none

        | CheckXml file ->
            let status = 
                InstrumentalArrangement.Load file
                |> ArrangementChecker.checkInstrumental
                |> List.map (fun issue -> $"[{Utils.timeToString issue.TimeCode}] {issue.Type}")
                |> function
                | [] -> "No issues found."
                | messages -> String.Join("\n", messages)

            { state with Status = status }, Cmd.none

        | SetConvertAudio conv ->
            { state with ConvertAudio = conv}, Cmd.none

        | SetConvertSNG conv ->
            { state with ConvertSNG = conv}, Cmd.none

        | Error e ->
            { state with Status = e.Message }, Cmd.none

    with e -> { state with Status = e.Message }, Cmd.none

let view (state: State) dispatch =
    DockPanel.create [
        DockPanel.children [
            StackPanel.create [
                DockPanel.dock Dock.Left
                StackPanel.width 250.
                StackPanel.margin 5.0
                StackPanel.spacing 5.0
                StackPanel.children [
                    StackPanel.create [
                        StackPanel.orientation Orientation.Horizontal
                        StackPanel.horizontalAlignment HorizontalAlignment.Center
                        StackPanel.children [
                            RadioButton.create [
                                RadioButton.groupName "Platform"
                                RadioButton.content "PC"
                                RadioButton.margin (0., 0., 10., 0.)
                                RadioButton.isChecked (state.Platform = PC)
                                RadioButton.onIsPressedChanged (fun p -> if p then dispatch (ChangePlatform PC))
                            ]
                            RadioButton.create [
                                RadioButton.groupName "Platform"
                                RadioButton.content "Mac"
                                RadioButton.isChecked (state.Platform = Mac)
                                RadioButton.onIsPressedChanged (fun p -> if p then dispatch (ChangePlatform Mac))
                            ]
                        ]
                    ]

                    TextBlock.create [
                        TextBlock.fontSize 18.0
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                        TextBlock.text "SNG"
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdSng (UnpackSNGFile >> dispatch))
                        Button.content "Unpack File..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdSng (PackSNGFile >> dispatch))
                        Button.content "Pack File..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdSng (ConvertVocalsSNGtoXML >> dispatch))
                        Button.content "Convert Vocals SNG to XML..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdXml (ConvertVocalsXMLtoSNG >> dispatch))
                        Button.content "Convert Vocals XML to SNG..."
                    ]
            
                    Button.create [
                        Button.onClick (fun _ -> ofdSng (ConvertInstrumentalSNGtoXML >> dispatch))
                        Button.content "Convert Instrumental SNG to XML..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdXml (ConvertInstrumentalXMLtoSNG >> dispatch))
                        Button.content "Convert Instrumental XML to SNG..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdMultiXml (BatchConvertToSng >> dispatch))
                        Button.content "Batch Convert to SNG..."
                    ]

                    TextBlock.create [
                        TextBlock.fontSize 18.0
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                        TextBlock.text "PSARC"
                    ]

                    CheckBox.create [
                        CheckBox.content "Convert audio to ogg"
                        CheckBox.isChecked state.ConvertAudio
                        CheckBox.onChecked (fun _ -> true |> SetConvertAudio |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> SetConvertAudio |> dispatch)
                    ]

                    CheckBox.create [
                        CheckBox.content "Convert SNG to XML"
                        CheckBox.isChecked state.ConvertSNG
                        CheckBox.onChecked (fun _ -> true |> SetConvertSNG |> dispatch)
                        CheckBox.onUnchecked (fun _ -> false |> SetConvertSNG |> dispatch)
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdPsarc (UnpackPSARC >> dispatch))
                        Button.content "Unpack File..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofod (PackDirectoryPSARC >> dispatch))
                        Button.content "Pack a Directory into PSARC File..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdPsarcs (ExtractSNGtoXML >> dispatch))
                        Button.content "Convert SNG to XML from PSARC..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdPsarc (ConvertPCtoMac >> dispatch))
                        Button.content "Convert PC to Mac..."
                    ]

                    TextBlock.create [
                        TextBlock.fontSize 18.0
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                        TextBlock.text "Misc."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdXml (CreateManifest >> dispatch))
                        Button.content "Create Manifest from XML File..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdAll (ConvertToDDS >> dispatch))
                        Button.content "Convert an Image to DDS..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdWem (GenerateSoundBank >> dispatch))
                        Button.content "Generate Sound Bank..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdBnk (ReadVolume >> dispatch))
                        Button.content "Read Volume from Sound Bank..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdAll (DecryptProfile >> dispatch))
                        Button.content "Decrypt Profile..."
                    ]

                    Button.create [
                        Button.onClick (fun _ -> ofdXml (CheckXml >> dispatch))
                        Button.content "Check Instrumental XML..."
                    ]
                ]
            ]

            ScrollViewer.create [
                ScrollViewer.content (
                    TextBlock.create [
                        TextBlock.fontSize 22.0
                        TextBlock.textWrapping TextWrapping.Wrap
                        TextBlock.verticalAlignment VerticalAlignment.Center
                        TextBlock.horizontalAlignment HorizontalAlignment.Center
                        TextBlock.text (string state.Status)
                    ]
                )
            ]
        ]
    ]
