
using System.Text.Json.Serialization;

namespace floody.common
{
    public class StartupSettings
    {
        public StartupSettings(TimeSpan duration, TimeSpan warmupDuration, FileInfo? outputFile)
        {
            Duration = duration;
            WarmupDuration = warmupDuration;
            OutputFile = outputFile;
        }

        public TimeSpan Duration { get; }

        public TimeSpan WarmupDuration { get; }

        [JsonIgnore] public FileInfo? OutputFile { get; }

        [JsonPropertyName("outputFile")] public string? OutputFileString => OutputFile?.FullName;
    }

}