[<AutoOpen>]
module Common

open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open Avalonia.Controls.Selection
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
      ManuallyEditingKnobKey = None
      ShowSortFields = false
      ShowJapaneseFields = false
      Overlay = NoOverlay
      SelectedImportTones = SelectionModel()
      RunningTasks = Set.empty
      StatusMessages = []
      CurrentPlatform = if OperatingSystem.IsMacOS() then Mac else PC
      OpenProjectFile = None
      AvailableUpdate = None
      ArrangementIssues = Map.empty }
