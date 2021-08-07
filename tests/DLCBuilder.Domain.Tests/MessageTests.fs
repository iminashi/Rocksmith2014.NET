module MessageTests

open Expecto
open DLCBuilder
open System
open Rocksmith2014.DLCProject
open Elmish

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
            let newState, _ = Main.update (ShowOverlay ConfigEditor) initialState

            Expect.equal newState.Overlay ConfigEditor "Overlay is set to configuration editor"

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
            let messages = [ SetSelectedImportTones selectedTones
                             ImportSelectedTones ]

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)

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
    ]
