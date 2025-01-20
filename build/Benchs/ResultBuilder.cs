using System.Text.Json;
using floody.common;

namespace build.Benchs
{
    public class ResultBuilder
    {
        public static IEnumerable<string> BuildMarkdownResults(List<BenchmarkResult> results)
        {
            var groupByTestCase =
                results.GroupBy(g => g.Configuration.GetGroupingKey);
            
            
        }
    }


    public class BenchmarkResult
    {
        public BenchmarkResult(FloodResult result, BenchmarkConfig configuration)
        {
            Result = result;
            Configuration = configuration;
        }

        public FloodResult Result { get; } 
        
        public BenchmarkConfig Configuration { get; }

        public static BenchmarkResult CreateFrom(BenchmarkConfig config, string floodResultFile)
        {
            var floodResult = JsonSerializer.Deserialize<FloodResult>(floodResultFile, 
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            
            return new BenchmarkResult(floodResult, config);
        }
    }
}