// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

//#load "FHttpClient.fs"
//open FHttpClient

// Define your library scripting code here
#r "System.dll"
#r "System.Net.Http.dll"
#r "System.Net.Http.WebRequest.dll"
#r "mscorlib.dll"

open System.Text.RegularExpressions;
open System.IO
open System.Net.Http
open System.Collections.Generic
open System.Linq

let mySeq = new List<HttpRequestMessage>().As
//let mySeq = seq { for i in 0 .. 10 do yield new HttpRequestMessage() }
let length = Seq.length mySeq
printf "%d" length



let regEx = new Regex(@"\s\s+", RegexOptions.IgnoreCase);

let content = File.ReadAllText("C:\\Users\\Dan\\Documents\\test.txt")

let result = regEx.Replace(content.Trim(), " ")

printf "%s" result

