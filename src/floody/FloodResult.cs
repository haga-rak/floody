using System.Text.Json;
using System.Xml.Linq;

namespace floody
{
    public class FloodResult
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public FloodResult(int count, int successCount, int httpFailCount, int networkFailCount, FloodyOptions options, long totalReceivedBytes)
        {
            Count = count;
            SuccessCount = successCount;
            HttpFailCount = httpFailCount;
            NetworkFailCount = networkFailCount;
            Options = options;
            TotalReceivedBytes = totalReceivedBytes;
        }
        public FloodyOptions Options { get; }

        public int Count { get; }

        public int SuccessCount { get; }

        public int HttpFailCount { get; }

        public int NetworkFailCount { get; }

        public double RequestPerSeconds
        {
            get
            {
                var totalDuration = Options.StartupSettings.Duration;
                var reqPerSeconds = SuccessCount / totalDuration.TotalSeconds;
                return reqPerSeconds;
            }
        }

        public long TotalReceivedBytes { get; }

        public string TotalReceivedPerSeconds
        {
            get
            {
                var totalDuration = Options.StartupSettings.Duration;
                var reqPerSeconds = TotalReceivedBytes / totalDuration.TotalSeconds;
                return $"{FormatHelper.FormatBytes(reqPerSeconds)}/s";
            }
        }


        public string PrettyFormat()
        {
            return JsonSerializer.Serialize(this, JsonSerializerOptions);
        }
    }
}