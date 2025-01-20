using System.Text;
using System.Text.Json;
using floody.common;
using HardwareInformation;

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

            MachineInformation info = MachineInformationGatherer.GatherInformation(true);
            var ramBytes = FormatHelper.FormatBytes(info.RAMSticks.Sum(s => (double) s.Capacity));

            foreach (var group in groupByTestCases)
            {
                var builder = new StringBuilder();

                builder.AppendLine("Test settings: " + group.Key + $" ({info.Cpu.Name} {ramBytes})");
                builder.AppendLine("********");

                // build header 
                builder.AppendLine(
                    CreateMarkdownLine(
                        "   ",
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

                // Print difference 

                var proxyGroups = group.Where(g => g.Configuration.ProxyUri != null).ToList();

                if (proxyGroups.Count == 2)
                {
                    var ordered = proxyGroups.OrderBy(g => g.Configuration.ProxyUri?.ToString()).ToList();

                    builder.AppendLine(
                        CreateMarkdownLine(
                            "Deviation",
                            GetFormattedDifference(ordered[0].Result.Count, ordered[1].Result.Count),
                            GetFormattedDifference(ordered[0].Result.SuccessCount, ordered[1].Result.SuccessCount),
                            GetFormattedDifference(ordered[0].Result.HttpFailCount, ordered[1].Result.HttpFailCount),
                            GetFormattedDifference(ordered[0].Result.RequestPerSeconds, ordered[1].Result.RequestPerSeconds),
                            GetFormattedDifference(ordered[0].Result.TotalReceivedBytes, ordered[1].Result.TotalReceivedBytes)
                        ));
                }

                builder.AppendLine();

                yield return builder.ToString();
            }
        }

        private static string GetFormattedDifference(double nbA, double nbB)
        {
            if (nbA == 0)
            {
                return "/";
            }

            var proportion = (nbB - nbA) / nbA * 100;
            return $"{proportion:F}%";
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