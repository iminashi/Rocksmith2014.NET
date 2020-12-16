module Rocksmith2014.XML.Processing.Utils

let timeToString time =
    let minutes = time / 1000 / 60
    let seconds = (time / 1000) - (minutes * 60)
    let milliSeconds = time - (minutes * 60 * 1000) - (seconds * 1000)
    $"{minutes:D2}:{seconds:D2}.{milliSeconds:D3}"
