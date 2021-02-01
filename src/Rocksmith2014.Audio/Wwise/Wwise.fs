module Rocksmith2014.Audio.Wwise

open System
open System.IO
open System.IO.Compression
open System.Reflection
open System.Diagnostics
open Microsoft.Extensions.FileProviders
open Rocksmith2014.Common
open Rocksmith2014.Common.BinaryWriters

/// Recursively removes files and subdirectories from a directory.
let rec private cleanDirectory (path: string) =
    Directory.EnumerateFiles path |> Seq.iter File.Delete
    let subDirs = Directory.EnumerateDirectories path
    subDirs |> Seq.iter cleanDirectory
    subDirs |> Seq.iter Directory.Delete

let private tryFindWwiseInstallation rootDir =
    match Path.Combine(rootDir, "Audiokinetic") with
    | dir when Directory.Exists dir ->
        dir
        |> Directory.EnumerateDirectories
        |> Seq.tryFind (String.contains "2019")
    | _ ->
        None

/// Returns the path to the Wwise console executable.
let private getCLIPath () =
    let cliPath =
        if OperatingSystem.IsWindows() then
            let wwiseRoot =
                match Environment.GetEnvironmentVariable "WWISEROOT" |> Option.ofString with
                | Some (Contains "2019" as path) ->
                    path
                | _ ->
                    // Try the default installation directory in program files
                    tryFindWwiseInstallation (Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86))
                    |> Option.orElseWith (fun () -> tryFindWwiseInstallation (Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)))
                    |> Option.defaultWith (fun () -> failwith "Could not locate Wwise 2019 installation from WWISEROOT environment variable or path Program Files\Audiokinetic.")

            Path.Combine(wwiseRoot, @"Authoring\x64\Release\bin\WwiseConsole.exe")
        elif OperatingSystem.IsMacOS() then
            let wwiseAppPath =
                tryFindWwiseInstallation "/Applications"
                |> Option.defaultWith (fun () -> failwith "Could not find Wwise 2019 installation in /Applications/Audiokinetic/")

            Path.Combine(wwiseAppPath, "Wwise.app/Contents/Tools/WwiseConsole.sh")
        else
            raise <| NotSupportedException "Only Windows and macOS are supported for Wwise conversion."

    if not <| File.Exists cliPath then
        failwith "Could not find Wwise Console executable."

    cliPath

let private getTempDirectory () =
    let dir = Path.Combine(Path.GetTempPath(), "RS2WwiseConv")
    if Directory.Exists dir then cleanDirectory dir
    else Directory.CreateDirectory dir |> ignore
    dir

/// Extracts the Wwise template into the target directory.
let private extractTemplate targetDir =
    let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
    use templateZip = embeddedProvider.GetFileInfo("Wwise/wwise2019.zip").CreateReadStream()
    using (new ZipArchive(templateZip)) (fun zip -> zip.ExtractToDirectory targetDir)

/// Extracts the Wwise template and copies the audio files into the Originals/SFX directory.
let private loadTemplate (sourcePath: string) =
    let templateDir = getTempDirectory()
    extractTemplate templateDir

    let orgSfxDir = Path.Combine(templateDir, "Originals", "SFX")

    let wavPreview = Utils.createPreviewAudioPath sourcePath
    let oggPreview = Path.ChangeExtension(wavPreview, "ogg")
    let mainTarget = Path.Combine(orgSfxDir, "Audio.wav")
    let previewTarget = Path.Combine(orgSfxDir, "Audio_preview.wav")
    
    // Copy main audio file
    match sourcePath with
    | EndsWith ".wav" -> File.Copy(sourcePath, mainTarget, true)
    | EndsWith ".ogg" -> Conversion.oggToWav sourcePath mainTarget
    | _ -> failwith "Could not detect file type from extension."

    // Copy preview audio file
    if File.Exists oggPreview && not <| File.Exists wavPreview then
        Conversion.oggToWav oggPreview previewTarget
    else
        File.Copy(wavPreview, previewTarget, true)

    templateDir

/// Fixes the header of a wem file.
let private fixHeader (fileName: string) =
    use file = File.Open(fileName, FileMode.Open, FileAccess.Write)
    let writer = LittleEndianBinaryWriter(file) :> IBinaryWriter
    file.Seek(40L, SeekOrigin.Begin) |> ignore
    writer.WriteUInt32 3u

/// Copies the wem files from the template cache directory into the destination path.
let private copyWemFiles (destPath: string) (templateDir: string) =
    let cachePath = Path.Combine(templateDir, ".cache", "Windows", "SFX")
    let wemFiles = Seq.toArray (DirectoryInfo(cachePath).EnumerateFiles("*.wem"))
    if wemFiles.Length < 2 then
        failwith "Could not find converted Wwise audio and preview audio files."

    wemFiles
    |> Array.iter (fun fileInfo ->
        let destFile =
            match fileInfo.Name with
            | Contains "preview" -> $"{destPath}_preview.wem" 
            | _ -> $"{destPath}.wem" 

        File.Copy(fileInfo.FullName, destFile, overwrite=true)
        fixHeader destFile)

/// Converts the source audio and preview audio files into wem files.
let convertToWem (cliPath: string option) (sourcePath: string) = async {
    // The target filename without extension
    let destPath = Path.Combine(Path.GetDirectoryName sourcePath,
                                Path.GetFileNameWithoutExtension sourcePath)
    let cliPath = cliPath |> Option.defaultWith getCLIPath
    let templateDir = loadTemplate sourcePath
    
    let args =
        Path.Combine(templateDir, "Template.wproj")
        |> sprintf """generate-soundbank "%s" --platform "Windows" --language "English(US)" --no-decode --quiet"""
    
    let startInfo = ProcessStartInfo(FileName = cliPath, Arguments = args, CreateNoWindow = true, RedirectStandardOutput = true)
    use wwiseCli = new Process(StartInfo = startInfo)
    wwiseCli.Start() |> ignore
    do! wwiseCli.WaitForExitAsync()

    let output = wwiseCli.StandardOutput.ReadToEnd()
    if output.Length > 0 then failwith output
    
    copyWemFiles destPath templateDir
    cleanDirectory templateDir }
