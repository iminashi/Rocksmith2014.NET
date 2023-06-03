open Newtonsoft.Json.Linq
open Rocksmith2014.Common
open System

let printProgress (isVerbose: bool) (p: ProfileCleaner.IdReadingProgress) =
    Console.SetCursorPosition(0, 1)
    let progressBar = String('=', (int (60. * p.Progress)))
    printf "[%-60s]" progressBar

    if isVerbose then
        Console.SetCursorPosition(0, 2)
        printf "%s" (String(' ', Console.BufferWidth))
        printf $"\r{p.FileName}"

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

            let! ids, keys =
                async {
                    let onDisc = ProfileCleaner.readOnDiscIdsAndKeys ()
                    printfn "Reading IDs..."
                    let! dlcIds, dlcKeys = ProfileCleaner.gatherDLCData (printProgress isVerbose) dlcDirectory
                    printfn ""
                    return Set.union onDisc.OnDiscIds (Set.ofList dlcIds), Set.union onDisc.OnDiscKeys (Set.ofList dlcKeys)
                }

            let filterIds = ProfileCleaner.filterJTokenIds ids
            let filterKeys = ProfileCleaner.filterJArrayKeys keys
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
