module ManifestTests

open Expecto
open Rocksmith2014.Common.Manifest
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.Manifest.AttributesCreation
open System

[<Tests>]
let manifestTests =
    testList "Manifest Tests" [
        testCase "Can be converted to JSON" <| fun _ ->
            let arr =
                { Id = Guid.Empty
                  XmlPath = "lyrics.xml"
                  Japanese = false
                  CustomFont = None
                  MasterId = 123456
                  PersistentId = Guid.NewGuid() }

            let attr = createAttributes testProject (FromVocals arr)
            let jsonString =
                Manifest.create attr
                |> Manifest.toJsonString

            Expect.isNotEmpty jsonString "JSON string is not empty"

        testCase "Can be read from JSON" <| fun _ ->
            let attr =
                 { Id = Guid.Empty
                   XmlPath = "lyrics.xml"
                   Japanese = false
                   CustomFont = None
                   MasterId = 123456
                   PersistentId = Guid.NewGuid() }
                |> FromVocals
                |> createAttributes testProject

            let jsonString =
                Manifest.create attr
                |> Manifest.toJsonString

            let mani = Manifest.fromJsonString jsonString

            Expect.isTrue (mani.Entries.ContainsKey attr.PersistentID) "Manifest contains same key"
    ]
