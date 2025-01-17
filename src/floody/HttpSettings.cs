using System.Net;
using System.Text.Json.Serialization;

namespace floody;

public class FloodyOptions
{
    public FloodyOptions(HttpSettings httpSettings, StartupSettings startupSettings)
    {
        HttpSettings = httpSettings;
        StartupSettings = startupSettings;
    }

    public HttpSettings HttpSettings { get; }

    public StartupSettings StartupSettings { get;  }
}

public class HttpSettings
{
    public HttpSettings(Uri uri, string method, int concurrentConnection,
        WebProxy? webProxy, IReadOnlyCollection<Header> additionalHeaders)
    {
        Uri = uri;
        Method = method;
        ConcurrentConnection = concurrentConnection;
        WebProxy = webProxy;
        AdditionalHeaders = additionalHeaders;
    }

    [JsonIgnore]
    public Uri Uri { get; }

    [JsonPropertyName("uri")]
    public string UriString => Uri.ToString();

    public string Method { get; }

    public int ConcurrentConnection { get; }

    [JsonIgnore]
    public WebProxy? WebProxy { get; }

    public IReadOnlyCollection<Header> AdditionalHeaders { get;  }
}


public class Header
{
    public Header(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public string Value { get; }
}