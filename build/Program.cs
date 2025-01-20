// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text.Json;
using build.Benchs;

namespace build;

using SimpleExec;
using static Bullseye.Targets;
using static SimpleExec.Command;

public class Program
{
    private static string _outputClient = null!;
    private static string _outputServer = null!;
    private static string _serverExecutable = null!;
    private static string _clientExecutable = null!;

    public static async Task Main(string[] args)
    {
        args = ExtractFloodysArgs(args,out var floodyArgs);
        args = ExtractBenchArgs(args,out var proxyUris);

        var workRootPath = $"_work/out";

        _outputServer = Path.Combine(workRootPath, "floodys");
        _outputClient = Path.Combine(workRootPath, "floody");

        _serverExecutable = Path.Combine(_outputServer, "floodys");
        _clientExecutable = Path.Combine(_outputClient, "floody");

        int httpPort = -1; 
        int httpsPort = -1;

        using var exitTokenSource = new CancellationTokenSource();
        var exitToken = exitTokenSource.Token;
        
        var disableAot = string.Equals(Environment.GetEnvironmentVariable("DISABLE_AOT"), "1",
            StringComparison.OrdinalIgnoreCase);

        var disableAotString = disableAot ? "-p:PublishAot=false" : string.Empty;

        Target("build-server", () =>
            RunAsync("dotnet", $"build --configuration Release {disableAotString} /nologo --verbosity quiet src/floody.server", cancellationToken: exitToken, noEcho: true,
                configureEnvironment: ConfigureDotnetEnvironment));

        Target("publish-server", DependsOn("build-server"),
            () => RunAsync("dotnet", $"publish --configuration Release {disableAotString} /nologo --verbosity quiet src/floody.server -o {_outputServer}",
                configureEnvironment: ConfigureDotnetEnvironment, cancellationToken: exitToken));

        Target("build-client", () =>
            RunAsync("dotnet", $"build --configuration Release {disableAotString} /nologo --verbosity quiet src/floody",
                configureEnvironment: ConfigureDotnetEnvironment, cancellationToken: exitToken, noEcho: true));

        Target("publish-client", DependsOn("build-client"),
            () => RunAsync("dotnet", $"publish --configuration Release {disableAotString} /nologo --verbosity quiet src/floody -o {_outputClient}",
                configureEnvironment: ConfigureDotnetEnvironment, cancellationToken: exitToken));

        Target("publish", DependsOn("publish-client", "publish-server", "build-client"));
        
        Target("start-server", DependsOn("publish-server"),
            async () =>
            {
                var processId = Process.GetCurrentProcess().Id;
                
               (httpPort, httpsPort) = await CommandExtension
                    .ReadBuffered(_serverExecutable, $"--pid={processId}", _outputServer,
                        exitToken)
                    .WaitForPortNumbers();
            });
        
        Target("test-http", DependsOn("publish-client", "start-server"),
            async () =>
            {
                await RunTest(floodyArgs, false, httpPort, exitToken);
            });

        Target("test-https", DependsOn("publish-client", "start-server"),
            async () =>
            {
                await RunTest(floodyArgs, true, httpsPort, exitToken);
            });
        
        Target("bench", DependsOn("publish-client", "publish-server", "start-server"),
            async () =>
            {
                var benchmarkAgenda = BenchmarkAgenda.DefaultAgenda;

                if (proxyUris.Any())
                {
                    benchmarkAgenda = new BenchmarkAgenda(new string ? [] { null}.Concat(proxyUris).ToList(),
                        benchmarkAgenda.ResponseBodySize, benchmarkAgenda.Schemes);
                }
                
                var configs = benchmarkAgenda.GenerateBenchmarkConfigs();

                var benchmarkResults = new List<BenchmarkResult>();

                var flatDate = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                var outPath = Path.Combine("_results", flatDate);

                foreach (var config in configs)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Running {config.ToFileName()}");
                    Console.WriteLine("******************************");
                    Console.WriteLine();
                    
                    var currentArgs = config.ToFloodyArgs(outPath, out var fileName);
                    var port = config.IsHttps ? httpsPort : httpPort;
                    await RunTest(currentArgs, config.IsHttps,port, exitToken);

                    benchmarkResults.Add(BenchmarkResult.CreateFrom(config, fileName));
                }

                var markDownPath = Path.Combine(outPath, "results.md");

                ResultBuilder.BuildMarkdownResults(benchmarkResults, markDownPath);
            });

        await RunTargetsAndExitAsync(args, ex => ex is ExitCodeException);
    }

    private static void ConfigureDotnetEnvironment(IDictionary<string, string?> c)
    {
        c["DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE"] = "true";
    }

    private static async Task RunTest(string? floodyArgs, bool isHttps, int port, CancellationToken exitToken)
    {
        var scheme = isHttps ? "https" : "http";
        var finalUrl = $"{scheme}://127.0.0.1:{port}";
        var clientArgs = $"{finalUrl} {floodyArgs}";
        await RunAsync(_clientExecutable, clientArgs, cancellationToken: exitToken, noEcho: false);
    }

    private static string[] ExtractFloodysArgs(string[] originalArgs, out string? floodyArgs)
    {
        var originalList = originalArgs.ToList();

        floodyArgs = null;

        foreach (var item in originalList.ToList())
        {
            if (item.StartsWith("floody:"))
            {
                originalList.Remove(item);
                var finalValue = item.Split(":", 2)[1];
                floodyArgs = finalValue;
            }
        }

        return originalList.ToArray();
    }
    
    private static string[] ExtractBenchArgs(string[] originalArgs, out List<string> proxyUris)
    {
        var originalList = originalArgs.ToList();

        proxyUris = new();

        foreach (var item in originalList.ToList())
        {
            if (item.StartsWith("compare:"))
            {
                originalList.Remove(item);
                var finalValue = item.Split(":", 2)[1].Trim();

                foreach (var uri in finalValue.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(uri, out var port))
                    {
                        // Assume local port number 
                        proxyUris.Add($"127.0.0.1:{port}");
                        continue;
                    }
                    
                    proxyUris.Add(uri);
                }


            }
        }

        return originalList.ToArray();
    }
}