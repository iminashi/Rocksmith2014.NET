module Rocksmith2014.DD.LevelCounter

open Rocksmith2014.DD.Model
open System

let private lockObj = obj()

let predictLevelCount (path: int) (p: DataExtractor.PhraseData) =
    // Add input data
    let input =
        ModelInput(
            Path = (path |> float32),
            LengthMs = (p.LengthMs |> float32),
            LengthBeats = (p.LengthBeats |> float32),
            Tempo = (p.TempoEstimate |> float32),
            Notes = (p.NoteCount |> float32),
            RepeatedNotes = (p.RepeatedNotes |> float32),
            Chords = (p.RepeatedChords |> float32),
            TechCount = (p.TechCount |> float32),
            PalmMutes = (p.PalmMuteCount |> float32),
            Bends = (p.BendCount |> float32),
            Harmonics = (p.HarmonicCount |> float32),
            Pharmonics = (p.PinchHarmonicCount |> float32),
            Taps = (p.TapCount |> float32),
            Tremolos = (p.TremoloCount |> float32),
            Vibratos = (p.VibratoCount |> float32),
            Slides = (p.SlideCount |> float32),
            UnpSlides = (p.UnpitchedSlideCount |> float32),
            Anchors = (p.AnchorCount |> float32),
            MaxChordStrings = (p.MaxChordStrings |> float32),
            Solo = (if p.SoloPhrase then "1" else "0")
        )
    
    // Load model and predict the output
    let result =
        lock lockObj (fun _ -> ConsumeModel.Predict(input))

    let levels =
        Math.Round(float result.Score)
        |> int
    Math.Clamp(levels, 2, 30)

let getSimpleLevelCount (phraseData: DataExtractor.PhraseData)
                        (divisionMap: DivisionMap) =
    let minLevels = max 2 phraseData.MaxChordStrings
    Math.Clamp(divisionMap.Count, minLevels, 30)
