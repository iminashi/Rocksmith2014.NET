module Rocksmith2014.XML.Processing.ArrangementImprover

open Rocksmith2014.XML

/// Adds crowd events to the arrangement if it does not have them.
let addCrowdEvents = CrowdEventAdder.improve

/// Processes the chord names in the arrangement.
let processChordNames = ChordNameProcessor.improve

/// Removes beats that come after the audio has ended.
let removeExtraBeats = ExtraBeatRemover.improve

/// Applies various minor fixes.
let eofFixes = EOFFixes.fixAll

/// Moves phrases that have a special name "mover" (move right).
let movePhrases = PhraseMover.improve

/// Processes custom events.
let processCustomEvents = CustomEvents.improve

/// Applies all the improvements to the arrangement.
let applyAll (arrangement: InstrumentalArrangement) =
    BasicFixes.validatePhraseNames arrangement
    BasicFixes.addIgnores arrangement
    BasicFixes.fixLinkNexts arrangement
    BasicFixes.removeOverlappingBendValues arrangement
    BasicFixes.removeMutedNotesFromChords arrangement
    // Do before phrase start anchors are fixed
    AnchorMover.improve arrangement
    movePhrases arrangement
    eofFixes arrangement
    // Should be done after fixing the anchors at phrase start
    BasicFixes.removeRedundantAnchors arrangement
    addCrowdEvents arrangement
    processChordNames arrangement
    processCustomEvents arrangement
    removeExtraBeats arrangement
    HandShapeAdjuster.lengthenHandshapes arrangement
    HandShapeAdjuster.shortenHandshapes arrangement

/// Applies the basic needed improvements to the arrangement.
let applyMinimum (arrangement: InstrumentalArrangement) =
    BasicFixes.validatePhraseNames arrangement
    BasicFixes.addIgnores arrangement
    BasicFixes.removeOverlappingBendValues arrangement
    BasicFixes.removeMutedNotesFromChords arrangement
    EOFFixes.fixChordNotes arrangement
    EOFFixes.fixPhraseStartAnchors arrangement
