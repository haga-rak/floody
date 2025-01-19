// See https://aka.ms/new-console-template for more information

namespace build;

using SimpleExec;
using static Bullseye.Targets;
using static SimpleExec.Command;

public class Program
{
    public static async Task Main(string[] args)
    {
        args = FixArguments(args,out var floodyArgs);

        var workRootPath = $"_work/{DateTime.Now.ToString("yyyyMMddYYmmss")}";

        var outputServer = Path.Combine(workRootPath, "floodys");
        var outputClient = Path.Combine(workRootPath, "floody");

        var serverExecutable = Path.Combine(outputServer, "floodys");
        var clientExecutable = Path.Combine(outputClient, "floody");

        using var exitTokenSource = new CancellationTokenSource();
        var exitToken = exitTokenSource.Token;
        
        var disableAot = string.Equals(Environment.GetEnvironmentVariable("DISABLE_AOT"), "1",
            StringComparison.OrdinalIgnoreCase);

        var disableAotString = disableAot ? "-p:PublishAot=false" : string.Empty;

        var serverPortNumber = -1;

        Target("build-server", () =>
            RunAsync("dotnet", $"build --configuration Release {disableAotString} --verbosity quiet src/floody.server", cancellationToken: exitToken));

        Target("publish-server", DependsOn("build-server"),
            () => RunAsync("dotnet", $"publish --configuration Release {disableAotString} --verbosity quiet src/floody.server -o {outputServer}", cancellationToken: exitToken));

        Target("build-client", () =>
            RunAsync("dotnet", $"build --configuration Release {disableAotString} --verbosity quiet src/floody", cancellationToken: exitToken));

        Target("publish-client", DependsOn("build-client"),
            () => RunAsync("dotnet", $"publish --configuration Release {disableAotString} --verbosity quiet src/floody -o {outputClient}", cancellationToken: exitToken));

        Target("publish", DependsOn("publish-client", "publish-server", "build-client"));

        Target("start-server-http", DependsOn("publish-server"), 
            async () =>
            {
                serverPortNumber = await CommandExtension
                    .ReadBuffered(serverExecutable, $"", workingDirectory: outputServer,
                        exitToken)
                    .WaitForPortNumber("http");
            });

        Target("start-server-https", DependsOn("publish-server"), 
            async () =>
            {
                serverPortNumber = await CommandExtension
                    .ReadBuffered(serverExecutable, $"", workingDirectory: outputServer,
                        exitToken)
                    .WaitForPortNumber("https");
            });

        Target("benchmark-http", DependsOn("publish-client", "start-server-http"),
            async () =>
            {
                var finalUrl = $"http://127.0.0.1:{serverPortNumber}";
                var clientArgs = $"{finalUrl} {floodyArgs}";
                await RunAsync(clientExecutable, clientArgs, cancellationToken: exitToken, noEcho: false);
            });

        Target("benchmark-https", DependsOn("publish-client", "start-server-https"),
            async () =>
            {
                var finalUrl = $"https://127.0.0.1:{serverPortNumber}";
                var clientArgs = $"{finalUrl} {floodyArgs}";
                await RunAsync(clientExecutable, clientArgs, cancellationToken: exitToken, noEcho: false);
            });

        await RunTargetsAndExitAsync(args, ex => ex is ExitCodeException);
    }

    private static string[] FixArguments(string[] originalArgs, out string? floodyArgs)
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

}