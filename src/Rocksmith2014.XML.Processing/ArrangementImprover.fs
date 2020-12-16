module Rocksmith2014.XML.Processing.ArrangementImprover

/// Adds crowd events to the arrangement if it does not have them.
let addCrowdEvents = CrowdEventAdder.improve

/// Processes the chord names in the arrangement.
let processChordNames = ChordNameProcessor.improve

/// Removes beats that come after the audio has ended.
let removeExtraBeats = ExtraBeatRemover.improve

/// Applies various minor fixes.
let eofFixes = EOFFixes.fixAll
