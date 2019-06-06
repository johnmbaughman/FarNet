﻿[<AutoOpen>]
module FSharpFar.Kit
open System
open System.Text.RegularExpressions

/// Makes a string for one line show.
let strAsLine =
    let re = Regex @"[\r\n\t]+"
    fun x -> re.Replace (x, " ")

/// Zips 2+ spaces into one.
let strZipSpace =
    let re = Regex @"[ \t]{2,}"
    fun x -> re.Replace (x, " ")

/// A function that always returns the same value.
let inline always value = fun _ -> value

/// Gets true if a char is an identifier char.
let isIdentChar char = Char.IsLetterOrDigit char || char = '_' || char = '\''

/// Gets true if a char is a long identifier char.
let isLongIdentChar char = isIdentChar char || char = '.'

/// Gets true if a string is a normal identifier.
let isIdentStr str = String.forall isIdentChar str

module Seq =
    /// Gets true if the string sequence contains the value.
    let containsIgnoreCase value (source: string seq) =
        (source |> Seq.tryFind (fun x -> x.Equals(value, StringComparison.OrdinalIgnoreCase))).IsSome

module String =
    /// Gets true if two strings are equal.
    let equalsIgnoreCase x y =
        String.Equals (x, y, StringComparison.OrdinalIgnoreCase)

    /// Gets true if x ends with y.
    let endsWithIgnoreCase (x: string) (y: string) =
        x.EndsWith (y, StringComparison.OrdinalIgnoreCase)
