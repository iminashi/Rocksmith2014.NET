module internal TestArrangement

open Rocksmith2014.XML

let toneChanges =
    ResizeArray(seq { ToneChange("test", 5555, 1uy) })

let sections =
    ResizeArray(
        seq {
            Section("noguitar", 6000, 1s)
            Section("riff", 6500, 1s)
            Section("noguitar", 8000, 2s)
        }
    )

let phrases =
    ResizeArray(seq { Phrase("mover6.700", 0uy, PhraseMask.None) })

let phraseIterations =
    ResizeArray(seq { PhraseIteration(6500, 0) })

let chordTemplates =
    ResizeArray(seq {
        ChordTemplate("", "", fingers = [| 2y; 2y; -1y; -1y; -1y; -1y |], frets = [| 2y; 2y; -1y; -1y; -1y; -1y |])
        // 1st finger not on lowest fret
        // | | 3 | | |
        // | 2 | 1 | |
        ChordTemplate("WEIRDO1", "", fingers = [| -1y; 2y; 3y; 1y; -1y; -1y |], frets = [| -1y; 2y; 1y; 2y; -1y; -1y |])
        // 2nd finger not on lowest fret
        // | | 4 | | |
        // | 2 | 3 | |
        ChordTemplate("WEIRDO2", "", fingers = [| -1y; 2y; 4y; 3y; -1y; -1y |], frets = [| -1y; 2y; 1y; 2y; -1y; -1y |])
        // Chord using thumb, fingering perfectly possible
        // | | 1 1 1 |
        // T | | | | |
        ChordTemplate("THUMB", "", fingers = [| 0y; -1y; 1y; 1y; 1y; -1y |], frets = [| 2y; -1y; 1y; 1y; 1y; -1y |])
        // Chord with impossible barre
        // | | o o | |
        // 1 | o o 1 |
        ChordTemplate("BARRE2", "", fingers = [| 1y; -1y; -1y; -1y; 1y; -1y |], frets = [| 3y; -1y; 0y; 0y; 3y; -1y |])
        // Chord with impossible barre
        // | o | | | |
        // 2 o 2 2 1 |
        ChordTemplate("BARRE2", "", fingers = [| 2y; -1y; 2y; 2y; 1y; -1y |], frets = [| 2y; 0y; 2y; 2y; 2y; -1y |])
        // Chord using thumb with open strings
        // | | o o o |
        // T | o o o |
        ChordTemplate("THUMB2", "", fingers = [| 0y; -1y; -1y; -1y; -1y; -1y |], frets = [| 5y; -1y; 0y; 0y; 0y; -1y |])
    })

let testArr =
    InstrumentalArrangement(
        Sections = sections,
        ChordTemplates = chordTemplates,
        Phrases = phrases,
        PhraseIterations = phraseIterations
    )
    |> apply (fun a ->
        a.Tones.Changes <- toneChanges
        a.MetaData.SongLength <- 10000)

