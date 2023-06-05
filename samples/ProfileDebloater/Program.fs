open Newtonsoft.Json.Linq
open Rocksmith2014.Common
open System
open System.IO
open System.Threading

let printerLock = obj()

let printProgress (directory: string) (isVerbose: bool) =
    let mutable processedFiles = 0

    fun (p: ProfileCleaner.IdReadingProgress) ->
        Interlocked.Increment(&processedFiles) |> ignore

        lock printerLock (fun () ->
            let totalProgress = float processedFiles / float p.TotalFiles
            if isVerbose then
                let progressStr = sprintf "%-5.1f%%" (100. * totalProgress)
                let fn = Path.GetRelativePath(directory, p.CurrentFilePath)
                printfn $"{progressStr} - {fn}"
            else
                let progressBar = String('=', (int (60. * totalProgress)))
                printf "\r[%-60s]" progressBar)

[<EntryPoint>]
let main argv =
    if argv.Length < 2 then
        Console.WriteLine "Give as arguments: path to profile file and path to DLC directory."
    else
        backgroundTask {
            let profilePath = argv[0]
            let dlcDirectory = argv[1]
            let isVerbose = Array.contains "-v" argv
            let isDryRun = Array.contains "-d" argv

            Console.Clear()
            let cursorVisibleOld = Console.CursorVisible
            Console.CursorVisible <- false

            printfn "Reading IDs..."

            let progressReporter = printProgress dlcDirectory isVerbose
            let maxDegreeOfParallelism = min 4 Environment.ProcessorCount

            let! data = ProfileCleaner.gatherIdAndKeyData progressReporter maxDegreeOfParallelism dlcDirectory
            let filterIds, filterKeys = ProfileCleaner.getFilteringFunctions data

            printfn ""

            let printStats section num =
                printfn "%-9s: %i record%s removed" section num (if num = 1 then "" else "s")

            printfn "Reading profile..."

            let! profile, profileId = Profile.readAsJToken profilePath

            printfn "Cleaning profile..."

            filterIds profile["Playnexts"].["Songs"] |> printStats "Playnexts"
            filterIds profile["Songs"] |> printStats "Songs"
            filterIds profile["SongsSA"] |> printStats "Songs SA"
            filterIds profile["Stats"].["Songs"] |> printStats "Stats"

            profile["SongListsRoot"]["SongLists"] :?> JArray
            |> Seq.iter (fun songList -> songList :?> JArray |> filterKeys)

            profile["FavoritesListRoot"]["FavoritesList"] :?> JArray
            |> filterKeys

            if not isDryRun then
                printfn "Saving profile file..."

                ProfileCleaner.backupProfile profilePath
                printfn "Backup file created."

                do! Profile.saveJToken profilePath profileId profile
                printfn "Profile saved."

            Console.CursorVisible <- cursorVisibleOld
        }
        |> fun t -> t.Wait()
    0
