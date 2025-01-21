
using System.Text.Json.Serialization;

namespace floody.common
{
    public class StartupSettings
    {
        public StartupSettings(TimeSpan duration, TimeSpan warmupDuration, string? outputFileString)
        {
            Duration = duration;
            WarmupDuration = warmupDuration;
            OutputFileString = outputFileString;
        }

        public TimeSpan Duration { get; }

        public double DurationSeconds => Duration.TotalSeconds;

        public TimeSpan WarmupDuration { get; }

        public double WarmupDurationSeconds => WarmupDuration.TotalSeconds;

        [JsonIgnore]
        public FileInfo? OutputFile => OutputFileString == null ? null : new FileInfo(OutputFileString);

        [JsonPropertyName("outputFile")]
        public string? OutputFileString { get; }
    }

}