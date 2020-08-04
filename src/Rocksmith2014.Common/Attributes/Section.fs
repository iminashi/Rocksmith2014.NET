namespace Rocksmith2014.Common.Attributes

type Section =
    { Name : string
      UIName : string
      Number : int16
      StartTime : float32
      EndTime : float32
      StartPhraseIterationIndex : int16
      EndPhraseIterationIndex : int16
      IsSolo : bool }
