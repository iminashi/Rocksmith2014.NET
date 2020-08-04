namespace Rocksmith2014.DLCProject.Manifest

type Section =
    { Name : string
      UIName : string
      Number : int16
      StartTime : float32
      EndTime : float32
      StartPhraseIterationIndex : int16
      EndPhraseIterationIndex : int16
      IsSolo : bool }
