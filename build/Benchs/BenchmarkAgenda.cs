namespace build.Benchs
{
    public class BenchmarkAgenda
    {
        public static BenchmarkAgenda DefaultAgenda => new BenchmarkAgenda(
            new List<string?> { null, "127.0.0.1:44344", "127.0.0.1:8080" },
            new List<int> { 0, 8192 },
            new List<bool> { true, false }
        );
        
        public BenchmarkAgenda(List<string?> proxyEndPoints, List<int> responseBodySize, List<bool> schemes)
        {
            ProxyEndPoints = proxyEndPoints;
            ResponseBodySize = responseBodySize;
            Schemes = schemes;
        }

        public List<string?> ProxyEndPoints { get; }
        
        public List<int> ResponseBodySize { get; }
        
        public List<bool> Schemes { get; }
        
        public int DurationSeconds { get; set; } = 2;

        public int WarmUpDuration { get; set; } = 2;
        
        public List<BenchmarkConfig> GenerateBenchmarkConfigs()
        {
            var configs = new List<BenchmarkConfig>();
            
            foreach (var proxyEndPoint in ProxyEndPoints)
            {
                foreach (var responseBody in ResponseBodySize)
                {
                    foreach (var scheme in Schemes)
                    {
                        configs.Add(new BenchmarkConfig(proxyEndPoint, responseBody, scheme, DurationSeconds));
                    }
                }
            }

            return configs;
        }
    }
}