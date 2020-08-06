module TestApp.Tools

open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI.DSL
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Threading
open Rocksmith2014.Common
open Rocksmith2014.SNG
open Rocksmith2014.Conversion
open Rocksmith2014.PSARC
open Rocksmith2014.DLCProject.Manifest
open Rocksmith2014.XML
open Rocksmith2014.DLCProject
open System.Threading.Tasks
open System.IO
open System
open System.Diagnostics

let project = 
    { DLCKey = "dummy"
      AppID = 123456
      ArtistName = "Artist"
      ArtistNameSort = "Artist"
      JapaneseArtistName = None
      JapaneseTitle = None
      Title = "Title"
      TitleSort = "Title"
      AlbumName = "Album"
      AlbumNameSort = "Album"
      Year = 1999
      AlbumArtFile = "cover.dds"
      AudioFile = "audio.wem"
      AudioPreviewFile = "preview.wem"
      CentOffset = 0.
      Arrangements = []
      Tones = [] }

let private window =
    lazy ((Application.Current.ApplicationLifetime :?> ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime).MainWindow)

let sngFilters =
    let filter = FileDialogFilter(Extensions = ResizeArray(seq { "sng" }), Name = "SNG Files")
    ResizeArray(seq { filter })

let xmlFilters =
    let filter = FileDialogFilter(Extensions = ResizeArray(seq { "xml" }), Name = "XML Files")
    ResizeArray(seq { filter })

let psarcFilters =
    let filter = FileDialogFilter(Extensions = ResizeArray(seq { "psarc" }), Name = "PSARC Files")
    ResizeArray(seq { filter })

let openFileDialogSingle title filters dispatch = 
    Dispatcher.UIThread.InvokeAsync(
        fun () ->
            OpenFileDialog(Title = title, AllowMultiple = false, Filters = filters)
               .ShowAsync(window.Force())
               .ContinueWith(fun (t: Task<string[]>) -> 
                   match t.Result with
                   | [| file |] -> file |> dispatch
                   | _ -> ())
        ) |> ignore

let openFileDialogMulti title filters dispatch = 
    Dispatcher.UIThread.InvokeAsync(
        fun () ->
            OpenFileDialog(Title = title, AllowMultiple = true, Filters = filters)
               .ShowAsync(window.Force())
               .ContinueWith(fun (t: Task<string[]>) -> 
                   match t.Result with
                   | null | [||] -> ()
                   | files -> files |> dispatch)
        ) |> ignore

let openFolderDialog title dispatch = 
    Dispatcher.UIThread.InvokeAsync(
        fun () ->
            OpenFolderDialog(Title = title)
               .ShowAsync(window.Force())
               .ContinueWith(fun (t: Task<string>) -> 
                   match t.Result with
                   | null -> ()
                   | file -> file |> dispatch)
        ) |> ignore

let ofdSng = openFileDialogSingle "Select File" sngFilters
let ofdXml = openFileDialogSingle "Select File" xmlFilters
let ofdPsarc = openFileDialogSingle "Select File" psarcFilters
let ofdMultiXml = openFileDialogMulti "Select Files" xmlFilters
let ofod = openFolderDialog "Select Folder"

type State = { Status:string; Platform:Platform }

let init = { Status = ""; Platform = PC }

type Msg =
    | UnpackFile of file:string
    | ConvertVocalsSNGtoXML of file:string
    | ConvertVocalsXMLtoSNG of file:string
    | ConvertInstrumentalSNGtoXML of file:string
    | ConvertInstrumentalXMLtoSNG of file:string
    | BatchConvertToSng of files:string array
    | UnpackPSARC of file:string
    | PackDirectoryPSARC of path:string
    | TouchPSARC of file:string
    | CreateManifest of file:string
    | ExtractSNGtoXML of file:string
    //| RoundTrip of file:string
    | ChangePlatform of Platform

let convertFileToSng platform (fileName: string) =
    let target = Path.ChangeExtension(fileName, "sng")
    if fileName.Contains("vocal", StringComparison.OrdinalIgnoreCase) ||
       fileName.Contains("lyric", StringComparison.OrdinalIgnoreCase) then
        ConvertVocals.xmlFileToSng fileName target None platform
    else
        ConvertInstrumental.xmlFileToSng fileName target platform

let update (msg: Msg) (state: State) : State =
    try
        match msg with
        //| RoundTrip file ->
        //    SNGFile.readPacked file state.Platform
        //    |> SNGFile.savePacked (file + "re") state.Platform
        //    state
        
        | UnpackFile file ->
            SNGFile.unpackFile file state.Platform; state

        | ConvertVocalsSNGtoXML file ->
            let target = Path.ChangeExtension(file, "xml")
            ConvertVocals.sngFileToXml file target state.Platform
            state

        | ConvertVocalsXMLtoSNG file ->
            let target = Path.ChangeExtension(file, "sng")
            ConvertVocals.xmlFileToSng file target None state.Platform
            state

        | ConvertInstrumentalSNGtoXML file ->
            let targetFile = Path.ChangeExtension(file, "xml")
            ConvertInstrumental.sngFileToXml file targetFile state.Platform
            state

        | ConvertInstrumentalXMLtoSNG file ->
            let targetFile = Path.ChangeExtension(file, "sng")
            ConvertInstrumental.xmlFileToSng file targetFile state.Platform
            state

        | BatchConvertToSng files ->
            files
            |> Array.Parallel.iter (convertFileToSng state.Platform)
            state

        | UnpackPSARC file ->
            let dir = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file))
            Directory.CreateDirectory(dir) |> ignore
            use psarc = PSARC.ReadFile file
            psarc.ExtractFiles dir
            state

        | TouchPSARC file ->
            use psarc = PSARC.ReadFile file
            psarc.Edit({ Mode = InMemory; EncyptTOC = true }, ignore)
            state

        | PackDirectoryPSARC path ->
            let sw = Stopwatch.StartNew()
            PSARC.PackDirectory(path, path + ".psarc", true)
            sw.Stop()
            { state with Status = sprintf "Elapsed: %A" sw.Elapsed }

        | CreateManifest file ->
            let arrangement =
                { XML = file
                  ArrangementName = ArrangementName.Lead
                  RouteMask = RouteMask.Lead
                  ScrollSpeed = 13
                  MasterID = 12345
                  PersistentID = Guid.NewGuid() }

            let project = { project with Arrangements = [ Instrumental arrangement ] }

            let xml = InstrumentalArrangement.Load file
            let sng = ConvertInstrumental.xmlToSng xml
            let attr = AttributesCreation.createAttributes project (AttributesCreation.FromInstrumental (arrangement, sng))
            use target = File.Create(Path.ChangeExtension(file, "json"))
            Manifest.create [ attr ]
            |> Manifest.toJsonStream target
            |> Async.AwaitTask
            |> Async.RunSynchronously

            state

        | ExtractSNGtoXML file ->
            let targetDirectory = Path.Combine(Path.GetDirectoryName(file), "xml")
            Directory.CreateDirectory(targetDirectory) |> ignore
            use psarc = PSARC.ReadFile file
            let sngs =
                psarc.Manifest
                |> Seq.filter (fun n -> n.EndsWith "sng")
            let manifests =
                psarc.Manifest
                |> Seq.filter (fun n -> n.EndsWith "json")
            sngs
            |> Seq.iter (fun s ->
                let targetFile = Path.Combine(targetDirectory, Path.ChangeExtension(Path.GetFileName s, "xml"))
                let sng = using (MemoryStreamPool.Default.GetStream()) (fun data -> psarc.InflateFile(s, data); SNGFile.readStream data PC)

                if s.Contains "vocals" then
                    let vocals = ConvertVocals.sngToXml sng
                    Vocals.Save(targetFile, vocals)
                else
                    let attributes =
                        manifests
                        |> Seq.tryFind (fun m -> m.Contains(Path.GetFileNameWithoutExtension s))
                        |> Option.map (fun m ->
                            use mData = MemoryStreamPool.Default.GetStream()
                            psarc.InflateFile(m, mData)
                            Manifest.fromJsonStream(mData).AsTask()
                            |> Async.AwaitTask
                            |> Async.RunSynchronously
                            |> Manifest.getSingletonAttributes
                        )
                    let xml = ConvertInstrumental.sngToXml attributes sng
                    xml.Save targetFile
                )
            state

        | ChangePlatform platform -> { state with Platform = platform }

    with e -> { state with Status = e.Message }

let view (state: State) dispatch =
    StackPanel.create [
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

            //Button.create [
            //    Button.onClick (fun _ ->  ofdSng (RoundTrip >> dispatch))
            //    Button.content "Round-trip Packed SNG File..."
            //]

            Button.create [
                Button.onClick (fun _ -> ofdSng (UnpackFile >> dispatch))
                Button.content "Unpack SNG File..."
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

            Button.create [
                Button.onClick (fun _ -> ofdPsarc (TouchPSARC >> dispatch))
                Button.content "Touch PSARC File..."
            ]

            Button.create [
                Button.onClick (fun _ -> ofdPsarc (UnpackPSARC >> dispatch))
                Button.content "Unpack PSARC File..."
            ]

            Button.create [
                Button.onClick (fun _ -> ofod (PackDirectoryPSARC >> dispatch))
                Button.content "Pack a Directory into PSARC File..."
            ]

            Button.create [
                Button.onClick (fun _ -> ofdXml (CreateManifest >> dispatch))
                Button.content "Create Manifest from XML File..."
            ]

            Button.create [
                Button.onClick (fun _ -> ofdPsarc (ExtractSNGtoXML >> dispatch))
                Button.content "Convert SNG to XML from PSARC..."
            ]

            TextBlock.create [
                TextBlock.fontSize 26.0
                TextBlock.textWrapping TextWrapping.Wrap
                TextBlock.verticalAlignment VerticalAlignment.Center
                TextBlock.horizontalAlignment HorizontalAlignment.Center
                TextBlock.text (string state.Status)
            ]
        ]
    ]
