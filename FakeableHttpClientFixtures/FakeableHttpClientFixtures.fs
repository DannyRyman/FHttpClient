namespace FakeableHttpClientFixtures

open System
open System.Text.RegularExpressions
open System.Net
open System.Net.Http
open NUnit.Framework
open FakeableHttpClient

[<TestFixture>]
type FakeableHttpClientFixture() =          
    let client = new FHttpClient()

    [<Test>] 
    member this.``must be a able to fake requests``() =         
        using (new HttpInterceptor()) ( fun interceptor ->
            interceptor.ForRequestMatching(HttpMethod.Get, "http://someurl.com/")
                .RespondWith(new HttpResponseMessage(HttpStatusCode.Forbidden))
            let response = client.GetAsync("http://someurl.com").Result
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden))
        ) 

    [<Test>]
    member this.``must record requests``() =
        using (new HttpInterceptor()) ( fun interceptor ->
            interceptor.ForRequestMatching(HttpMethod.Post, "http://someurl.com/")
                .RespondWith(new HttpResponseMessage(HttpStatusCode.Forbidden))

            let request = new HttpRequestMessage(HttpMethod.Post, "http://someurl.com/")
            request.Content <- new StringContent("test")

            let response = client.SendAsync(request).Result
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden))

            Assert.That(Seq.length interceptor.RequestsLogged, Is.EqualTo(1))            
            let loggedRequest = Seq.head interceptor.RequestsLogged
            let loggedRequestContent = loggedRequest.Content.ReadAsStringAsync().Result
            Assert.AreEqual("test", loggedRequestContent)            
        )
    
    [<Test>]
    member this.``must be able to match on request content using regex (no match found)``() =
        using (new HttpInterceptor()) ( fun interceptor ->
            interceptor.ForRequestMatching(HttpMethod.Post, "http://someurl.com/")
                .WithRequestContentMatching(new Regex("^match$"))
                .RespondWith(new HttpResponseMessage(HttpStatusCode.Forbidden))
           
            let request = new HttpRequestMessage(HttpMethod.Post, "http://someurl.com") 
            request.Content <- new StringContent("nomatch")
            let thrownException = Assert.Throws<FHttpClientException>(fun() -> 
                client.SendAsync(request).Result |> ignore
                )

            Assert.That(thrownException.Message, Is.EqualTo("An call was encounted that could not be matched. HttpMethod: POST; Url: http://someurl.com/"))
        )

    [<Test>]
    member this.``must be able to match on request content using regex (match found)``() =
        using (new HttpInterceptor()) ( fun interceptor ->
            interceptor.ForRequestMatching(HttpMethod.Post, "http://someurl.com/")
                .WithRequestContentMatching(new Regex("^match$"))
                .RespondWith(new HttpResponseMessage(HttpStatusCode.Forbidden))
           
            let request = new HttpRequestMessage(HttpMethod.Post, "http://someurl.com") 
            request.Content <- new StringContent("match")            

            let response = client.SendAsync(request).Result
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden))            
        )

    [<Test>]
    member this.``must be able to match on request content (no match found)``() =
        using (new HttpInterceptor()) ( fun interceptor ->
            interceptor.ForRequestMatching(HttpMethod.Post, "http://someurl.com/")
                .WithRequestContentMatching("match")
                .RespondWith(new HttpResponseMessage(HttpStatusCode.Forbidden))
           
            let request = new HttpRequestMessage(HttpMethod.Post, "http://someurl.com") 
            request.Content <- new StringContent("nomatch")
            let thrownException = Assert.Throws<FHttpClientException>(fun() -> 
                client.SendAsync(request).Result |> ignore
                )

            Assert.That(thrownException.Message, Is.EqualTo("An call was encounted that could not be matched. HttpMethod: POST; Url: http://someurl.com/"))
        )

    [<Test>]
    member this.``must be able to match on request content (match found)``() =
        using (new HttpInterceptor()) ( fun interceptor ->
            interceptor.ForRequestMatching(HttpMethod.Post, "http://someurl.com/")
                .WithRequestContentMatching("match")
                .RespondWith(new HttpResponseMessage(HttpStatusCode.Forbidden))
           
            let request = new HttpRequestMessage(HttpMethod.Post, "http://someurl.com") 
            request.Content <- new StringContent("match")            

            let response = client.SendAsync(request).Result
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden))            
        )

    [<Test>]
    [<TestCase(" { \"sample\": \"sampleValue\" } ", "{\"sample\"   :   \"samplevalue\"}")>]
    [<TestCase("\tTest", "test")>]
    [<TestCase("test", "\tTest")>]
    [<TestCase("\nTest", "test")>]
    [<TestCase("test", "\nTest")>]
    member this.``match a range of literal matches against the request content`` (requestContent:string) (matchContent:string) =
        using (new HttpInterceptor()) ( fun interceptor ->
            interceptor.ForRequestMatching(HttpMethod.Post, "http://someurl.com/")
                .WithRequestContentMatching(matchContent)
                .RespondWith(new HttpResponseMessage(HttpStatusCode.Forbidden))
           
            let request = new HttpRequestMessage(HttpMethod.Post, "http://someurl.com") 
            request.Content <- new StringContent(requestContent)            

            let response = client.SendAsync(request).Result
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden))            
        )

    [<Test>]
    [<TestCase("http://www.someurl.com/?queryStr1=one&queryString2=two")>]
    member this.``match a range of literal matches against the url`` (url:string) =
        using (new HttpInterceptor()) ( fun interceptor ->
            interceptor.ForRequestMatching(HttpMethod.Get, url)                
                .RespondWith(new HttpResponseMessage(HttpStatusCode.Forbidden))
           
            let response = client.GetAsync(url).Result
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden))            
        )

    [<Test>]
    member this.``match url using regex``() =
        using (new HttpInterceptor()) ( fun interceptor ->
            interceptor.ForRequestMatching(HttpMethod.Get, new Regex(".*someurl.*"))                
                .RespondWith(new HttpResponseMessage(HttpStatusCode.Forbidden))
           
            let response = client.GetAsync("http://someurl.com").Result
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden))            
        )

    [<Test>]
    member this.``must raise a meaningful exception when no request was matched``() =        
        using (new HttpInterceptor()) ( fun interceptor ->            
            let thrownException = Assert.Throws<FHttpClientException>(fun() -> 
                client.GetAsync("http://someurl.com").Result |> ignore                                                
                )       
       
            Assert.That(thrownException.Message, Is.EqualTo("An call was encounted that could not be matched. HttpMethod: GET; Url: http://someurl.com/"))
        )

    [<Test>]
    member this.``must be able to make real calls``() =
        let response = client.GetAsync("http://www.google.com").Result
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK))