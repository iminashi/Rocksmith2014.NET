module DLCBuilder.InitState

open Elmish
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open System.Diagnostics
open System.IO

let private createExitCheckFile () =
    using (File.Create(Configuration.exitCheckFilePath)) ignore

let init localizer albumArtLoader databaseConnector args =
    let commands =
        let wasAbnormalExit =
            File.Exists(Configuration.exitCheckFilePath)
            && Process.GetProcessesByName("DLCBuilder").Length = 1

        createExitCheckFile ()

        let loadProject =
            args
            |> Array.tryFind (String.endsWith ".rs2dlc")
            |> Option.map (fun path ->
                Cmd.OfTask.either
                    DLCProject.load
                    path
                    (fun p -> ProjectLoaded(p, FromFile path))
                    ErrorOccurred)
            |> Option.toList

        Cmd.batch [
            Cmd.OfTask.perform Configuration.load localizer (fun config -> SetConfiguration(config, loadProject.IsEmpty, wasAbnormalExit))
            Cmd.OfTask.perform RecentFilesList.load () SetRecentFiles
#if !DEBUG
            Cmd.OfTask.perform OnlineUpdate.checkForUpdates () SetAvailableUpdate
#endif
            Cmd.OfTask.perform ToneGear.loadRepository () SetToneRepository
            yield! loadProject
        ]

    { Project = DLCProject.Empty
      SavedProject = DLCProject.Empty
      RecentFiles = List.empty
      Config = Configuration.Default
      SelectedArrangementIndex = -1
      SelectedToneIndex = -1
      SelectedGearSlot = ToneGear.Amp
      SelectedImportTones = List.empty
      ManuallyEditingKnobKey = None
      ShowSortFields = false
      ShowJapaneseFields = false
      Overlay = NoOverlay
      RunningTasks = Set.empty
      StatusMessages = List.empty
      CurrentPlatform = PlatformSpecific.Value(mac = Mac, windows = PC, linux = PC)
      OpenProjectFile = None
      ArrangementIssues = Map.empty
      AvailableUpdate = None
      ToneGearRepository = None
      AlbumArtLoadTime = None
      QuickEditData = None
      ImportedBuildToolVersion = None
      AudioLength = None
      FontGenerationWatcher = None
      Localizer = localizer
      AlbumArtLoader = albumArtLoader
      DatabaseConnector = databaseConnector }, commands
