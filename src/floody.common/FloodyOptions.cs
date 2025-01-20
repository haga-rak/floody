namespace floody.common
{
    public class FloodyOptions
    {
        public FloodyOptions(HttpSettings httpSettings, StartupSettings startupSettings)
        {
            HttpSettings = httpSettings;
            StartupSettings = startupSettings;
        }

        public HttpSettings HttpSettings { get; }

        public StartupSettings StartupSettings { get; }
    }
}