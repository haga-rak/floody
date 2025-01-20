using System.Net;
using System.Text.Json.Serialization;

namespace floody.common;

public class HttpSettings
{
    public HttpSettings(Uri uri, string method, int concurrentConnection,
        WebProxy? webProxy, IReadOnlyCollection<Header> additionalHeaders, long requestBodyLength, long responseBodyLength)
    {
        Uri = uri;
        Method = method;
        ConcurrentConnection = concurrentConnection;
        WebProxy = webProxy;
        AdditionalHeaders = additionalHeaders;
        RequestBodyLength = requestBodyLength;
        ResponseBodyLength = responseBodyLength;
    }

    [JsonIgnore]
    public Uri Uri { get; }
    
    [JsonIgnore]
    public WebProxy? WebProxy { get; }
    
    [JsonPropertyName("proxy")]
    public string Proxy => WebProxy?.Address?.ToString() ?? string.Empty;

    [JsonPropertyName("uri")]
    public string UriString => Uri.ToString();

    public string Method { get; }

    public int ConcurrentConnection { get; }


    public IReadOnlyCollection<Header> AdditionalHeaders { get; }
    
    public long RequestBodyLength { get; }
    
    public long ResponseBodyLength { get; }
}