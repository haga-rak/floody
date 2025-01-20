using build.Benchs;

namespace build
{
    public class BenchmarkConfig
    {
        public BenchmarkConfig(string? proxyUri, int responseBodySize, bool isHttps, int durationSeconds)
        {
            ProxyUri = proxyUri;
            ResponseBodySize = responseBodySize;
            IsHttps = isHttps;
            DurationSeconds = durationSeconds;
        }

        public string ? ProxyUri { get; }
        
        public int ResponseBodySize { get; }
        
        public bool IsHttps { get; }
        public int DurationSeconds { get; }

        public string ToFileName()
        {
            var plainArgs = new List<string>();
            
            if (ProxyUri != null)
            {
                plainArgs.Add($"{ProxyUri}");
            }
            
            plainArgs.Add($"Size-{ResponseBodySize}");
            
            if (IsHttps)
            {
                plainArgs.Add("HTTPS");
            }
            else
            {
                plainArgs.Add("HTTP");
            }

            return string.Join("_", plainArgs).StripeInvalidPathChars() + ".json";
        }
        
        public string ToFloodyArgs(string outDirectory, out string fileName)
        {
            Directory.CreateDirectory(outDirectory);

            fileName = Path.Combine(outDirectory, ToFileName());
            
            var plainArgs = new List<string>() { };
            
            if (ProxyUri != null)
            {
                plainArgs.Add($"-x {ProxyUri}");
            }
            
            plainArgs.Add($"-l {ResponseBodySize}");
            plainArgs.Add($"-d {DurationSeconds}");
            plainArgs.Add($"-o \"{fileName}\"");

            return string.Join(" ", plainArgs);
        }
    }
}