using System.Net;
using System.Text.Json.Serialization;

namespace floody.common;

public class HttpSettings
{
    public HttpSettings(string uriString, string method, int concurrentConnection, string? proxy,
        IReadOnlyCollection<Header> additionalHeaders,
        long requestBodyLength, long responseBodyLength, bool httpConnect = false)
    {
        UriString = uriString;
        Method = method;
        ConcurrentConnection = concurrentConnection;
        AdditionalHeaders = additionalHeaders;
        RequestBodyLength = requestBodyLength;
        ResponseBodyLength = responseBodyLength;
        Proxy = proxy;
        HttpConnect = httpConnect;
    }

    public Uri GetUri()
    {
        return new Uri(UriString);
    }

    public WebProxy? GetWebProxy()
    {
        return Proxy == null ? null : new WebProxy(Proxy);
    }

    [JsonPropertyName("proxy")]
    public string? Proxy { get; }

    [JsonPropertyName("httpConnect")]
    public bool HttpConnect { get; }

    [JsonPropertyName("uri")]
    public string UriString { get; }

    public string Method { get; }

    public int ConcurrentConnection { get; }

    public IReadOnlyCollection<Header> AdditionalHeaders { get; }

    public long RequestBodyLength { get; }

    public long ResponseBodyLength { get; }
}