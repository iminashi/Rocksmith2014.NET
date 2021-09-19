module Rocksmith2014.Audio.Wwise

open System
open System.Diagnostics
open System.IO
open System.IO.Compression
open System.Reflection
open Microsoft.Extensions.FileProviders
open Rocksmith2014.Common
open Rocksmith2014.Common.BinaryWriters

type private WwiseVersion = Wwise2019 | Wwise2021

/// Returns the path to the Wwise console executable.
let getCLIPath () =
    let cliPath =
        if OperatingSystem.IsWindows() then
            WwiseFinder.findWindows()
        elif OperatingSystem.IsMacOS() then
            WwiseFinder.findMac()
        elif OperatingSystem.IsLinux() then
            WwiseFinder.findLinux()
        else
            raise <| NotSupportedException "Wwise conversion is not supported on this OS."

    if not <| File.Exists cliPath then
        failwith "Could not find Wwise Console executable."

    cliPath

/// Returns a path to an empty temporary directory.
let private getTempDirectory () =
    let dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
    if Directory.Exists dir then Directory.Delete(dir, true)
    (Directory.CreateDirectory dir).FullName

/// Extracts the Wwise template into the target directory.
let private extractTemplate targetDir (version: WwiseVersion) =
    let embeddedProvider = EmbeddedFileProvider(Assembly.GetExecutingAssembly())
    let templateFile = version.ToString().ToLowerInvariant()
    use templateZip = embeddedProvider.GetFileInfo($"Wwise/{templateFile}.zip").CreateReadStream()
    using (new ZipArchive(templateZip)) (fun zip -> zip.ExtractToDirectory targetDir)

/// Extracts the Wwise template and copies the audio files into the Originals/SFX directory.
let private loadTemplate sourcePath version =
    let templateDir = getTempDirectory()
    let targetPath = Path.Combine(templateDir, "Originals", "SFX", "Audio.wav")
    extractTemplate templateDir version

    match sourcePath with
    | EndsWith ".wav" ->
        File.Copy(sourcePath, targetPath, overwrite=true)
    | EndsWith ".ogg" ->
        Conversion.oggToWav sourcePath targetPath
    | _ ->
        failwith "Could not detect audio file type from extension."

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

    Directory.EnumerateFiles(cachePath, "*.wem")
    |> Seq.tryHead
    |> function
        | Some convertedFile ->
            File.Copy(convertedFile, destPath, overwrite=true)
            fixHeader destPath
        | None ->
            failwith "Could not find converted Wwise audio file."

let private getWwiseVersion executablePath =
    if OperatingSystem.IsWindows() then
        let version =
            FileVersionInfo.GetVersionInfo executablePath

        match version.ProductMajorPart with
        | 2019 -> Wwise2019
        | 2021 -> Wwise2021
        | _ -> failwith $"Unsupported Wwise version ({version.FileVersion}).\nMust be major version 2019 or 2021."
    else
        match executablePath with
        | Contains "2019" -> Wwise2019
        | Contains "2021" -> Wwise2021
        | _ -> Wwise2021

let private createArgs templateDir =
    Path.Combine(templateDir, "Template.wproj")
    |> sprintf """generate-soundbank "%s" --platform "Windows" --language "English(US)" --no-decode --quiet"""

/// Converts the source audio file into a wem file.
let convertToWem (cliPath: string option) (sourcePath: string) = async {
    let destPath = Path.ChangeExtension(sourcePath, "wem")

    let cliPath =
        match cliPath with
        | Some (Contains "WwiseConsole" as path) ->
            if not <| File.Exists path then
                failwith $"The file: \"{path}\" does not exist."
            path
        | None ->
            getCLIPath()
        | _ ->
            failwith "Path to Wwise console executable appears to be wrong.\nIt should be to WwiseConsole.exe on Windows or WwiseConsole.sh on macOS."

    let version = getWwiseVersion cliPath
    let templateDir = loadTemplate sourcePath version

    try
        let startInfo =
            let args = createArgs templateDir
            let fileName, arguments =
                if OperatingSystem.IsLinux() then
                    "wine", $"\"{cliPath}\" {args}"
                else
                    cliPath, args
            ProcessStartInfo(FileName = fileName, Arguments = arguments, CreateNoWindow = true, RedirectStandardOutput = true)

        use wwiseCli = new Process(StartInfo = startInfo)
        wwiseCli.Start() |> ignore
        do! wwiseCli.WaitForExitAsync()

        let output = wwiseCli.StandardOutput.ReadToEnd()
        if output.Length > 0 then failwith output

        copyWemFile destPath templateDir
    finally
        Directory.Delete(templateDir, true) }
