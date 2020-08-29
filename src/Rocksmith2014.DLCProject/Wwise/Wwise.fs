module Rocksmith2014.DLCProject.Wwise

open System
open System.IO
open System.Reflection
open System.IO.Compression
open System.Diagnostics
open System.Runtime.InteropServices
open Microsoft.Extensions.FileProviders
open Rocksmith2014.Common.BinaryWriters
open Rocksmith2014.Common.Interfaces

let rec private cleanDirectory (path: string) =
    Directory.EnumerateFiles path |> Seq.iter File.Delete
    let subDirs = Directory.EnumerateDirectories path
    subDirs |> Seq.iter cleanDirectory
    subDirs |> Seq.iter Directory.Delete

let private getCLIPath() =
    let cliPath =
        if RuntimeInformation.IsOSPlatform OSPlatform.Windows then
            let wwiseRoot = Environment.GetEnvironmentVariable "WWISEROOT"
            if String.IsNullOrEmpty wwiseRoot then
                failwith "Failed to read WWISEROOT environment variable."
            elif not <| wwiseRoot.Contains "2019" then
                failwith "Wwise version must be 2019."
            Path.Combine(wwiseRoot, @"Authoring\x64\Release\bin\WwiseConsole.exe")
        elif RuntimeInformation.IsOSPlatform OSPlatform.OSX then
            // TODO
            ".../Authoring/Wwise.app/Contents/Tools/WwiseConsole.sh"
        else
            failwith "Not supported."

    if not <| File.Exists cliPath then
        failwith "Could not find Wwise Console executable."

    cliPath

let private loadTemplate (sourcePath: string) =
    let templateDir = Path.Combine(Path.GetTempPath(), "RS2WwiseConv")
    if Directory.Exists templateDir then
        cleanDirectory templateDir
    else
        Directory.CreateDirectory templateDir |> ignore

    let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
    use templateZip = embeddedProvider.GetFileInfo("res/wwise2019.zip").CreateReadStream()
    using (new ZipArchive(templateZip)) (fun zip -> zip.ExtractToDirectory templateDir)

    let orgSfxDir = Path.Combine(templateDir, "Originals", "SFX")

    let previewFile =
        let dir = Path.GetDirectoryName sourcePath
        let fn = Path.GetFileNameWithoutExtension sourcePath
        sprintf "%s_%s.wav" (Path.Combine (dir,fn)) "preview"

    File.Copy (previewFile, Path.Combine(orgSfxDir, "Audio_preview.wav"), true)
    File.Copy (sourcePath, Path.Combine(orgSfxDir, "Audio.wav"), true)

    templateDir

let private fixHeader (fileName: string) =
    use file = File.Open(fileName, FileMode.Open, FileAccess.Write)
    let writer = LittleEndianBinaryWriter(file) :> IBinaryWriter
    file.Seek(40L, SeekOrigin.Begin) |> ignore
    writer.WriteUInt32 3u

let private copyWemFiles (destPath: string) (templateDir: string) =
    let wemPath = Path.Combine (templateDir, ".cache", "Windows", "SFX")
    let wemPathInfo = DirectoryInfo(wemPath)

    let wemPaths =
        wemPathInfo.EnumerateFiles("*")
        |> Seq.filter (fun x -> x.FullName.EndsWith "wem")
        |> Seq.toArray
    if wemPaths.Length < 2 then
        failwith "Could not find converted Wwise audio and preview audio files."

    for path in wemPaths do
        let destFile =
            if path.Name.Contains "preview" then
                sprintf "%s_preview.wem" destPath
            else
                sprintf "%s.wem" destPath
        File.Copy(path.FullName, destFile, true)
        fixHeader destFile

let convertToWem (sourcePath: string) (destPath: string) =
    let cliPath = getCLIPath()
    let templateDir = loadTemplate sourcePath

    let template = Path.Combine(templateDir, "Template.wproj")
    let args = sprintf "generate-soundbank \"%s\" --platform \"Windows\" --language \"English(US)\"" template

    let startInfo = ProcessStartInfo(FileName = cliPath, Arguments = args)
    use wwiseCli = new Process(StartInfo = startInfo)
    wwiseCli.Start() |> ignore
    wwiseCli.WaitForExit()

    copyWemFiles destPath templateDir
    cleanDirectory templateDir
