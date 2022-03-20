[<RequireQualifiedAccess>]
module String

open System

/// Returns true if the string is not null or whitespace.
let notEmpty = String.IsNullOrWhiteSpace >> not

/// Compares the two strings for equality, ignoring case.
let equalsIgnoreCase (str1: string) (str2: string) =
    str1.Equals(str2, StringComparison.OrdinalIgnoreCase)

/// Returns true if the string starts with the given value (case insensitive).
let startsWith prefix (str: string) =
    notNull str && str.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)

/// Returns true if the string ends with the given value (case insensitive).
let endsWith suffix (str: string) =
    notNull str && str.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)

/// Returns true if the string contains the given value (case sensitive).
let inline contains (substr: string) (str: string) =
    str.Contains(substr, StringComparison.Ordinal)

/// Returns true if the string contains the given value (case insensitive).
let inline containsIgnoreCase (substr: string) (str: string) =
    str.Contains(substr, StringComparison.OrdinalIgnoreCase)

/// Returns the string if it is shorter than the max length, otherwise a substring of it.
let truncate (maxLength: int) (str: string) =
    if str.Length > maxLength then
        str.Substring(0, maxLength)
    else
        str
