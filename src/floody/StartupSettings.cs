namespace floody
{
    public class StartupSettings
    {
        public StartupSettings(TimeSpan duration, TimeSpan warmupDuration)
        {
            Duration = duration;
            WarmupDuration = warmupDuration;
        }

        public TimeSpan Duration { get; }
        public TimeSpan WarmupDuration { get; }
    }
}