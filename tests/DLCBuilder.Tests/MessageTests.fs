module MessageTests

open Expecto
open DLCBuilder
open System
open Rocksmith2014.DLCProject

[<Tests>]
let messageTests =
    testList "Message Tests" [
        testCase "CloseOverlay closes overlay" <| fun _ ->
            let state = { initialState with Overlay = SelectPreviewStart(TimeSpan.MinValue)}

            let newState, _ = Main.update CloseOverlay state

            Expect.equal newState.Overlay NoOverlay "Overlay was closed"

        testCase "ChangeLocale changes locale" <| fun _ ->
            let newState, _ = Main.update (ChangeLocale(Locales.All.[1])) initialState

            Expect.equal newState.Config.Locale Locales.All.[1] "Locale was changed"

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
            let newState, _ = Main.update (BuildComplete Test) { initialState with RunningTasks = Set([ BuildPackage ]) }

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
    ]
