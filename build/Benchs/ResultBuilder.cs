using System.Text;
using System.Text.Json;
using floody.common;

namespace build.Benchs
{
    public class ResultBuilder
    {
        private static string CreateMarkdownLine(IEnumerable<string> values)
        {
            return $"| {string.Join(" | ", values)} |";
        }

        private static string CreateMarkdownLine(params string[] values)
        {
            return CreateMarkdownLine((IEnumerable<string>)values);
        }

        public static IEnumerable<string> BuildMarkdownResults(List<BenchmarkResult> results)
        {
            var groupByTestCases =
                results.GroupBy(g => g.Configuration.GetGroupingKey);

            foreach (var group in groupByTestCases)
            {
                var builder = new StringBuilder();

                // build header 
                builder.AppendLine(
                    CreateMarkdownLine(
                        group.Key,
                        "Total",
                        "Success",
                        "Fail",
                        "req/s",
                        "Bandwidth"
                    ));

                builder.AppendLine(
                    CreateMarkdownLine(
                        "---",
                        "---",
                        "---",
                        "---",
                        "---",
                        "---"
                    ));
                
                foreach (var item in group.OrderBy(g => g.Configuration.ProxyUri?.ToString()))
                {
                    builder.AppendLine(
                        CreateMarkdownLine(
                            item.Configuration.ProxyUri ?? "No proxy",
                            item.Result.Count.ToString(),
                            item.Result.SuccessCount.ToString(),
                            item.Result.HttpFailCount.ToString(),
                            item.Result.RequestPerSeconds.ToString("F"),
                            item.Result.TotalReceivedPerSeconds
                        ));
                }

                yield return builder.ToString();
            }
        }

        public static void BuildMarkdownResults(List<BenchmarkResult> results, Stream outStream)
        {
            using var writer = new StreamWriter(outStream, new UTF8Encoding(false));

            foreach (var markdownResult in BuildMarkdownResults(results))
            {
                writer.WriteLine(markdownResult);
                writer.WriteLine();
            }
        }
        public static void BuildMarkdownResults(List<BenchmarkResult> results, string fileName)
        {
            using var stream = File.Create(fileName);
            BuildMarkdownResults(results, stream);
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
            using var fileStream = File.OpenRead(floodResultFile);
            var floodResult = JsonSerializer.Deserialize<FloodResult>(fileStream, 
                new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            
            return new BenchmarkResult(floodResult, config);
        }
    }
}