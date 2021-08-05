module UserCollectionTests

open Expecto
open System.IO
open ToneCollection

let officialPath = Path.GetFullPath("none.db")
let userPath = Path.GetFullPath("user_test.db")

let queryOptions = { Search = None; PageNumber = 1 }

let connector =
    Database.createConnector (OfficialDataBasePath officialPath) (UserDataBasePath userPath)

[<Tests>]
let tests =
    testSequenced <| testList "User Collection Tests" [
        testCase "User database is created if it does not exist" <| fun _ ->
            if File.Exists userPath then
                File.Delete userPath

            using (connector.CreateUserTonesApi()) ignore

            Expect.isTrue (File.Exists userPath) "Database file was created"

        testCase "Tone can be added to the database" <| fun _ ->
            use api = connector.CreateUserTonesApi()

            let testTone =
                { Id = 111L
                  Artist = "Artist"
                  ArtistSort = "ArtistSort"
                  Title = "Title"
                  TitleSort = "TitleSort"
                  Name = "tone_name"
                  BassTone = false
                  Description = "CLEAN|EFFECT"
                  Definition = "n/a" }

            api.AddTone testTone
            let tones = api.GetTones(queryOptions)

            Expect.hasLength tones 1 "One tone returned"
            Expect.equal tones.[0].Id 1L "ID was auto generated"
            Expect.equal tones.[0].Artist testTone.Artist "Artist is correct"
            Expect.equal tones.[0].Title testTone.Title "Title is correct"
            Expect.equal tones.[0].Name testTone.Name "Name is correct"
            Expect.equal tones.[0].BassTone testTone.BassTone "BassTone is correct"
            Expect.equal tones.[0].Description testTone.Description "Description is correct"

        testCase "Tone metadata can be updated" <| fun _ ->
            use api = connector.CreateUserTonesApi()

            let updatedTone =
                { Id = 1L
                  Artist = "NewArtist"
                  ArtistSort = "NewArtistSort"
                  Title = "NewTitle"
                  TitleSort = "NewTitleSort"
                  Name = "new_tone_name"
                  BassTone = true
                  Description = "Can not be changed"
                  Definition = "Can not be changed" }

            api.UpdateData updatedTone
            let retrievedTone = api.GetToneDataById updatedTone.Id

            match retrievedTone with
            | Some tone ->
                Expect.equal tone.Id 1L "ID was auto generated"
                Expect.equal tone.Artist updatedTone.Artist "Artist is correct"
                Expect.equal tone.Title updatedTone.Title "Title is correct"
                Expect.equal tone.Name updatedTone.Name "Name is correct"
                Expect.equal tone.BassTone updatedTone.BassTone "BassTone is correct"
                Expect.equal tone.Description "CLEAN|EFFECT" "Description is correct"
            | None ->
                failwith "No tone returned from database."

        testCase "Tone can be deleted from database" <| fun _ ->
            use api = connector.CreateUserTonesApi()

            api.DeleteToneById 1L
            let tones = api.GetTones(queryOptions)

            Expect.isEmpty tones "No tones returned"
    ]
