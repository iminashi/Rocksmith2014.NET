module Rocksmith2014.DD.Tests.GeneratorTests

open Expecto
open Rocksmith2014.DD
open Rocksmith2014.XML

let config =
    { PhraseSearchThreshold = Some 85
      LevelCountGeneration = LevelCountGeneration.Simple }

let arrangement (levels: Level seq) (chordTemplates: ChordTemplate seq) =
    let phrases = ResizeArray(seq {
        Phrase("COUNT", 0uy, PhraseMask.None)
        Phrase("riff", 0uy, PhraseMask.None)
        Phrase("END", 0uy, PhraseMask.None) })

    let iterations = ResizeArray(seq {
        PhraseIteration(0, 0)
        PhraseIteration(5000, 1)
        PhraseIteration(9000, 2) })

    let metadata = MetaData(SongLength = 10_000)

    InstrumentalArrangement(
        Levels = ResizeArray(levels),
        ChordTemplates = ResizeArray(chordTemplates),
        Phrases = phrases,
        PhraseIterations = iterations,
        MetaData = metadata
    )

let private sevenStringArrangement (levels: Level seq) (chordTemplates: ChordTemplate seq) =
    let arr = arrangement levels chordTemplates
    arr.MetaData.Tuning.Strings <- Array.zeroCreate 7
    arr

let private tenLevels = { config with LevelCountGeneration = LevelCountGeneration.Constant 10 }

[<Tests>]
let ddGeneratorTests =
    testList "DD Generator Tests" [
        testCase "Creates difficulty levels" <| fun _ ->
            let notes = ResizeArray(seq { Note(Time = 5000) })
            let level = Level(0y, Notes = notes)

            let arr =
                arrangement [ level ] []
                |> Generator.generateForArrangement config

            Expect.equal arr.Phrases[1].Name "p0" "Phrase was renamed"
            Expect.isGreaterThan arr.Levels.Count 1 "Levels were generated"

        testCase "Chord is replaced with note in low level" <| fun _ ->
            let cn = ResizeArray(seq {
                Note(String = 0y, Fret = 3y, Time = 5000, Sustain = 800)
                Note(String = 1y, Fret = 5y, Time = 5000, Sustain = 800)
                Note(String = 2y, Fret = 5y, Time = 5000, Sustain = 800)
            })
            let chords = ResizeArray(seq { Chord(Time = 5000, ChordNotes = cn) })
            let handshapes = ResizeArray(seq { HandShape(0s, 5000, 800) })
            let level = Level(0y, Chords = chords, HandShapes = handshapes)
            let template = ChordTemplate("G5", "G5", [| 1y; 3y; 4y; -1y; -1y; -1y; |], [| 3y; 5y; 5y; -1y; -1y; -1y |])

            let arr =
                arrangement [ level ] [ template ]
                |> Generator.generateForArrangement tenLevels

            let highestLevel = arr.Levels[9]
            Expect.hasLength arr.Levels[0].Notes 1 "Note exists in lowest level"

            // Note should have no sustain at difficulty < 20%
            Expect.equal arr.Levels[0].Notes[0].Sustain 0 "Note sustain is correct at level 1"
            // Note should have sustain at difficulty >= 20%
            Expect.equal arr.Levels[2].Notes[0].Sustain 800 "Note sustain is correct at level 3"

            Expect.hasLength highestLevel.Notes 0 "Notes do not exist in highest level"
            Expect.hasLength highestLevel.Chords 1 "Chord exists in highest level"

        testCase "Note on lowest level for double stop on high strings is the highest string" <| fun _ ->
            let cn = ResizeArray(seq {
                Note(String = 4y, Fret = 5y, Time = 5000, Sustain = 800)
                Note(String = 5y, Fret = 5y, Time = 5000, Sustain = 800)
            })
            let chords = ResizeArray(seq { Chord(Time = 5000, ChordNotes = cn) })
            let handshapes = ResizeArray(seq { HandShape(0s, 5000, 800) })
            let level = Level(0y, Chords = chords, HandShapes = handshapes)
            let template = ChordTemplate("", "", [| -1y; -1y; -1y; -1y; 1y; 1y; |], [| -1y; -1y; -1y; -1y; 5y; 5y |])

            let arr =
                arrangement [ level ] [ template ]
                |> Generator.generateForArrangement tenLevels

            Expect.hasLength arr.Levels[0].Notes 1 "Note exists in lowest level"
            Expect.equal arr.Levels[0].Notes[0].String 5y "String is correct"

        testCase "Seven-string chord reduced to a note uses the low B as the lowest string" <| fun _ ->
            let cn = ResizeArray(seq {
                Note(String = 0y, Fret = 2y, Time = 5000, Sustain = 800)
                Note(String = 1y, Fret = 2y, Time = 5000, Sustain = 800)
                Note(String = 6y, Fret = 0y, Time = 5000, Sustain = 800)
            })
            let chords = ResizeArray(seq { Chord(Time = 5000, ChordNotes = cn) })
            let handshapes = ResizeArray(seq { HandShape(0s, 5000, 5800) })
            let level = Level(0y, Chords = chords, HandShapes = handshapes)
            let template =
                ChordTemplate("B5", "B5", [| 1y; 1y; -1y; -1y; -1y; -1y; -1y |], [| 2y; 2y; -1y; -1y; -1y; -1y; 0y |])

            let arr =
                sevenStringArrangement [ level ] [ template ]
                |> Generator.generateForArrangement tenLevels

            Expect.hasLength arr.Levels[0].Notes 1 "Note exists in lowest level"
            Expect.equal arr.Levels[0].Notes[0].String 6y "The low B is the lowest string"

        testCase "Reduced seven-string templates keep the seventh string and match their chord notes" <| fun _ ->
            let cn = ResizeArray(seq {
                Note(String = 0y, Fret = 2y, Time = 5000, Sustain = 800)
                Note(String = 1y, Fret = 2y, Time = 5000, Sustain = 800)
                Note(String = 2y, Fret = 2y, Time = 5000, Sustain = 800)
                Note(String = 6y, Fret = 0y, Time = 5000, Sustain = 800)
            })
            let chords = ResizeArray(seq { Chord(Time = 5000, ChordNotes = cn) })
            let handshapes = ResizeArray(seq { HandShape(0s, 5000, 5800) })
            let level = Level(0y, Chords = chords, HandShapes = handshapes)
            let template =
                ChordTemplate("B5", "B5", [| 1y; 1y; 1y; -1y; -1y; -1y; -1y |], [| 2y; 2y; 2y; -1y; -1y; -1y; 0y |])

            let arr =
                sevenStringArrangement [ level ] [ template ]
                |> Generator.generateForArrangement tenLevels

            Expect.all arr.ChordTemplates (fun t -> t.StringCount = 7) "All templates keep seven strings"
            Expect.exists
                arr.ChordTemplates
                (fun t -> t.Frets[6] = 0y && t.Frets[0] = 2y && t.Frets[2] = -1y)
                "A reduced template keeps the low B and removes the highest string"

            for lvl in arr.Levels do
                for chord in lvl.Chords do
                    if chord.HasChordNotes then
                        let t = arr.ChordTemplates[int chord.ChordId]
                        for chordNote in chord.ChordNotes do
                            Expect.notEqual t.Frets[int chordNote.String] -1y "Chord note string is used by its template"
    ]
