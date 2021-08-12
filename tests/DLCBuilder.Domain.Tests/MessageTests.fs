module MessageTests

open Expecto
open DLCBuilder
open System
open Rocksmith2014.DLCProject
open Elmish
open ToneCollection
open DLCBuilder.OnlineUpdate

let testUpdate =
    { AvailableUpdate = AvailableUpdate.Major
      UpdateVersion = Version(1, 0)
      ReleaseDate = DateTimeOffset.Now
      Changes = ""
      AssetUrl = "" }

let foldMessages state messages =
    messages
    |> List.fold (fun (state, _) message -> Main.update message state) (state, Cmd.none)

[<Tests>]
let messageTests =
    testList "Message Tests" [
        testCase "CloseOverlay closes overlay" <| fun _ ->
            let state = { initialState with Overlay = SelectPreviewStart(TimeSpan.MinValue)}

            let newState, _ = Main.update CloseOverlay state

            Expect.equal newState.Overlay NoOverlay "Overlay was closed"

        testCase "CloseOverlay does not close IdRegenerationConfirmation" <| fun _ ->
            let reply = AsyncReply(ignore)
            let state = { initialState with Overlay = IdRegenerationConfirmation(List.empty, reply) }

            let newState, _ = Main.update CloseOverlay state

            Expect.notEqual newState.Overlay NoOverlay "Overlay was not closed"

        testCase "ChangeLocale changes locale" <| fun _ ->
            let locale = { Name = "Foo"; ShortName = "f" }
            let mutable current = locale
            let stringLocalizer =
                { new IStringLocalizer with
                    member _.Translate _ = String.Empty
                    member _.TranslateFormat _ _ = String.Empty
                    member _.ChangeLocale x = current <- x
                    member _.LocaleFromShortName _ = Locale.Default }
            let state = { initialState with Localizer = stringLocalizer }

            let newState, _ = Main.update (ChangeLocale locale) state

            Expect.equal newState.Config.Locale locale "Locale was changed"
            Expect.equal current locale "IStringLocalizer.ChangeLocale was called"

        testCase "ErrorOccurred opens error message" <| fun _ ->
            let ex = exn("TEST")

            let newState, _ = Main.update (ErrorOccurred(ex)) initialState

            match newState.Overlay with
            | ErrorMessage (msg, _) ->
                Expect.equal msg ex.Message "Overlay is ErrorMessage"
            | _ ->
                failwith "Wrong overlay type"

        testCase "Build Release does nothing with empty project" <| fun _ ->
            Expect.isFalse (StateUtils.canBuild initialState) "Empty project cannot be built"

            let newState, cmd = Main.update (Build Release) initialState

            Expect.equal newState initialState "State was not changed"
            Expect.isTrue cmd.IsEmpty "No command was returned"

        testCase "BuildComplete removes build task" <| fun _ ->
            let newState, _ = Main.update (BuildComplete BuildCompleteType.Test) { initialState with RunningTasks = Set([ BuildPackage ]) }

            Expect.isFalse (newState.RunningTasks.Contains BuildPackage) "Build task was removed"

        testCase "ConversionComplete removes conversion task" <| fun _ ->
            let newState, _ = Main.update (WemConversionComplete ()) { initialState with RunningTasks = Set([ WemConversion ]) }

            Expect.isFalse (newState.RunningTasks.Contains WemConversion) "Conversion task was removed"

        testCase "SetRecentFiles sets recent files" <| fun _ ->
            let newState, _ = Main.update (SetRecentFiles [ "recent_file" ]) initialState

            Expect.equal newState.RecentFiles [ "recent_file" ] "Recent file list was changed"

        testCase "ShowOverlay ConfigEditor shows configuration editor" <| fun _ ->
            let newState, _ = Main.update (ShowOverlay (ConfigEditor FocusedSetting.ProfilePath)) initialState

            Expect.equal newState.Overlay (ConfigEditor FocusedSetting.ProfilePath) "Overlay is set to configuration editor"

        testCase "NewProject invalidates bitmap cache" <| fun _ ->
            let mutable wasInvalidated = false
            let bitmapLoaderMock =
                { new IBitmapLoader with
                    member _.InvalidateCache() = wasInvalidated <- true
                    member _.TryLoad _ = true }
            let state = { initialState with AlbumArtLoader = bitmapLoaderMock
                                            AlbumArtLoadTime = Some DateTime.Now }

            let newState, _ = Main.update NewProject state

            Expect.isTrue wasInvalidated "Cache was invalidated"
            Expect.isNone newState.AlbumArtLoadTime "Load time was set to none"

        testCase "ProjectLoaded calls IBitmapLoader TryLoad" <| fun _ ->
            let mutable wasCalled = false
            let bitmapLoaderMock =
                { new IBitmapLoader with
                    member _.InvalidateCache() = ()
                    member _.TryLoad _ = wasCalled <- true; true }
            let state = { initialState with AlbumArtLoader = bitmapLoaderMock }

            let newState, _ = Main.update (ProjectLoaded (DLCProject.Empty, "")) state

            Expect.isTrue wasCalled "TryLoad was called"
            Expect.isSome newState.AlbumArtLoadTime "Load time has a value"

        testCase "SetAlbumArt calls IBitmapLoader TryLoad" <| fun _ ->
            let mutable wasCalled = false
            let bitmapLoaderMock =
                { new IBitmapLoader with
                    member _.InvalidateCache() = ()
                    member _.TryLoad _ = wasCalled <- true; true }
            let state = { initialState with AlbumArtLoader = bitmapLoaderMock }

            let newState, _ = Main.update (EditProject (SetAlbumArt "")) state

            Expect.isTrue wasCalled "TryLoad was called"
            Expect.isSome newState.AlbumArtLoadTime "Load time has a value"

        testCase "SetSelectedImportTones, ImportSelectedTones" <| fun _ ->
            let selectedTones = [ testTone; testTone ]

            let newState, _ =
                [ SetSelectedImportTones selectedTones
                  ImportSelectedTones ]
                |> foldMessages initialState

            Expect.equal newState.SelectedImportTones selectedTones "Selected tones are correct"
            Expect.hasLength newState.Project.Tones 2 "Two tones were added to the project"

        testCase "ConfirmIdRegeneration shows overlay" <| fun _ ->
            let lead2 = { testLead with PersistentID = Guid.NewGuid() }
            let project = { initialState.Project with Arrangements = [ Instrumental testLead; Instrumental lead2 ] }
            let state = { initialState with Project = project }
            let ids = [ testLead.PersistentID ]
            let reply = AsyncReply(ignore)

            let newState, _ = Main.update (ConfirmIdRegeneration(ids, reply)) state

            match newState.Overlay with
            | IdRegenerationConfirmation(arrangements, overlayReply) ->
                Expect.hasLength arrangements 1 "Overlay has one arrangement"
                Expect.equal arrangements.[0] (Instrumental testLead) "Correct arrangement was selected"
                Expect.equal overlayReply reply "Reply is correct"
            | _ ->
                failwith "Wrong overlay type"

        testCase "SetNewArrangementIds updates arrangement IDs" <| fun _ ->
            let replacement = { testLead with PersistentID = Guid.NewGuid(); ScrollSpeed = 1.8 }
            let project = { initialState.Project with Arrangements = [ Instrumental testLead; Vocals testVocals ] }
            let state = { initialState with Project = project }
            let idMap = Map.ofList [ testLead.PersistentID, Instrumental replacement ]

            let newState, _ = Main.update (SetNewArrangementIds(idMap)) state

            match newState.Project.Arrangements.[0] with
            | Instrumental inst ->
                Expect.equal inst.PersistentID replacement.PersistentID "ID was updated"
                Expect.notEqual inst.ScrollSpeed replacement.ScrollSpeed "Scroll speed was not changed"
            | _ ->
                failwith "Wrong arrangement type"

        testCase "IdRegenerationAnswered closes overlay" <| fun _ ->
            let state = { initialState with Overlay = OverlayContents.AboutMessage }

            let newState, _ = Main.update IdRegenerationAnswered state

            Expect.equal newState.Overlay NoOverlay "Overlay was closed"

        testCase "DuplicateTone duplicates selected tone" <| fun _ ->
            let project = { initialState.Project with Tones = [ testTone ] }
            let state = { initialState with Project = project; SelectedToneIndex = 0 }

            let newState, _ = Main.update DuplicateTone state

            Expect.hasLength newState.Project.Tones 2 "Project has two tones"
            Expect.equal newState.Project.Tones.[0].Name (testTone.Name + "2") "2 was added to the name"

        testCase "DuplicateTone does nothing when no tone is selected" <| fun _ ->
            let newState, _ = Main.update DuplicateTone initialState

            Expect.equal newState initialState "State was not changed"

        testCase "CloseOverlay disposes tone collection" <| fun _ ->
            let mutable disposed = false
            let dataBase =
                { new ToneCollection.IDatabaseConnector with
                    member _.TryCreateOfficialTonesApi() = None
                    member _.CreateUserTonesApi() =
                        { new ToneCollection.IUserTonesApi with
                            member _.Dispose() = disposed <- true
                            member _.GetToneById _ = None
                            member _.GetTones _ = Array.empty
                            member _.GetToneDataById _ = None
                            member _.DeleteToneById _  = ()
                            member _.AddTone _ = ()
                            member _.UpdateData _ = () } }
            let collection = CollectionState.init dataBase ActiveTab.User
            let state = { initialState with Overlay = ToneCollection collection }

            ignore <| Main.update CloseOverlay state

            Expect.isTrue disposed "Collection was disposed"

        testCase "SetAudioFile sets audio file" <| fun _ ->
            let newState, cmd = Main.update (SetAudioFile "groovy.wav") initialState

            Expect.equal newState.Project.AudioFile.Path "groovy.wav" "Audio path was changed"
            Expect.isNonEmpty cmd "Auto volume command was returned"

        testCase "CalculateVolume adds long running task" <| fun _ ->
            let guid = Guid.NewGuid()

            let newState, cmd =
                [ CalculateVolume MainAudio
                  CalculateVolume PreviewAudio
                  CalculateVolume (CustomAudio("custom.ogg", guid)) ]
                |> foldMessages initialState

            Expect.contains newState.RunningTasks (VolumeCalculation MainAudio) "Correct task was added"
            Expect.contains newState.RunningTasks (VolumeCalculation PreviewAudio) "Correct task was added"
            Expect.contains newState.RunningTasks (VolumeCalculation (CustomAudio("custom.ogg", guid))) "Correct task was added"
            Expect.isNonEmpty cmd "Async command was returned"

        testCase "VolumeCalculated sets volume" <| fun _ ->
            let newState, _ =
                [ VolumeCalculated(99., MainAudio)
                  VolumeCalculated(-99., PreviewAudio) ]
                |> foldMessages initialState

            Expect.equal newState.Project.AudioFile.Volume 99. "Main audio volume was set"
            Expect.equal newState.Project.AudioPreviewFile.Volume -99. "Preview audio volume was set"

        testCase "SetSelectedArrangementIndex sets selected arrangement" <| fun _ ->
            let state = { initialState with Project = { initialState.Project with Arrangements = [ Instrumental testLead ] } }

            let newState, _ = Main.update (SetSelectedArrangementIndex 4) state

            Expect.equal newState.SelectedArrangementIndex -1 "Index was not changed when it is larger than the arrangement list's length"

            let newState, _ = Main.update (SetSelectedArrangementIndex 0) state

            Expect.equal newState.SelectedArrangementIndex 0 "Selected arrangement index was changed"

        testCase "SetAvailableUpdate sets update and adds status message" <| fun _ ->
            let newState, _ = Main.update (SetAvailableUpdate (Ok(Some testUpdate))) initialState

            Expect.equal newState.AvailableUpdate (Some testUpdate) "Update was set"
            Expect.hasLength newState.StatusMessages 1 "A status message was added"

        testCase "ShowUpdateInformation opens overlay" <| fun _ ->
            let state = { initialState with AvailableUpdate = Some testUpdate }

            let newState, _ = Main.update ShowUpdateInformation state

            Expect.equal newState.Overlay (UpdateInformationDialog testUpdate) "Overlay was opened"

        testCase "ProjectSaved updates recent files and configuration" <| fun _ ->
            let newState, _ = Main.update (ProjectSaved "target.file") initialState

            Expect.equal newState.RecentFiles.Head "target.file" "Recent files list was updated"
            Expect.equal newState.Config.PreviousOpenedProject "target.file" "Previously opened project was updated"

        testCase "GenerateNewIds changes IDs of selected arrangement" <| fun _ ->
            let project = { initialState.Project with Arrangements = [ Instrumental testLead; Vocals testVocals ] }
            let state = { initialState with Project = project; SelectedArrangementIndex = 0 }
            let oldPersistentId = testLead.PersistentID
            let oldMasterId = testLead.MasterID

            let newState, _ = Main.update GenerateNewIds state

            Expect.notEqual (Arrangement.getPersistentId newState.Project.Arrangements.[0]) oldPersistentId "Persistent ID was changed"
            Expect.notEqual (Arrangement.getMasterId newState.Project.Arrangements.[0]) oldMasterId "Master ID was changed"

        testCase "GenerateAllIds changes all arrangement IDs" <| fun _ ->
            let testLead2 = { testLead with PersistentID = Guid.NewGuid() }
            let project = { initialState.Project with Arrangements = [ Instrumental testLead; Instrumental testLead2; Vocals testVocals ] }
            let oldPersistentIds = project.Arrangements |> List.map Arrangement.getPersistentId |> Set.ofList
            let oldMasterIds = project.Arrangements |> List.map Arrangement.getMasterId |> Set.ofList
            let state = { initialState with Project = project }

            let newState, _ = Main.update GenerateAllIds state

            let newPersistentIds = newState.Project.Arrangements |> List.map Arrangement.getPersistentId |> Set.ofList
            let newMasterIds = newState.Project.Arrangements |> List.map Arrangement.getMasterId |> Set.ofList
            let intersect1 = Set.intersect oldPersistentIds newPersistentIds
            let intersect2 = Set.intersect oldMasterIds newMasterIds

            Expect.isEmpty intersect1 "All persistent IDs were changed"
            // Could fail due to the master IDs being generated randomly
            Expect.isEmpty intersect2 "All master IDs were changed"

        testCase "ApplyLowTuningFix applies fix to selected arrangement" <| fun _ ->
            let project = { initialState.Project with Arrangements = [ Instrumental testLead; Vocals testVocals ] }
            let state = { initialState with Project = project; SelectedArrangementIndex = 0 }

            let newState, _ = Main.update ApplyLowTuningFix state

            match newState.Project.Arrangements.[0] with
            | Instrumental inst ->
                Expect.equal inst.Tuning [| 12s; 12s; 12s; 12s; 12s; 12s |] "An octave was added to the tuning"
                Expect.equal inst.TuningPitch 220. "Tuning pitch was halved"
            | _ ->
                failwith "Wrong arrangement type"

        testCase "ShowIssueViewer opens overlay" <| fun _ ->
            let project = { initialState.Project with Arrangements = [ Instrumental testLead; Vocals testVocals ] }
            let state = { initialState with Project = project; SelectedArrangementIndex = 0 }

            let newState, _ = Main.update ShowIssueViewer state

            Expect.equal newState.Overlay (IssueViewer state.Project.Arrangements.[0]) "Issue viewer overlay was opened"

        testCase "RemoveStatusMessage removes correct status message" <| fun _ ->
            let id = Guid.NewGuid()
            let expectedMessage = "test message 2"
            let messages = [ MessageString(id, "test message")
                             MessageString(Guid.NewGuid(), expectedMessage) ]
            let state = { initialState with StatusMessages = messages }

            let newState, _ = Main.update (RemoveStatusMessage id) state

            Expect.hasLength newState.StatusMessages 1 "A message was removed"
            Expect.exists newState.StatusMessages (function MessageString(_, message) -> message = expectedMessage | _ -> false) "Correct message was removed"

        testCase "AddArrangements adds a new arrangement to the project" <| fun _ ->
            let newState, _ = Main.update (AddArrangements [| "instrumental.xml" |]) initialState

            Expect.hasLength newState.Project.Arrangements 1 "One arrangement was added"
            match newState.Project.Arrangements.[0] with
            | Instrumental inst ->
                Expect.equal inst.Priority ArrangementPriority.Main "Arrangement has correct priority"
                Expect.equal inst.Name ArrangementName.Lead "Arrangement has correct name"
                Expect.equal inst.RouteMask RouteMask.Lead "Arrangement has correct route mask"
            | _ ->
                failwith "Wrong arrangement type"
    ]
