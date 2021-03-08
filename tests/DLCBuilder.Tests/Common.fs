[<AutoOpen>]
module Common

open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System

let initialState =
    { Project = DLCProject.Empty
      SavedProject = DLCProject.Empty
      RecentFiles = []
      Config = Configuration.Default
      CoverArt = None
      SelectedArrangementIndex = -1
      SelectedToneIndex = -1
      SelectedGear = None
      SelectedGearSlot = ToneGear.Amp
      ShowSortFields = false
      ShowJapaneseFields = false
      Overlay = NoOverlay
      ImportTones = []
      RunningTasks = Set.empty
      CurrentPlatform = if OperatingSystem.IsMacOS() then Mac else PC
      OpenProjectFile = None
      ArrangementIssues = Map.empty }
