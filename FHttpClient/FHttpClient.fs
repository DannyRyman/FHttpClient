namespace FakeableHttpClient

open System
open System.Net.Http
open System.Threading.Tasks
open System.Net.Http.Headers
open System.Threading
open System.Reflection
open System.Runtime.Remoting.Messaging
open System.Text.RegularExpressions
open System.Collections.Generic

type internal IRetrieveRecordedResponses = 
    abstract member GetNextResponse : HttpRequestMessage -> HttpResponseMessage

module internal state =
    let key = "interceptor"
    let SetInterceptor(interceptor : IRetrieveRecordedResponses) =   
        CallContext.LogicalSetData(key, interceptor)
    let GetInterceptor() =
        let interceptor = CallContext.LogicalGetData(key)
        let interceptorOption = match interceptor with null -> None | _ -> Some(interceptor :?> IRetrieveRecordedResponses)  
        interceptorOption
    let RemoveInterceptor() =
        CallContext.FreeNamedDataSlot(key)

type internal FHttpClientHandler(inner:HttpMessageHandler) =
    inherit HttpMessageHandler()
    let sendAsync : MethodInfo = 
        inner.GetType().GetMethod("SendAsync", BindingFlags.NonPublic ||| BindingFlags.Instance)
    override this.SendAsync(request, cancellationToken) : Task<HttpResponseMessage> =
        let response = match state.GetInterceptor() with 
            | None -> sendAsync.Invoke(inner, [|request; cancellationToken|]) :?> Task<HttpResponseMessage> 
            | Some i -> async { 
                let response = i.GetNextResponse(request) 
                return response } |> Async.StartAsTask
        response
            
type FHttpClient(handler, disposeHandler) =
    inherit HttpClient(new FHttpClientHandler(handler), disposeHandler)    
    new(handler) = new FHttpClient(handler, true)
    new() = new FHttpClient(new HttpClientHandler(), true)

type internal ExpectedRequest = { httpMethod : HttpMethod; urlRegEx : Regex }

type internal ConfiguredResponseEntry = { Request : ExpectedRequest; Response : HttpResponseMessage }

type HttpInterceptorRequestSetup internal (configuredResponses : List<ConfiguredResponseEntry>, request : ExpectedRequest) =    
    member this.RespondWith(response:HttpResponseMessage) =
        let configuredResponse = { Request = request; Response = response}
        configuredResponses.Add(configuredResponse)        

exception FHttpClientException of string
    with 
        override this.Message =           
            this.Data0

type HttpInterceptor() as this =
    do state.SetInterceptor(this)
    
    let configuredResponses = new List<ConfiguredResponseEntry>()   

    interface IRetrieveRecordedResponses with
        member this.GetNextResponse(request : HttpRequestMessage) = 
            let nextResponse = 
                configuredResponses 
                |> Seq.where (fun c -> c.Request.httpMethod = request.Method && c.Request.urlRegEx.IsMatch(request.RequestUri.AbsoluteUri))
                |> Seq.tryLast
            match nextResponse with
                | None -> 
                    let requestMethod = request.Method.ToString()
                    let errorMessage = sprintf "An call was encounted that could not be matched. HttpMethod: %s; Url: %s" requestMethod request.RequestUri.AbsoluteUri
                    raise(FHttpClientException errorMessage)
                | Some i -> i.Response 

    interface IDisposable with
        member this.Dispose() = 
            configuredResponses.Clear()
            state.RemoveInterceptor()
    
    member this.ForRequestMatching(httpMethod : HttpMethod, urlRegEx : string) =
        let expectedRequest = { httpMethod = httpMethod; urlRegEx = new Regex(urlRegEx, RegexOptions.CultureInvariant ||| RegexOptions.IgnoreCase)}         
        HttpInterceptorRequestSetup(configuredResponses, expectedRequest)

