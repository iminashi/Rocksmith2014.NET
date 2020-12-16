module Rocksmith2014.XML.Processing.ArrangementImprover

/// Adds crowd events to the arrangement if it does not have them.
let addCrowdEvents = CrowdEventAdder.improve
