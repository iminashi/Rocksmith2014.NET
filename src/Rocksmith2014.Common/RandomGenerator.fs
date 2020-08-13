module Rocksmith2014.Common.RandomGenerator

open System

let private rand = Random()

let next () = rand.Next()

let nextAlphabet () = char <| rand.Next(int 'a', int 'z' + 1)
