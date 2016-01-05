namespace FakeableHttpClientFixtures

open System
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
            interceptor.ForRequestMatching(HttpMethod.Get, "^http://someurl.com/$")
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