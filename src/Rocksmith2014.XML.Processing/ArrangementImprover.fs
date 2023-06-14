module Rocksmith2014.XML.Processing.ArrangementImprover

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

/// Shortens the lengths of a handshapes that are too close to the next one.
let adjustHandShapes = HandShapeAdjuster.improve

/// Applies all the improvements to the arrangement.
let applyAll arrangement =
    BasicFixes.validatePhraseNames arrangement
    BasicFixes.addIgnoreToHighFretNotes arrangement
    BasicFixes.fixLinkNexts arrangement
    BasicFixes.removeOverlappingBendValues arrangement
    movePhrases arrangement
    eofFixes arrangement
    addCrowdEvents arrangement
    processChordNames arrangement
    processCustomEvents arrangement
    removeExtraBeats arrangement
    adjustHandShapes arrangement

/// Applies the basic needed improvements to the arrangement.
let applyMinimum arrangement =
    BasicFixes.validatePhraseNames arrangement
    BasicFixes.addIgnoreToHighFretNotes arrangement
    BasicFixes.fixLinkNexts arrangement
    BasicFixes.removeOverlappingBendValues arrangement
    EOFFixes.fixChordNotes arrangement
    EOFFixes.fixPhraseStartAnchors arrangement
