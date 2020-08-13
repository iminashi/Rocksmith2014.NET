module AttributesTechniquesTests

open Expecto
open System
open Rocksmith2014.SNG
open Rocksmith2014.DLCProject
open Rocksmith2014.DLCProject.Manifest.AttributesCreation
open Rocksmith2014.Common

let testLead =
    { XML = "instrumental.xml"
      Name = ArrangementName.Lead
      RouteMask = RouteMask.Lead
      Priority = ArrangementPriority.Main
      CentOffset = 0
      Tuning = [||]
      ScrollSpeed = 1.3
      BassPicked = false
      MasterID = 12345
      PersistentID = Guid.NewGuid() }

let testProject =
    { Version = "1.0"
      DLCKey = "SomeTest"
      ArtistName = SortableString.Create "Artist"
      JapaneseArtistName = None
      JapaneseTitle = None
      Title = SortableString.Create "Title"
      AlbumName = SortableString.Create "Album"
      Year = 2020
      AlbumArtFile = "cover.dds"
      AudioFile = { Path = "audio.wem"; Volume = 12. }
      AudioPreviewFile = { Path = "audio_preview.wem"; Volume = 12. }
      Arrangements = [ Instrumental testLead ]
      Tones = [] }

let testSng = SNG.readPackedFile "Tech_Test.sng" PC |> Async.RunSynchronously
let attr = createAttributes testProject (FromInstrumental (testLead, testSng))
let firstLevel = attr.Techniques.["0"]

[<Tests>]
let someTests =
  testList "Attribute Techniques Tests" [

    test "Accent, Bend, Palm Mute" {
        Expect.contains firstLevel.["1"] 0 "Phrase iteration 1 contains accent"
        Expect.contains firstLevel.["1"] 1 "Phrase iteration 1 contains bend"
        Expect.contains firstLevel.["1"] 7 "Phrase iteration 1 contains palm mute" }

    test "Fret Hand Mute, Hammer-on, Pull-off" {
        Expect.contains firstLevel.["2"] 2 "Phrase iteration 2 contains fret hand mute"
        Expect.contains firstLevel.["2"] 3 "Phrase iteration 2 contains hammer-on"
        Expect.contains firstLevel.["2"] 9 "Phrase iteration 2 contains pull-off"
        Expect.contains firstLevel.["2"] 6 "Phrase iteration 2 contains HOPO" }

    test "Harmonic, Pinch Harmonic, Slap, Pop" {
        Expect.contains firstLevel.["3"] 4 "Phrase iteration 3 contains harmonic"
        Expect.contains firstLevel.["3"] 5 "Phrase iteration 3 contains pinch harmonic"
        Expect.contains firstLevel.["3"] 10 "Phrase iteration 3 contains slap"
        Expect.contains firstLevel.["3"] 8 "Phrase iteration 3 contains pop / pluck" }

    test "Slide, Unpitched Slide, Sustain" {
        Expect.contains firstLevel.["4"] 11 "Phrase iteration 4 contains slide"
        Expect.contains firstLevel.["4"] 12 "Phrase iteration 4 contains unpitched slide"
        Expect.contains firstLevel.["4"] 13 "Phrase iteration 4 contains sustain" }

    test "Tap, Tremolo, Vibrato" {
        Expect.contains firstLevel.["5"] 14 "Phrase iteration 5 contains tap"
        Expect.contains firstLevel.["5"] 15 "Phrase iteration 5 contains tremolo"
        Expect.contains firstLevel.["5"] 16 "Phrase iteration 5 contains vibrato" }

    test "Palm Mute + Accent" {
        Expect.contains firstLevel.["6"] 0 "Phrase iteration 6 contains accent"
        Expect.contains firstLevel.["6"] 7 "Phrase iteration 6 contains palm mute"
        Expect.contains firstLevel.["6"] 17 "Phrase iteration 6 contains palm mute + accent" }

    test "Palm Mute + Hammer-on" {
        Expect.contains firstLevel.["7"] 7 "Phrase iteration 7 contains palm mute"
        Expect.contains firstLevel.["7"] 3 "Phrase iteration 7 contains hammer-on"
        Expect.contains firstLevel.["7"] 19 "Phrase iteration 7 contains palm mute + hammer-on" }

    test "Palm Mute + Pull-off" {
        Expect.contains firstLevel.["8"] 7 "Phrase iteration 8 contains palm mute"
        Expect.contains firstLevel.["8"] 9 "Phrase iteration 8 contains pull-off"
        Expect.contains firstLevel.["8"] 20 "Phrase iteration 8 contains palm mute + pull-off" }

    test "Fret Hand Mute + Accent" {
        Expect.contains firstLevel.["9"] 2 "Phrase iteration 9 contains fret hand mute"
        Expect.contains firstLevel.["9"] 0 "Phrase iteration 9 contains accent"
        Expect.contains firstLevel.["9"] 21 "Phrase iteration 9 contains fret hand mute + accent" }

    test "Tremolo + Bend" {
        Expect.contains firstLevel.["10"] 15 "Phrase iteration 10 contains tremolo"
        Expect.contains firstLevel.["10"] 1 "Phrase iteration 10 contains bend"
        Expect.contains firstLevel.["10"] 26 "Phrase iteration 10 contains tremolo + bend" }

    test "Tremolo + Slide" {
        Expect.contains firstLevel.["11"] 15 "Phrase iteration 11 contains tremolo"
        Expect.contains firstLevel.["11"] 11 "Phrase iteration 11 contains slide"
        Expect.contains firstLevel.["11"] 27 "Phrase iteration 11 contains tremolo + slide" }

    test "Tremolo + Vibrato" {
        Expect.contains firstLevel.["12"] 15 "Phrase iteration 12 contains tremolo"
        Expect.contains firstLevel.["12"] 16 "Phrase iteration 12 contains vibrato"
        Expect.contains firstLevel.["12"] 28 "Phrase iteration 12 contains tremolo + vibrato" }

    test "Pre-Bend" {
        Expect.contains firstLevel.["13"] 1 "Phrase iteration 13 contains bend"
        Expect.contains firstLevel.["13"] 13 "Phrase iteration 13 contains sustain"
        Expect.contains firstLevel.["13"] 29 "Phrase iteration 13 contains pre-bend" }

    test "Oblique Bend" {
        Expect.contains firstLevel.["14"] 30 "Phrase iteration 14 contains oblique bend" }

    test "Compound Bend" {
        Expect.contains firstLevel.["15"] 1 "Phrase iteration 15 contains bend"
        Expect.contains firstLevel.["15"] 13 "Phrase iteration 15 contains sustain"
        Expect.contains firstLevel.["15"] 31 "Phrase iteration 15 contains compound bend" }

    test "Double Stop Adjacent Strings" {
        Expect.contains firstLevel.["16"] 33 "Phrase iteration 16 contains double stop (adjacent strings)" }

    test "Double Stop Nonadjacent Strings" {
        Expect.contains firstLevel.["17"] 34 "Phrase iteration 17 contains double stop (nonadjacent strings)" }

    test "Power Chord" {
        Expect.hasLength firstLevel.["18"] 1 "Phrase iteration 18 technique count is 1"
        Expect.contains firstLevel.["18"] 35 "Phrase iteration 18 contains power chord" }

    test "Drop-D Power Chord" {
        Expect.contains firstLevel.["19"] 36 "Phrase iteration 19 contains drop-d power chord" }

    test "Barre Chord" {
        Expect.contains firstLevel.["20"] 38 "Phrase iteration 20 contains chord"
        Expect.contains firstLevel.["20"] 37 "Phrase iteration 20 contains barre chord" }

    test "Double Stop HOPO" {
        Expect.contains firstLevel.["21"] 40 "Phrase iteration 21 contains double stop HOPO" }

    test "Chord Slide" {
        Expect.contains firstLevel.["22"] 41 "Phrase iteration 22 contains chord slide" }

    test "Chord Tremolo" {
        Expect.contains firstLevel.["23"] 42 "Phrase iteration 23 contains chord tremolo" }

    test "Chord HOPO" {
        Expect.contains firstLevel.["24"] 43 "Phrase iteration 24 contains chord HOPO" }

    test "Double Stop Slide" {
        Expect.contains firstLevel.["25"] 33 "Phrase iteration 25 contains double stop (adjacent strings)"
        Expect.contains firstLevel.["25"] 44 "Phrase iteration 25 contains double stop slide" }

    test "Double Stop Tremolo" {
        Expect.contains firstLevel.["26"] 33 "Phrase iteration 26 contains double stop (adjacent strings)"
        Expect.contains firstLevel.["26"] 45 "Phrase iteration 26 contains double stop tremolo" }

    test "Double Stop Bend" {
        Expect.contains firstLevel.["27"] 33 "Phrase iteration 27 contains double stop (adjacent strings)"
        Expect.contains firstLevel.["27"] 46 "Phrase iteration 27 contains double stop bend" }

    test "Palm Mute + Harmonic" {
        Expect.contains firstLevel.["28"] 4 "Phrase iteration 28 contains harmonic"
        Expect.contains firstLevel.["28"] 7 "Phrase iteration 28 contains palm mute"
        Expect.contains firstLevel.["28"] 18 "Phrase iteration 28 contains palm mute + harmonic" }
  ]
