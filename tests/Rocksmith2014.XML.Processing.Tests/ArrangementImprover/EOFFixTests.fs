module Rocksmith2014.XML.Processing.Tests.EOFFixTests

open Expecto
open Rocksmith2014.XML
open Rocksmith2014.XML.Processing

[<Tests>]
let eofFixTests =
    testList "Arrangement Improver (EOF Fixes)" [
        testCase "Adds LinkNext to chords missing the attribute" <| fun _ ->
            let chord = Chord(ChordNotes = ![ Note(IsLinkNext = true) ])
            let levels = ![ Level(Chords = ![ chord ]) ]
            let arr = InstrumentalArrangement(Levels = levels)

            EOFFixes.fixChordNotes arr

            Expect.isTrue chord.IsLinkNext "LinkNext was enabled"

        testCase "Fixes varying sustain of chord notes" <| fun _ ->
            let correctSustain = 500
            let chord = Chord(ChordNotes = ![ Note(Sustain = 0); Note(String = 1y, Sustain = correctSustain); Note(String = 2y, Sustain = 85) ])
            let levels = ![ Level(Chords = ![ chord ]) ]
            let arr = InstrumentalArrangement(Levels = levels)

            EOFFixes.fixChordNotes arr

            Expect.all levels[0].Chords (fun c -> c.ChordNotes |> Seq.forall (fun cn -> cn.Sustain = correctSustain)) "Sustain was changed"

        testCase "Removes incorrect chord note linknexts" <| fun _ ->
            let cn = ![ Note(IsLinkNext = true) ]
            let chords = ![ Chord(ChordNotes = cn, IsLinkNext = true) ]
            let levels = ![ Level(Chords = chords) ]
            let arr = InstrumentalArrangement(Levels = levels)

            EOFFixes.removeInvalidChordNoteLinkNexts arr

            Expect.isFalse cn.[0].IsLinkNext "LinkNext was removed from chord note"

        testCase "Chord note linknext is not removed when there is 1ms gap" <| fun _ ->
            let cn = ![
                Note(String = 0y, Sustain = 499, IsLinkNext = true)
                Note(String = 1y, Sustain = 499, IsLinkNext = true)
            ]
            let chords = ![ Chord(ChordNotes = cn, IsLinkNext = true) ]
            let notes = ![ Note(String = 0y, Time = 500) ]
            let levels = ![ Level(Chords = chords, Notes = notes) ]
            let arr = InstrumentalArrangement(Levels = levels)

            EOFFixes.removeInvalidChordNoteLinkNexts arr

            Expect.isTrue cn.[0].IsLinkNext "First chord note has LinkNext"
            Expect.isFalse cn.[1].IsLinkNext "Second chord note does not have LinkNext"

        testCase "Fixes incorrect crowd events" <| fun _ ->
            let events = ![ Event("E0", 100); Event("E1", 200); Event("E2", 300) ]
            let arr = InstrumentalArrangement(Events = events)

            EOFFixes.fixCrowdEvents arr

            Expect.hasLength arr.Events 3 "Number of events is unchanged"
            Expect.exists arr.Events (fun e -> e.Code = "e0") "E0 -> e0"
            Expect.exists arr.Events (fun e -> e.Code = "e1") "E1 -> e1"
            Expect.exists arr.Events (fun e -> e.Code = "e2") "E2 -> e2"

        testCase "Does not change correct crowd events" <| fun _ ->
            let events = ![ Event("E3", 100); Event("E13", 200); Event("D3", 300); Event("E13", 400); ]
            let arr = InstrumentalArrangement(Events = events)

            EOFFixes.fixCrowdEvents arr

            Expect.hasLength arr.Events 4 "Number of events is unchanged"
            Expect.equal arr.Events.[0].Code "E3" "Event #1 code unchanged"
            Expect.equal arr.Events.[1].Code "E13" "Event #2 code unchanged"
            Expect.equal arr.Events.[2].Code "D3" "Event #3 code unchanged"
            Expect.equal arr.Events.[3].Code "E13" "Event #4 code unchanged"

        testCase "Fixes incorrect handshape lengths" <| fun _ ->
            let cn = ![ Note(IsLinkNext = true, SlideTo = 5y, Sustain = 1000) ]
            let chord = Chord(ChordNotes = cn, IsLinkNext = true)
            let hs = HandShape(0s, 0, 1500)
            let levels = ![ Level(Chords = ![ chord ], HandShapes = ![ hs ]) ]
            let arr = InstrumentalArrangement(Levels = levels)

            EOFFixes.fixChordSlideHandshapes arr

            Expect.equal hs.EndTime 1000 "Handshape end time is correct"

        testCase "Moves anchor to the beginning of phrase" <| fun _ ->
            let anchor = Anchor(5y, 700)
            let anchors = ![ anchor ]
            let levels = ![ Level(Anchors = anchors) ]
            let phraseIterations = ![ PhraseIteration(100, 0); PhraseIteration(650, 0); PhraseIteration(1000, 1) ]
            let arr = InstrumentalArrangement(Levels = levels, PhraseIterations = phraseIterations)

            EOFFixes.fixPhraseStartAnchors arr

            Expect.hasLength anchors 1 "Anchor was not copied"
            Expect.equal anchor.Time 650 "Anchor time is correct"

        testCase "Copies active anchor to the beginning of phrase" <| fun _ ->
            let anchor = Anchor(5y, 400, 7y)
            let anchors = ![ anchor ]
            let levels = ![ Level(Anchors = anchors) ]
            let phraseIterations = ![
                PhraseIteration(100, 0)
                PhraseIteration(400, 0)
                PhraseIteration(650, 0)
                PhraseIteration(1000, 1)
            ]
            let arr = InstrumentalArrangement(Levels = levels, PhraseIterations = phraseIterations)

            EOFFixes.fixPhraseStartAnchors arr

            Expect.hasLength anchors 2 "Anchor was copied"
            Expect.equal anchor.Time 400 "Existing anchor time is correct"
            Expect.equal anchors.[1].Time 650 "New anchor time is correct"
            Expect.equal anchors.[1].Fret anchor.Fret "New anchor fret is correct"
            Expect.equal anchors.[1].Width anchor.Width "New anchor width is correct"
    ]
