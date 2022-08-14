module FlagBuilder

type FlagBuilder () =
    member inline _.Yield(f) = f
    member inline _.Zero() = LanguagePrimitives.EnumOfValue(LanguagePrimitives.GenericZero)
    member inline _.Combine(f1, f2) = f1 ||| f2
    member inline _.Delay(f) = f()

let flags = FlagBuilder()
