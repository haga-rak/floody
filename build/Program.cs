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

        using var exitTokenSource = new CancellationTokenSource();
        var exitToken = exitTokenSource.Token;
        
        var disableAot = string.Equals(Environment.GetEnvironmentVariable("DISABLE_AOT"), "1",
            StringComparison.OrdinalIgnoreCase);

        var disableAotString = disableAot ? "-p:PublishAot=false" : string.Empty;

        Target("build-server", () =>
            RunAsync("dotnet", $"build --configuration Release {disableAotString} --verbosity quiet src/floody.server", cancellationToken: exitToken, noEcho: true));

        Target("publish-server", DependsOn("build-server"),
            () => RunAsync("dotnet", $"publish --configuration Release {disableAotString} --verbosity quiet src/floody.server -o {_outputServer}", cancellationToken: exitToken));

        Target("build-client", () =>
            RunAsync("dotnet", $"build --configuration Release {disableAotString} --verbosity quiet src/floody", cancellationToken: exitToken, noEcho: true));

        Target("publish-client", DependsOn("build-client"),
            () => RunAsync("dotnet", $"publish --configuration Release {disableAotString} --verbosity quiet src/floody -o {_outputClient}", cancellationToken: exitToken));

        Target("publish", DependsOn("publish-client", "publish-server", "build-client"));

        Target("test-http", DependsOn("publish-client", "publish-server"),
            async () =>
            {
                await RunTest(floodyArgs, false, exitToken);
            });

        Target("test-https", DependsOn("publish-client", "publish-server"),
            async () =>
            {
                await RunTest(floodyArgs, true, exitToken);
            });
        
        Target("bench", DependsOn("publish-client", "publish-server"),
            async () =>
            {
                var benchmarkAgenda = BenchmarkAgenda.DefaultAgenda;

                if (proxyUris.Any())
                {
                    benchmarkAgenda = new BenchmarkAgenda(new string ? [] { null}.Concat(proxyUris).ToList(),
                        benchmarkAgenda.ResponseBodySize, benchmarkAgenda.Schemes);
                }
                
                var configs = benchmarkAgenda.GenerateBenchmarkConfigs();

                foreach (var config in configs)
                {
                    Console.WriteLine($"Running {config.ToFileName()}");
                    
                    var outPath = Path.Combine("_results", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                    var floodyArgs = config.ToFloodyArgs(outPath, out var fileName);
                    await RunTest(floodyArgs, config.IsHttps, exitToken); 
                }

                await RunTest(floodyArgs, true, exitToken);
            });

        await RunTargetsAndExitAsync(args, ex => ex is ExitCodeException);
    }

    private static async Task RunTest(string? floodyArgs, bool isHttps, CancellationToken exitToken)
    {
        var scheme = isHttps ? "https" : "http";
        var processId = Process.GetCurrentProcess().Id;
        
        var serverPortNumber = await CommandExtension
            .ReadBuffered(_serverExecutable, $"--pid={processId}", _outputServer,
                exitToken)
            .WaitForPortNumber(scheme);
                
        var finalUrl = $"{scheme}://127.0.0.1:{serverPortNumber}";
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
            if (item.StartsWith("proxys:"))
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