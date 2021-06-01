open System
open System.IO
open System.Diagnostics

let rec waitForBuilderExit count = async {
    let processes =
        Process.GetProcessesByName "DLCBuilder"
        
    if processes.Length > 0 then
        if count >= 5 then
            ()
        else
            do! Async.Sleep 200
            do! waitForBuilderExit (count + 1) }

let copyFiles sourceDirectory targetDirectory =
    Directory.EnumerateFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
    |> Seq.iter (fun path ->
        let targetPath =
            let rel = Path.GetRelativePath(sourceDirectory, path)
            Path.Combine(targetDirectory, rel)

        Directory.CreateDirectory(Path.GetDirectoryName targetPath) |> ignore

        File.Copy(path, targetPath, overwrite=true))

let startBuilder tempDirectory directory =
    try
        let builderPath =
            if OperatingSystem.IsMacOS() then
                Path.Combine(directory, "MacOS", "DLCBuilder")
            else
                Path.Combine(directory, "DLCBuilder")
        let startInfo = ProcessStartInfo(FileName = builderPath, Arguments = $"--updated \"{tempDirectory}\"")
        use dlcBuilder = new Process(StartInfo = startInfo)
        dlcBuilder.Start() |> ignore
    with e ->
        printfn $"Starting DLC Builder failed: {e.Message}"
        printfn "%s" e.StackTrace

        printfn "Press any key..."
        Console.ReadLine() |> ignore

[<EntryPoint>]
let main argv =
    if argv.Length >= 2 then
        let sourceDirectory = argv.[0]
        let targetDirectory = argv.[1]

        try
            printfn "Waiting for the DLC Builder process to exit..."

            async { do! waitForBuilderExit 0 } |> Async.RunSynchronously

            printfn "Copying files..."

            copyFiles sourceDirectory targetDirectory
            startBuilder sourceDirectory targetDirectory

            0
        with e ->
            printfn $"Update failed: {e.Message}"
            printfn "%s" e.StackTrace

            printfn "Press any key..."
            Console.ReadLine() |> ignore
            1
    else
        0
