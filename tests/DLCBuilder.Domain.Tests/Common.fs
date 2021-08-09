[<AutoOpen>]
module Common

open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open System

let albumArtLoaderStub =
    { new IBitmapLoader with
        member _.InvalidateCache() = ()
        member _.TryLoad _ = true }

let toneDatabaseStub =
    { new ToneCollection.IDatabaseConnector with
        member _.TryCreateOfficialTonesApi() = None
        member _.CreateUserTonesApi() =
            { new ToneCollection.IUserTonesApi with
                member _.Dispose() = ()
                member _.GetToneById _ = None
                member _.GetTones _ = Array.empty
                member _.GetToneDataById _ = None
                member _.DeleteToneById _  = ()
                member _.AddTone _ = ()
                member _.UpdateData _ = () } }

let stringLocalizerStub =
    { new IStringLocalizer with
        member _.Translate _ = String.Empty
        member _.TranslateFormat _ _ = String.Empty
        member _.ChangeLocale _ = ()
        member _.LocaleFromShortName _ = Locale.Default }

let initialState =
    { Project = DLCProject.Empty
      SavedProject = DLCProject.Empty
      RecentFiles = List.empty
      Config = { Configuration.Default with ShowAdvanced = true }
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
      StatusMessages = List.empty
      CurrentPlatform = if OperatingSystem.IsMacOS() then Mac else PC
      OpenProjectFile = None
      AvailableUpdate = None
      ArrangementIssues = Map.empty
      ToneGearRepository = None
      AlbumArtLoadTime = None
      Localizer = stringLocalizerStub
      AlbumArtLoader = albumArtLoaderStub
      DatabaseConnector = toneDatabaseStub
      WindowMaximized = false }

let testTone =
    let gear =
        let pedal =
            { Type = ""
              KnobValues = Map.empty
              Key = ""
              Category = None
              Skin = None
              SkinIndex = None }

        { Amp = pedal
          Cabinet = pedal
          Racks =  [||]
          PrePedals =  [||]
          PostPedals =  [||] }

    { GearList = gear
      ToneDescriptors = [||]
      NameSeparator = " - "
      Volume = 17.
      MacVolume = None
      Key = "tone_key"
      Name = "tone_name"
      SortOrder = None }

let testLead =
    { XML = "instrumental.xml"
      Name = ArrangementName.Lead
      RouteMask = RouteMask.Lead
      Priority = ArrangementPriority.Main
      TuningPitch = 440.
      Tuning = [|0s;0s;0s;0s;0s;0s|]
      BaseTone = "Base_Tone"
      Tones = ["Tone_1"; "Tone_2"; "Tone_3"; "Tone_4"]
      ScrollSpeed = 1.3
      BassPicked = false
      MasterID = 12345
      PersistentID = Guid.NewGuid()
      CustomAudio = None }

let testVocals =
    { XML = "vocals.xml"
      Japanese = false
      CustomFont = None
      MasterID = 54321
      PersistentID = Guid.NewGuid() }
