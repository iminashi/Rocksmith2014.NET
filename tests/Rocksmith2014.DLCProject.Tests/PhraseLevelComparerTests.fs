module PhraseLevelComparerTests

open Expecto
open Rocksmith2014.DLCProject
open Rocksmith2014.SNG

let private p1 = "phrase1"
let private p2 = "phrase2"

let private testPhrase =
    { Solo = 0y
      Disparity = 0y
      Ignore = 0y
      IterationCount = 1
      MaxDifficulty = 1
      Name = p1 }

let private arrangement = Instrumental testLead

let private storedLevels =
    readOnlyDict [ testLead.PersistentId, readOnlyDict [ p1, 5; p2, 2 ] ]

[<Tests>]
let tests =
    testList "Phrase Level Comparer Tests" [
        test "Detects phrase with less levels than stored" {
            let sng = { SNG.Empty with Phrases = [| testPhrase |] }

            let ids = PhraseLevelComparer.compareLevels storedLevels [ arrangement, sng ]

            Expect.hasLength ids 1 "One ID was returned"
            Expect.equal ids.[0] testLead.Id "Correct ID was returned"
        }

        test "Does not return ID of phrase with more levels than stored" {
            let phrase = { testPhrase with MaxDifficulty = 15; Name = p2 }
            let sng = { SNG.Empty with Phrases = [| phrase |] }

            let ids = PhraseLevelComparer.compareLevels storedLevels [ arrangement, sng ]

            Expect.hasLength ids 0 "No IDs were returned"
        }
    ]
