module Rocksmith2014.Audio.Wwise

open System
open System.IO
open System.IO.Compression
open System.Reflection
open System.Diagnostics
open Microsoft.Extensions.FileProviders
open Rocksmith2014.Common
open Rocksmith2014.Common.BinaryWriters

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

/// Returns a path to an empty temporary directory.
let private getTempDirectory () =
    let dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
    if Directory.Exists dir then Directory.Delete(dir, true)
    (Directory.CreateDirectory dir).FullName

/// Extracts the Wwise template into the target directory.
let private extractTemplate targetDir =
    let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
    use templateZip = embeddedProvider.GetFileInfo("Wwise/wwise2019.zip").CreateReadStream()
    using (new ZipArchive(templateZip)) (fun zip -> zip.ExtractToDirectory targetDir)

/// Extracts the Wwise template and copies the audio files into the Originals/SFX directory.
let private loadTemplate sourcePath =
    let templateDir = getTempDirectory()
    let targetPath = Path.Combine(templateDir, "Originals", "SFX", "Audio.wav")
    extractTemplate templateDir
    
    match sourcePath with
    | EndsWith ".wav" -> File.Copy(sourcePath, targetPath, overwrite=true)
    | EndsWith ".ogg" -> Conversion.oggToWav sourcePath targetPath
    | _ -> failwith "Could not detect file type from extension."

    templateDir

/// Fixes the header of a wem file to be compatible with Rocksmith 2014.
let private fixHeader (path: string) =
    use file = File.Open(path, FileMode.Open, FileAccess.Write)
    let writer = LittleEndianBinaryWriter(file) :> IBinaryWriter
    file.Seek(40L, SeekOrigin.Begin) |> ignore
    writer.WriteUInt32 3u

/// Copies the wem file from the template cache directory into the destination path.
let private copyWemFile (destPath: string) (templateDir: string) =
    let cachePath = Path.Combine(templateDir, ".cache", "Windows", "SFX")
    
    let wemFiles = Seq.toArray <| Directory.EnumerateFiles(cachePath, "*.wem")
    if wemFiles.Length = 0 then
        failwith "Could not find converted Wwise audio file."

    File.Copy(wemFiles.[0], destPath, overwrite=true)
    fixHeader destPath

/// Converts the source audio file into a wem file.
let convertToWem (cliPath: string option) (sourcePath: string) = async {
    let destPath = Path.ChangeExtension(sourcePath, "wem")
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
    
    copyWemFile destPath templateDir
    Directory.Delete(templateDir, true) }
