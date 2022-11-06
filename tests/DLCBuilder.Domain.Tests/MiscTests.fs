module MiscTests

open Expecto
open DLCBuilder
open Rocksmith2014.Common
open Rocksmith2014.DLCProject
open System

let testProject =
    { Version = "1"
      DLCKey = "Abcdefghijklmn"
      ArtistName = SortableString.Create "Artist"
      JapaneseArtistName = None
      JapaneseTitle = Some "日本"
      Title = { Value = "The Title"; SortValue = "" }
      AlbumName = SortableString.Create "Album"
      Year = DateTime.Now.Year
      AlbumArtFile = ""
      AudioFile = AudioFile.Empty
      AudioPreviewFile = AudioFile.Empty
      AudioPreviewStartTime = None
      PitchShift = None
      IgnoredIssues = Set.empty
      Arrangements = List.empty
      Tones = List.empty
      Author = None }

[<Tests>]
let tests =
    testList "Misc Builder Tests" [
        test "Test build project is updated correctly" {
            let updatedProject = TestPackageBuilder.updateProject "v3" testProject

            Expect.equal updatedProject.DLCKey "Abcdefghijklmnv3" "DLC key is correct"
            Expect.equal updatedProject.Title.SortValue "Title v3" "Title sort value is correct"
            Expect.equal updatedProject.Title.Value "The Title v3" "Title value is correct"
            Expect.equal updatedProject.JapaneseTitle (Some "日本 v3") "Japanese title is correct"
        }
    ]
