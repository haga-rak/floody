namespace floody
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

        public FileInfo ? OutputFile { get; }
    }
}