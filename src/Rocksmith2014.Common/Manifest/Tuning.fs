// fsharplint:disable RecordFieldNames
namespace Rocksmith2014.Common.Manifest

type Tuning =
    { string0: int16
      string1: int16
      string2: int16
      string3: int16
      string4: int16
      string5: int16 }

    member this.ToArray() =
        [| this.string0
           this.string1
           this.string2
           this.string3
           this.string4
           this.string5 |]

    static member Default =
        { string0 = 0s
          string1 = 0s
          string2 = 0s
          string3 = 0s
          string4 = 0s
          string5 = 0s }

    static member FromArray(strings: int16 array) =
        { string0 = strings[0]
          string1 = strings[1]
          string2 = strings[2]
          string3 = strings[3]
          string4 = strings[4]
          string5 = strings[5] }
