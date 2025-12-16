module internal Rocksmith2014.DD.LevelCounter

open System
open Rocksmith2014.DD.Model

let [<Literal>] MinimumNumberOfLevelsToGenerate = 2
let [<Literal>] MaximumPossibleLevels = 30

let private lockObj = obj ()

let predictLevelCount (path: int) (p: DataExtractor.PhraseData) =
    // Add input data
    let input =
        ModelInput(
            Path = float32 path,
            LengthMs = float32 p.LengthMs,
            LengthBeats = float32 p.LengthBeats,
            Tempo = float32 p.TempoEstimate,
            Notes = float32 p.NoteCount,
            RepeatedNotes = float32 p.RepeatedNotes,
            Chords = float32 p.RepeatedChords,
            TechCount = float32 p.TechCount,
            PalmMutes = float32 p.PalmMuteCount,
            Bends = float32 p.BendCount,
            Harmonics = float32 p.HarmonicCount,
            Pharmonics = float32 p.PinchHarmonicCount,
            Taps = float32 p.TapCount,
            Tremolos = float32 p.TremoloCount,
            Vibratos = float32 p.VibratoCount,
            Slides = float32 p.SlideCount,
            UnpSlides = float32 p.UnpitchedSlideCount,
            Anchors = float32 p.AnchorCount,
            MaxChordStrings = float32 p.MaxChordStrings,
            Solo = if p.SoloPhrase then "1" else "0"
        )

    // Load model and predict the output
    let result = lock lockObj (fun _ -> ConsumeModel.Predict(input))

    let levels = round result.Score |> int
    Math.Clamp(levels, MinimumNumberOfLevelsToGenerate, MaximumPossibleLevels)

let private getRepeatedNotePercent (phraseData: DataExtractor.PhraseData) =
    let repeated = float (phraseData.RepeatedNotes + phraseData.RepeatedChords)
    let total = float (phraseData.NoteCount + phraseData.ChordCount)
    repeated / total

let getSimpleLevelCount (phraseData: DataExtractor.PhraseData) (scoreMap: ScoreMap) =
    let baseCount = scoreMap.Count

    // Try to prevent inflated level count for phrases that are mostly repeated notes
    let levelCount =
        if baseCount > 15 && getRepeatedNotePercent phraseData > 0.8 then
            baseCount / 2
        else
            baseCount

    let minLevels = max MinimumNumberOfLevelsToGenerate phraseData.MaxChordStrings
    Math.Clamp(levelCount, minLevels, MaximumPossibleLevels)
