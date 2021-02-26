# Proxy HTTP Client Configuration

Introduced: preview5

## Introduction

Each [Cluster](xref:Microsoft.ReverseProxy.Abstractions.Cluster) has a dedicated [HttpMessageInvoker](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessageinvoker?view=netcore-3.1) instance used to proxy requests to its [Destination](xref:Microsoft.ReverseProxy.Abstractions.Destination)s. The configuration is defined per cluster. On YARP startup, all `Clusters` get new `HttpMessageInvoker` instances, however if later the `Cluster` configuration gets changed the [IProxyHttpClientFactory](xref:Microsoft.ReverseProxy.Service.Proxy.Infrastructure.IProxyHttpClientFactory) will re-run and decide if it should create a new `HttpMessageInvoker` or keep using the existing one. The default `IProxyHttpClientFactory` implementation creates a new `HttpMessageInvoker` when there are changes to the [ProxyHttpClientOptions](xref:Microsoft.ReverseProxy.Abstractions.ProxyHttpClientOptions).

Properties of outgoing requests for a given cluster can be configured as well. They are defined in [ProxyHttpRequestOptions](xref:Microsoft.ReverseProxy.Abstractions.ProxyHttpRequestOptions).

The configuration is represented differently if you're using the [IConfiguration](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfiguration?view=dotnet-plat-ext-3.1) model or the code-first model.

## IConfiguration
These types are focused on defining serializable configuration. The code based configuration model is described below in the "Code Configuration" section.

### HttpClient
HTTP client configuration is based on [ProxyHttpClientOptions](xref:Microsoft.ReverseProxy.Abstractions.ProxyHttpClientOptions) and represented by the following configuration schema.
```JSON
"HttpClient": {
    "SslProtocols": [ "<protocol-names>" ],
    "MaxConnectionsPerServer": "<int>",
    "DangerousAcceptAnyServerCertificate": "<bool>",
    "ClientCertificate": {
        "Path": "<string>",
        "KeyPath": "<string>",
        "Password": "<string>",
        "Subject": "<string>",
        "Store": "<string>",
        "Location": "<string>",
        "AllowInvalid": "<bool>"
    },
    "RequestHeaderEncoding": "<encoding-name>"
}
```

Configuration settings:
- SslProtocols - [SSL protocols](https://docs.microsoft.com/en-us/dotnet/api/system.security.authentication.sslprotocols?view=netcore-3.1) enabled on the given HTTP client. Protocol names are specified as array of strings. Default value is [None](https://docs.microsoft.com/en-us/dotnet/api/system.security.authentication.sslprotocols?view=netcore-3.1#System_Security_Authentication_SslProtocols_None).
```JSON
"SslProtocols": [
    "Tls11",
    "Tls12"
]
```
- MaxConnectionsPerServer - maximal number of HTTP 1.1 connections open concurrently to the same server. Default value is [int32.MaxValue](https://docs.microsoft.com/en-us/dotnet/api/system.int32.maxvalue?view=netcore-3.1).
```JSON
"MaxConnectionsPerServer": "10"
```
- DangerousAcceptAnyServerCertificate - indicates whether the server's SSL certificate validity is checked by the client. Setting it to `true` completely disables validation. Default value is `false`.
```JSON
"DangerousAcceptAnyServerCertificate": "true"
```
- ClientCertificate - specifies a client [X509Certificate](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate?view=netcore-3.1) certificate used to authenticate client on the server. Default value is `null`. There are 3 supported certificate formats
    - PFX file and optional password
    - PEM file and the key with an optional password
    - Certificate subject, store and location as well as `AllowInvalid` flag indicating whether or not an invalid certificate accepted
```JSON
// PFX file
"ClientCertificate": {
    "Path": "my-client-cert.pfx",
    "Password": "1234abc"
}

// PEM file
"ClientCertificate": {
    "Path": "my-client-cert.pem",
    "KeyPath": "my-client-cert.key",
    "Password": "1234abc"
}

// Subject, store and location
"ClientCertificate": {
    "Subject": "MyClientCert",
    "Store": "AddressBook",
    "Location": "LocalMachine",
    "AllowInvalid": "true"
}

```
- RequestHeaderEncoding - enables other than ASCII encoding for outgoing request headers. Setting this value will leverage [`SocketsHttpHandler.RequestHeaderEncodingSelector`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.requestheaderencodingselector?view=net-5.0) and use the selected encoding for all headers. If you need more granular approach, please use custom `IProxyHttpClientFactory`. The value is then parsed by [`Encoding.GetEncoding`](https://docs.microsoft.com/en-us/dotnet/api/system.text.encoding.getencoding?view=net-5.0#System_Text_Encoding_GetEncoding_System_String_), use values like: "utf-8", "iso-8859-1", etc. **This setting is only available for .NET 5.0.**
```JSON
"RequestHeaderEncoding": "utf-8"
```
If you're using an encoding other than ASCII (or UTF-8 for Kestrel) you also need to set your server to accept requests with such headers. For example, use [`KestrelServerOptions.RequestHeaderEncodingSelector`](https://docs.microsoft.com/en-us/dotnet/api/Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions.RequestHeaderEncodingSelector?view=aspnetcore-5.0) to set up Kestrel to accept Latin1 ("iso-8859-1") headers:
```C#
private static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>()
                      .ConfigureKestrel(kestrel =>
                      {
                          kestrel.RequestHeaderEncodingSelector = _ => Encoding.Latin1;
                      });
        );
```

For .NET Core 3.1, Latin1 ("iso-8859-1") is the only non-ASCII header encoding that can be accepted and only via `appsettings.json`:
```JSON
{
    "Kestrel":
    {
        "Latin1RequestHeaders": true
    }
}
```
together with an application wide switch:
```C#
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.AllowLatin1Headers", true);
```

At the moment, there is no solution for changing encoding for response headers in Kestrel (see [aspnetcore#26334](https://github.com/dotnet/aspnetcore/issues/26334)), only ASCII is accepted.


### HttpRequest
HTTP request configuration is based on [ProxyHttpRequestOptions](xref:Microsoft.ReverseProxy.Abstractions.ProxyHttpRequestOptions) and represented by the following configuration schema.
```JSON
"HttpRequest": {
    "Timeout": "<timespan>",
    "Version": "<string>",
    "VersionPolicy": ["RequestVersionOrLower", "RequestVersionOrHigher", "RequestVersionExact"]
}
```

Configuration settings:
- Timeout - the timeout for the outgoing request sent by [HttpMessageInvoker.SendAsync](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessageinvoker.sendasync?view=netcore-3.1). If not specified, 100 seconds is used.
- Version - outgoing request [version](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httprequestmessage.version?view=netcore-3.1). The supported values at the moment are `1.0`, `1.1` and `2`. Default value is 2.
- VersionPolicy - defines how the final version is selected for the outgoing requests. This feature is available from .NET 5.0, see [HttpRequestMessage.VersionPolicy](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httprequestmessage.versionpolicy?view=net-5.0). The default value is `RequestVersionOrLower`.


## Configuration example
The below example shows 2 samples of HTTP client and request configurations for `cluster1` and `cluster2`.

```JSON
{
    "Clusters": {
        "cluster1": {
            "LoadBalancingPolicy": "Random",
            "HttpClient": {
                "SslProtocols": [
                    "Tls11",
                    "Tls12"
                ],
                "MaxConnectionsPerServer": "10",
                "DangerousAcceptAnyServerCertificate": "true"
            },
            "HttpRequest": {
                "Timeout": "00:00:30"
            },
            "Destinations": {
                "cluster1/destination1": {
                    "Address": "https://localhost:10000/"
                },
                "cluster1/destination2": {
                    "Address": "http://localhost:10010/"
                }
            }
        },
        "cluster2": {
            "HttpClient": {
                "SslProtocols": [
                    "Tls12"
                ],
                "ClientCertificate": {
                    "Path": "my-client-cert.pem",
                    "KeyPath": "my-client-cert.key",
                    "Password": "1234abc"
                }
            },
            "HttpRequest": {
                "Version": "1.1",
                "VersionPolicy": "RequestVersionExact"
            },
            "Destinations": {
                "cluster2/destination1": {
                    "Address": "https://localhost:10001/"
                }
            }
        }
    }
}
```

## Code Configuration
HTTP client configuration abstraction consists of the only type [ProxyHttpClientOptions](xref:Microsoft.ReverseProxy.Abstractions.ProxyHttpClientOptions) defined as follows.

```C#
public sealed class ProxyHttpClientOptions
{
    public List<SslProtocols> SslProtocols { get; set; }

    public bool DangerousAcceptAnyServerCertificate { get; set; }

    public X509Certificate ClientCertificate { get; set; }

    public int? MaxConnectionsPerServer { get; set; }
}
```

Note that instead of defining certificate location as it was in the config model, this type exposes a fully constructed [X509Certificate](xref:System.Security.Cryptography.X509Certificates.X509Certificate) certificate. Conversion from the configuration contract to the abstraction model is done by a [IProxyConfigProvider](xref:Microsoft.ReverseProxy.Service.IProxyConfigProvider) which loads a client certificate into memory.

The following is an example of `ProxyHttpClientOptions` using [code based](configproviders.md) configuration. An instance of `ProxyHttpClientOptions` is assigned to the [Cluster.HttpClient](xref:Microsoft.ReverseProxy.Abstractions.Cluster) property before passing the `Cluster` array to `LoadFromMemory` method.

```C#
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();
    var routes = new[]
    {
        new ProxyRoute()
        {
            RouteId = "route1",
            ClusterId = "cluster1",
            Match =
            {
                Path = "{**catch-all}"
            }
        }
    };
    var clusters = new[]
    {
        new Cluster()
        {
            Id = "cluster1",
            Destinations =
            {
                { "destination1", new Destination() { Address = "https://localhost:10000" } }
            },
            HttpClient = new ProxyHttpClientOptions { MaxConnectionsPerServer = 10, SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 }
        }
    };

    services.AddReverseProxy()
        .LoadFromMemory(routes, clusters)
        .AddProxyConfigFilter<CustomConfigFilter>();
}
```

## Custom IProxyHttpClientFactory
In case the direct control on a proxy HTTP client construction is necessary, the default [IProxyHttpClientFactory](xref:Microsoft.ReverseProxy.Service.Proxy.Infrastructure.IProxyHttpClientFactory) can be replaced with a custom one. In example, that custom logic can use [Cluster](xref:Microsoft.ReverseProxy.Abstractions.Cluster)'s metadata as an extra data source for [HttpMessageInvoker](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpmessageinvoker?view=netcore-3.1) configuration. However, it's still recommended for any custom factory to set the following `HttpMessageInvoker` properties to the same values as the default factory does in order to preserve a correct reverse proxy behavior.

Always return an HttpMessageInvoker instance rather than an HttpClient instance which derives from HttpMessageInvoker. HttpClient buffers responses by default which breaks streaming scenarios and increases memory usage and latency.

```C#
new SocketsHttpHandler
{
    UseProxy = false,
    AllowAutoRedirect = false,
    AutomaticDecompression = DecompressionMethods.None,
    UseCookies = false
};
```

The below is an example of a custom `IProxyHttpClientFactory` implementation.

```C#
public class CustomProxyHttpClientFactory : IProxyHttpClientFactory
{
    public HttpMessageInvoker CreateClient(ProxyHttpClientContext context)
    {
        if (context.OldClient != null && context.NewOptions == context.OldOptions)
        {
            return context.OldClient;
        }

        var newClientOptions = context.NewOptions;
        
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false
        };

        if (newClientOptions.SslProtocols.HasValue)
        {
            handler.SslOptions.EnabledSslProtocols = newClientOptions.SslProtocols.Value;
        }
        if (newClientOptions.ClientCertificate != null)
        {
            handler.SslOptions.ClientCertificates = new X509CertificateCollection
            {
                newClientOptions.ClientCertificate
            };
        }
        if (newClientOptions.MaxConnectionsPerServer != null)
        {
            handler.MaxConnectionsPerServer = newClientOptions.MaxConnectionsPerServer.Value;
        }
        if (newClientOptions.DangerousAcceptAnyServerCertificate)
        {
            handler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) => cert.Subject == "dev.mydomain";
        }
#if NET
        if (newClientOptions.RequestHeaderEncoding != null)
        {
            handler.RequestHeaderEncodingSelector = (_, _) => newClientOptions.RequestHeaderEncoding;
        }
#endif

        return new HttpMessageInvoker(handler, disposeHandler: true);
    }
}
```
