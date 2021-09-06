module Rocksmith2014.DLCProject.Manifest.Techniques

open Rocksmith2014.SNG

let [<Literal>] private ChordNotesDoubleStop = NoteMask.DoubleStop ||| NoteMask.ChordNotes

let inline hasFlag (n: Note) flag = (n.Mask &&& flag) = flag

let isPowerChord sng note =
    hasFlag note NoteMask.DoubleStop
    &&
    let s1 = Array.findIndex (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    let s2 = Array.findIndexBack (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    let f1 = Array.find (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    let f2 = Array.findBack (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    // Root on D string or lower
    s1 <= 2 && s1 + 1 = s2 && f1 + 2y = f2

let private isDropTuning (tuning: int16 array) =
    tuning.[0] = tuning.[1] - 2s && tuning.[0] = tuning.[2] - 2s

let isDropDPower sng note =
    hasFlag note NoteMask.Chord
    && isDropTuning sng.MetaData.Tuning
    &&
    let frets = sng.Chords.[note.ChordId].Frets
    frets.[0] <> -1y && frets.[0] = frets.[1] && frets.[0] = frets.[2]

let isChord (note: Note) =
    note.Mask &&& (NoteMask.Chord ||| NoteMask.Sustain ||| NoteMask.DoubleStop) = NoteMask.Chord

let isBarre sng (note: Note) =
    note.Mask &&& (NoteMask.Chord ||| NoteMask.DoubleStop) = NoteMask.Chord
    &&
    sng.Chords.[note.ChordId].Fingers
    |> Array.countBy id
    |> Array.exists (fun (finger, count) -> finger <> -1y && count >= 3)

let isObliqueBend sng note =
    hasFlag note NoteMask.ChordNotes
    &&
    sng.ChordNotes.[note.ChordNotesId].BendData
    |> Array.sumBy (fun b -> if b.UsedCount > 0 then 1 else 0) = 1

let isCompoundBend note =
    hasFlag note NoteMask.Bend
    && note.BendData.Length >= 3
    && note.BendData.[0].Step <> note.BendData.[1].Step
    && note.BendData.[1].Step <> note.BendData.[2].Step

let isPreBend note =
    hasFlag note NoteMask.Bend
    &&
    let firstBend = note.BendData.[0]
    firstBend.Time = note.Time && firstBend.Step > 0.f

let isChordHammerOn sng note =
    hasFlag note NoteMask.ChordNotes
    &&
    sng.ChordNotes.[note.ChordNotesId].Mask
    |> Array.exists (fun x -> (x &&& NoteMask.HammerOn) <> NoteMask.None)

let isDoubleStopAdjacentStrings sng note =
    hasFlag note NoteMask.DoubleStop
    &&
    let s1 = Array.findIndex (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    let s2 = Array.findIndexBack (fun x -> x >= 0y) sng.Chords.[note.ChordId].Frets
    s1 + 1 = s2

let isDoubleStopNonAdjacentStrings sng note =
    hasFlag note NoteMask.DoubleStop && not <| isDoubleStopAdjacentStrings sng note

let isChordSlide sng (note: Note) =
    note.Mask &&& ChordNotesDoubleStop = NoteMask.ChordNotes
    &&
    sng.ChordNotes.[note.ChordNotesId].SlideTo
    |> Array.exists (fun x -> x <> -1y)

let isChordTremolo sng (note: Note) =
    note.Mask &&& ChordNotesDoubleStop = NoteMask.ChordNotes
    &&
    sng.ChordNotes.[note.ChordNotesId].Mask
    |> Array.exists (fun x -> (x &&& NoteMask.Tremolo) <> NoteMask.None)

let isDoubleStopSlide sng note =
    hasFlag note ChordNotesDoubleStop
    &&
    sng.ChordNotes.[note.ChordNotesId].SlideTo
    |> Array.exists (fun x -> x <> -1y)

let isDoubleStopTremolo sng note =
    hasFlag note ChordNotesDoubleStop
    &&
    sng.ChordNotes.[note.ChordNotesId].Mask
    |> Array.exists (fun x -> (x &&& NoteMask.Tremolo) <> NoteMask.None)

let isDoubleStopBend sng note =
    hasFlag note ChordNotesDoubleStop
    &&
    sng.ChordNotes.[note.ChordNotesId].BendData
    |> Array.exists (fun x -> x.UsedCount > 0)

let getTechniques (sng: SNG) (note: Note) =
    if note.Mask = NoteMask.None || note.Mask = NoteMask.Single || note.Mask = (NoteMask.Single ||| NoteMask.Open) then
        Seq.empty
    else
        let isHopo = note.Mask &&& (NoteMask.HammerOn ||| NoteMask.PullOff) <> NoteMask.None
        let isPower = isPowerChord sng note

        (* Some technique numbers don't seem to match the description in the lesson technique database.
           The technique database has 45 as chord + fret hand mute, but that is already tech #2.

           This method will miss some techniques when they are on chord notes, otherwise it should be pretty complete.

           Not used:
           22: Fret hand mute + pop
           23: Fret hand mute + slap (found in "Living in America", but not included in the tech map)
           24: Harmonic + pop
           25: Harmonic + slap
           32: Arpeggio
           39: Unknown (not included in technique database, but could be "chord zone" or "non-standard chord") *)

        seq {
            // 0: Accent
            if hasFlag note NoteMask.Accent then 0

            // 1: Bend
            if hasFlag note NoteMask.Bend then 1

            // 2: Fret-hand mute (chords only)
            if hasFlag note NoteMask.FretHandMute then 2

            // 3: Hammer-on
            if hasFlag note NoteMask.HammerOn then 3

            // 4: Harmonic
            if hasFlag note NoteMask.Harmonic then 4

            // 5: Pinch harmonic
            if hasFlag note NoteMask.PinchHarmonic then 5

            // 6: HOPO (not in technique database)
            if isHopo then 6

            // 7: Palm mute
            if hasFlag note NoteMask.PalmMute then 7

            // 8: Pluck aka Pop
            if hasFlag note NoteMask.Pluck then 8

            // 9: Pull-off
            if hasFlag note NoteMask.PullOff then 9

            // 10: Slap
            if hasFlag note NoteMask.Slap then 10

            // 11: Slide
            if hasFlag note NoteMask.Slide then 11

            // 12: Unpitched slide
            if hasFlag note NoteMask.UnpitchedSlide then 12

            // 13: Sustain (single notes)
            if hasFlag note NoteMask.Single && hasFlag note NoteMask.Sustain then 13

            // 14: Tap
            if hasFlag note NoteMask.Tap then 14

            // 15: Tremolo
            if hasFlag note NoteMask.Tremolo then 15

            // 16: Vibrato
            if hasFlag note NoteMask.Vibrato then 16

            // 17: Palm mute + accent
            if hasFlag note (NoteMask.PalmMute ||| NoteMask.Accent) then 17

            // 18: Palm mute + harmonic
            if hasFlag note (NoteMask.PalmMute ||| NoteMask.Harmonic) then 18

            // 19: Palm mute + hammer-on
            if hasFlag note (NoteMask.PalmMute ||| NoteMask.HammerOn) then 19

            // 20: Palm mute + pull off
            if hasFlag note (NoteMask.PalmMute ||| NoteMask.PullOff) then 20

            // 21: Fret hand mute + accent
            if hasFlag note (NoteMask.FretHandMute ||| NoteMask.Accent) then 21

            // 26: Tremolo + bend
            if hasFlag note (NoteMask.Bend ||| NoteMask.Tremolo) then 26

            // 27: Tremolo + slide
            if hasFlag note (NoteMask.Tremolo ||| NoteMask.Slide) then 27

            // 28: Tremolo + vibrato (used on pick scratches)
            if hasFlag note (NoteMask.Tremolo ||| NoteMask.Vibrato) then 28

            // 29: Pre-bend
            if isPreBend note then 29

            // 30: Oblique bend (compound bend in technique database)
            if isObliqueBend sng note then 30

            // 31: Compound bend (oblique bend in technique database)
            if isCompoundBend note then 31

            // 33: Double stop with adjacent strings
            if not isPower && isDoubleStopAdjacentStrings sng note then 33

            // 34: Double stop with nonadjacent strings
            if not isPower && isDoubleStopNonAdjacentStrings sng note then 34

            // 35: Two string power chord
            if isPower then 35

            // 36: Drop-D power chord (not in technique database)
            if isDropDPower sng note then 36

            // 37: Barre chord
            if isBarre sng note then 37

            // 38: Chord (with three or more strings, no sustain)
            if isChord note then 38

            // 40: Double stop HOPO (actually HOPO inside hand shape)
            if isHopo && note.FingerPrintId.[0] <> -1s then 40

            // 41: Chord slide (chord HOPO in technique database)
            if isChordSlide sng note then 41

            // 42: Chord tremolo (double stop slide in technique database)
            if isChordTremolo sng note then 42

            // 43: Chord HOPO (chord slide in technique database) 
            if isChordHammerOn sng note then 43

            // 44: Double stop slide (chord tremolo in technique database)
            if isDoubleStopSlide sng note then 44

            // 45: Double stop tremolo (double stop bend in technique database)
            if isDoubleStopTremolo sng note then 45

            // 46: Double stop bend (double stop tremolo in technique database)
            if isDoubleStopBend sng note then 46 }
