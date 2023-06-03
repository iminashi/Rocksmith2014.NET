open Newtonsoft.Json.Linq
open Rocksmith2014.Common
open System
open System.IO

let printProgress (directory: string) (isVerbose: bool) (p: ProfileCleaner.IdReadingProgress) =
    Console.SetCursorPosition(0, 1)
    let progressBar = String('=', (int (60. * (float p.CurrentFileIndex / float p.TotalFiles))))
    printf "[%-60s]" progressBar

    if isVerbose then
        Console.SetCursorPosition(0, 2)
        printf "%s" (String(' ', Console.BufferWidth))
        let fn = Path.GetRelativePath(directory, p.CurrentFilePath)
        printf $"\r{fn}"

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

            let! data = ProfileCleaner.gatherIdAndKeyData (printProgress dlcDirectory isVerbose) dlcDirectory
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
