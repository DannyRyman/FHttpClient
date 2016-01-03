// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.

#r "System.Net.Http.dll"
open System.Reflection
open System.Net.Http
// Define your library scripting code here

let sendAsync : MethodInfo = 
        typedefof<HttpMessageHandler>.GetMethod("SendAsync", BindingFlags.Instance ||| BindingFlags.NonPublic)

