module DLCBuilder.InitState

open Elmish
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System
open System.IO

let private createExitCheckFile () =
    using (File.Create Configuration.exitCheckFilePath) ignore

let init localizer albumArtLoader databaseConnector args =
    let commands =
        let wasAbnormalExit = File.Exists Configuration.exitCheckFilePath
        createExitCheckFile()

        let loadProject =
            args
            |> Array.tryFind (String.endsWith ".rs2dlc")
            |> Option.map (fun path ->
                Cmd.OfAsyncImmediate.either DLCProject.load path (fun p -> ProjectLoaded(p, path)) ErrorOccurred)
            |> Option.toList

        Cmd.batch [
            Cmd.OfAsyncImmediate.perform Configuration.load localizer (fun config -> SetConfiguration(config, loadProject.IsEmpty, wasAbnormalExit))
            Cmd.OfAsyncImmediate.perform RecentFilesList.load () SetRecentFiles
            Cmd.OfAsyncImmediate.perform OnlineUpdate.checkForUpdates () SetAvailableUpdate
            Cmd.OfAsyncImmediate.perform ToneGear.loadRepository () SetToneRepository
            yield! loadProject ]

    { Project = DLCProject.Empty
      SavedProject = DLCProject.Empty
      RecentFiles = []
      Config = Configuration.Default
      SelectedArrangementIndex = -1
      SelectedToneIndex = -1
      SelectedGear = None
      SelectedGearSlot = ToneGear.Amp
      SelectedImportTones = List.empty
      ManuallyEditingKnobKey = None
      ShowSortFields = false
      ShowJapaneseFields = false
      Overlay = NoOverlay
      RunningTasks = Set.empty
      StatusMessages = []
      CurrentPlatform = if OperatingSystem.IsMacOS() then Mac else PC
      OpenProjectFile = None
      ArrangementIssues = Map.empty
      AvailableUpdate = None
      ToneGearRepository = None
      AlbumArtLoadTime = None
      Localizer = localizer
      AlbumArtLoader = albumArtLoader
      DatabaseConnector = databaseConnector
      WindowMaximized = false }, commands
