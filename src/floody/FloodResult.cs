using System.Xml.Linq;

namespace floody
{
    public class FloodResult
    {
        public FloodResult(int count, int successCount, int httpFailCount, int networkFailCount, FloodyOptions options)
        {
            Count = count;
            SuccessCount = successCount;
            HttpFailCount = httpFailCount;
            NetworkFailCount = networkFailCount;
            Options = options;
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

        public string PrettyFormat()
        {
            return $"Total requests: {Count}\n" +
                   $"Success: {SuccessCount}\n" +
                   $"Fail: {HttpFailCount}\n" +
                   $"Network Fail: {NetworkFailCount}\n" +
                   $"Requests per seconds: {RequestPerSeconds}";
        }
    }
}