using build.Benchs;
using floody.common;

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

        public string GetGroupingKey
        {
            get
            {
                var listItem = new List<string>();

                listItem.Add(IsHttps ? "HTTPS" : "HTTP");
                listItem.Add($"{FormatHelper.FormatBytes(ResponseBodySize)}");
                listItem.Add($"{DurationSeconds}s");

                return string.Join(" - ", listItem); 
            }
        }

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