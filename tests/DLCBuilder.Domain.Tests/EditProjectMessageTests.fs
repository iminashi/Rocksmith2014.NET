module EditProjectMessageTests

open Elmish
open Expecto
open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.DLCProject

[<Tests>]
let editProjectTests =
    testList "EditProject Message Tests" [
        testCase "SetDLCKey, SetVersion" <| fun _ ->
            let messages =
                [ SetDLCKey "DLCKey"
                  SetVersion "2000" ]
                |> List.map EditProject

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
            let project = newState.Project

            Expect.equal project.DLCKey "DLCKey" "DLC key is correct"
            Expect.equal project.Version "2000" "Version is correct"

        testCase "SetArtistName, SetArtistNameSort, SetJapaneseArtistName" <| fun _ ->
            let messages =
                [ SetArtistName "ArtistName"
                  SetArtistNameSort "ArtistNameSort"
                  SetJapaneseArtistName(Some "JapaneseArtistName") ]
                |> List.map EditProject

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
            let project = newState.Project

            Expect.equal project.ArtistName.Value "ArtistName" "Artist name is correct"
            Expect.equal project.ArtistName.SortValue "ArtistNameSort" "Artist name sort value is correct"
            Expect.equal project.JapaneseArtistName (Some "JapaneseArtistName") "Japanese artist name is correct"

        testCase "SetTitle, SetTitleSort, SetJapaneseTitle" <| fun _ ->
            let messages =
                [ SetTitle "Title"
                  SetTitleSort "TitleSort"
                  SetJapaneseTitle(Some "JapaneseTitle") ]
                |> List.map EditProject

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
            let project = newState.Project

            Expect.equal project.Title.Value "Title" "Title is correct"
            Expect.equal project.Title.SortValue "TitleSort" "Title sort value is correct"
            Expect.equal project.JapaneseTitle (Some "JapaneseTitle") "Japanese title is correct"

        testCase "SetAlbumName, SetAlbumNameSort, SetYear" <| fun _ ->
            let messages =
                [ SetAlbumName "AlbumName"
                  SetAlbumNameSort "AlbumNameSort"
                  SetYear 1800 ]
                |> List.map EditProject

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
            let project = newState.Project

            Expect.equal project.AlbumName.Value "AlbumName" "Album name is correct"
            Expect.equal project.AlbumName.SortValue "AlbumNameSort" "Album sort value is correct"
            Expect.equal project.Year 1800 "Year is correct"

        testCase "SetAudioVolume, SetPreviewVolume, SetPreviewStartTime" <| fun _ ->
            let messages =
                [ SetAudioVolume 9.
                  SetPreviewVolume -9.
                  SetPreviewStartTime 50. ]
                |> List.map EditProject

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
            let project = newState.Project

            Expect.equal project.AudioFile.Volume 9. "Volume is correct"
            Expect.equal project.AudioPreviewFile.Volume -9. "Preview volume is correct"
            Expect.equal project.AudioPreviewStartTime (Some 50.) "Preview start time is correct"

        testCase "SetPitchShift" <| fun _ ->
            let messages = [ SetPitchShift 1s ] |> List.map EditProject

            let newState, _ =
                messages
                |> List.fold (fun (state, _) message -> Main.update message state) (initialState, Cmd.none)
            let project = newState.Project

            Expect.equal project.PitchShift (Some 1s) "Pitch shift is correct"
    ]
