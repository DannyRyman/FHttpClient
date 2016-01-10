// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

//#load "FHttpClient.fs"
//open FHttpClient

// Define your library scripting code here
#r "System.dll"

open System.Text.RegularExpressions;
open System.IO

let regEx = new Regex(@"\s\s+", RegexOptions.IgnoreCase);

let content = File.ReadAllText("C:\\Users\\Dan\\Documents\\test.txt")

let result = regEx.Replace(content.Trim(), " ")

printf "%s" result

