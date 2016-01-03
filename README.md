# FHttpClient

A fakeable http client.  

Allows unit tests to intercept requests and replace them with pre-configured responses.  

This is useful for stubbing out external services.

## Usage 
```c#
var client = new FHttpClient();

using (var interceptor = new HttpInterceptor())
{
    interceptor.ForRequestMatching(HttpMethod.Get, "^http://www.google.com/$")
        .RespondWith(new HttpResponseMessage(HttpStatusCode.BadGateway));
    
    var result = client.GetAsync("http://www.google.com").Result;
    Console.WriteLine(result.StatusCode);
}

Console.ReadLine();
```

## Additional Information

Nuget package:
https://www.nuget.org/packages/FakeableHttpClient/
