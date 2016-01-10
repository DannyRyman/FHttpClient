﻿namespace FakeableHttpClient

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
        match interceptor with :? IRetrieveRecordedResponses as x -> Some x | _ -> None
    let RemoveInterceptor() =
        CallContext.FreeNamedDataSlot(key)


type internal FHttpClientHandler(inner:HttpMessageHandler) =
    inherit DelegatingHandler(inner)    
    override this.SendAsync(request, cancellationToken) : Task<HttpResponseMessage> =
        match state.GetInterceptor() with 
        | None -> base.SendAsync(request, cancellationToken)
        | Some i -> Task.FromResult(i.GetNextResponse(request))
         
            
type FHttpClient(handler, disposeHandler) =
    inherit HttpClient(new FHttpClientHandler(handler), disposeHandler)    
    new(handler) = new FHttpClient(handler, true)
    new() = new FHttpClient(new HttpClientHandler(), true)


module internal regexHelper =
    let stripExcessWhitespace (strValue:string) = 
        let regex = new Regex(@"\s+(?=([^""]*""[^""]*"")*[^""]*$)") // \s+(?=([^"]*"[^"]*")*[^"]*$)
        regex.Replace(strValue.Trim(), "")    
    let buildLiteralRegex literalString : Regex =          
        literalString 
        |> stripExcessWhitespace
        |> Regex.Escape       
        |> sprintf "^%s$"
        |> fun str -> new Regex(str, RegexOptions.IgnoreCase)


type internal ExpectedRequest = { httpMethod : HttpMethod; urlRegEx : Regex; requestContentRegex : Regex option }


type internal ConfiguredResponseEntry = { Request : ExpectedRequest; Response : HttpResponseMessage }


type HttpInterceptorRequestSetup internal (configuredResponses : List<ConfiguredResponseEntry>, request : ExpectedRequest) =    
    member this.RespondWith(response:HttpResponseMessage) =
        let configuredResponse = { Request = request; Response = response}
        configuredResponses.Add(configuredResponse)   
    member this.WithRequestContentMatching(contentMatchRegEx:Regex) =
        let expectedRequestWithContent = { httpMethod = request.httpMethod; urlRegEx = request.urlRegEx; requestContentRegex = Some(contentMatchRegEx) }
        HttpInterceptorRequestSetup(configuredResponses, expectedRequestWithContent)
    member this.WithRequestContentMatching(contentMatch:string) =        
        let literalMatchRegex = regexHelper.buildLiteralRegex contentMatch
        this.WithRequestContentMatching(literalMatchRegex)


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
                |> Seq.where (fun c -> c.Request.httpMethod = request.Method 
                                    && c.Request.urlRegEx.IsMatch(request.RequestUri.AbsoluteUri)
                                    && match c.Request.requestContentRegex with 
                                        | option.None -> true
                                        | option.Some regex -> 
                                            let resultWithoutWhitespace = regexHelper.stripExcessWhitespace(request.Content.ReadAsStringAsync().Result)
                                            regex.IsMatch(resultWithoutWhitespace)
                                    )
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
    
    member this.ForRequestMatching(httpMethod : HttpMethod, url : string) =
        this.ForRequestMatching(httpMethod, regexHelper.buildLiteralRegex(url))        

    member this.ForRequestMatching(httpMethod : HttpMethod, urlRegex : Regex) =
        let expectedRequest = { httpMethod = httpMethod; urlRegEx = urlRegex; requestContentRegex = None }         
        HttpInterceptorRequestSetup(configuredResponses, expectedRequest)

    member this.Dispose() = (this :> IDisposable).Dispose()




