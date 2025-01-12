namespace floody
{
    public class FloodResult
    {
        public FloodResult(int count, int successCount, int failCount, int networkFailCount)
        {
            Count = count;
            SuccessCount = successCount;
            FailCount = failCount;
            NetworkFailCount = networkFailCount;
        }

        public int Count { get; }
        public int SuccessCount { get; }
        public int FailCount { get; }
        public int NetworkFailCount { get; }

        public string PrettyFormat(FloodyOptions floodOptions)
        {
            var totalDuration = floodOptions.StartupSettings.Duration;

            var reqPerSeconds = SuccessCount / totalDuration.TotalSeconds;

            // Format as pretty table for console 

            return $"Total requests: {Count}\n" +
                   $"Success: {SuccessCount}\n" +
                   $"Fail: {FailCount}\n" +
                   $"Network Fail: {NetworkFailCount}\n" +
                   $"Requests per seconds: {reqPerSeconds}";
        }
    }
}