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
      AudioFileLength = None
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
        test "Test build project is updated correctly (missing title sort value)" {
            let updatedProject = TestPackageBuilder.updateProject (Some "v3") testProject

            Expect.equal updatedProject.DLCKey "Abcdefghijklmnv3" "DLC key is correct"
            Expect.equal updatedProject.Title.SortValue "Title v3" "Title sort value is correct"
            Expect.equal updatedProject.Title.Value "The Title v3" "Title value is correct"
            Expect.equal updatedProject.JapaneseTitle (Some "日本 v3") "Japanese title is correct"
        }

        test "Test build project sort value is set correctly" {
            let project = { testProject with Title = { testProject.Title with SortValue = "The Title" } }

            let updatedProject = TestPackageBuilder.updateProject (Some "v3") project

            Expect.equal updatedProject.Title.SortValue "The Title v3" "Title sort value is correct ('The' should not be removed)"
        }

        test "Test build project version is set to 'test'" {
            let updatedProject = TestPackageBuilder.updateProject None testProject

            Expect.equal updatedProject.Version "test" "version is correct"
        }
    ]
