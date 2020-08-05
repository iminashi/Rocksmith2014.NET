namespace Rocksmith2014.Common.Attributes

type Section =
    { Name : string
      UIName : string
      Number : int32
      StartTime : float32
      EndTime : float32
      StartPhraseIterationIndex : int32
      EndPhraseIterationIndex : int32
      IsSolo : bool }
