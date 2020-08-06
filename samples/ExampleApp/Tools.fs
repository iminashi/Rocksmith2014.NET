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
open Elmish

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

let init () = { Status = ""; Platform = PC }, Cmd.none

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
    | ChangePlatform of Platform
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
        | UnpackFile file ->
            let t () = SNG.unpackFile file state.Platform
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
            let dir = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file))
            Directory.CreateDirectory(dir) |> ignore
            let t () = async {
                use psarc = PSARC.ReadFile file
                do! psarc.ExtractFiles dir }
            state, Cmd.OfAsync.attempt t () Error

        | TouchPSARC file ->
            let t () = async {
                use psarc = PSARC.ReadFile file
                do! psarc.Edit({ Mode = InMemory; EncyptTOC = true }, ignore) }
            state, Cmd.OfAsync.attempt t () Error

        | PackDirectoryPSARC path ->
            let t () = PSARC.PackDirectory(path, path + ".psarc", true)

            state, Cmd.OfAsync.attempt t () Error

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
            let t () = async {
                use target = File.Create(Path.ChangeExtension(file, "json"))
                do! Manifest.create [ attr ]
                    |> Manifest.toJsonStream target
                    |> Async.AwaitTask }

            state, Cmd.OfAsync.attempt t () Error

        | ExtractSNGtoXML file -> 
            let targetDirectory = Path.Combine(Path.GetDirectoryName(file), "xml")
            Directory.CreateDirectory(targetDirectory) |> ignore

            let t () = async {
                use psarc = PSARC.ReadFile file

                let! sngs =
                    psarc.Manifest
                    |> Seq.filter (fun x -> x.EndsWith "sng")
                    |> Seq.map (fun x -> async {
                        use mem = MemoryStreamPool.Default.GetStream()
                        do! psarc.InflateFile(x, mem)
                        let! sng = SNG.fromStream mem PC
                        return {| File = x; SNG = sng |} })
                    |> Async.Sequential

                let! manifests =
                    psarc.Manifest
                    |> Seq.filter (fun x -> x.EndsWith "json")
                    |> Seq.map (fun x -> async {
                        use mem = MemoryStreamPool.Default.GetStream()
                        do! psarc.InflateFile(x, mem)
                        let! manifest = Manifest.fromJsonStream(mem).AsTask()
                        return {| File = x; Manifest = manifest |} })
                    |> Async.Sequential

                return sngs
                |> Array.Parallel.iter (fun s ->
                    let targetFile = Path.Combine(targetDirectory, Path.ChangeExtension(Path.GetFileName s.File, "xml"))
                    if s.File.Contains "vocals" then
                        let vocals = ConvertVocals.sngToXml s.SNG
                        Vocals.Save(targetFile, vocals)
                    else
                        let attributes =
                            manifests
                            |> Seq.tryFind (fun m -> m.File.Contains(Path.GetFileNameWithoutExtension s.File))
                            |> Option.map (fun m -> Manifest.getSingletonAttributes m.Manifest)
                        let xml = ConvertInstrumental.sngToXml attributes s.SNG
                        xml.Save targetFile
                    )
                }
            state, Cmd.OfAsync.attempt t () Error

        | ChangePlatform platform -> { state with Platform = platform }, Cmd.none

        | Error e -> { state with Status = e.Message }, Cmd.none

    with e -> { state with Status = e.Message }, Cmd.none

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
