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

        var listenUrl = Environment.GetEnvironmentVariable("FLOODYS_LISTEN_URL") ?? "http://127.0.0.1:0";

        var serverPortNumber = -1;

        Target("build-server", () =>
            RunAsync("dotnet", "build --configuration Release --verbosity quiet src/floody.server", cancellationToken: exitToken));

        Target("publish-server", DependsOn("build-server"),
            () => RunAsync("dotnet", $"publish --configuration Release --verbosity quiet src/floody.server -o {outputServer}", cancellationToken: exitToken));

        Target("build-client", () =>
            RunAsync("dotnet", "build --configuration Release --verbosity quiet src/floody", cancellationToken: exitToken));

        Target("publish-client", DependsOn("build-client"),
            () => RunAsync("dotnet", $"publish --configuration Release --verbosity quiet src/floody -o {outputClient}", cancellationToken: exitToken));

        Target("publish", DependsOn("publish-client", "publish-server", "build-client"));

        Target("start-server", DependsOn("publish-server"), 
            async () =>
            {
                serverPortNumber = await CommandExtension
                    .ReadBuffered(serverExecutable, $"--urls={listenUrl}", workingDirectory: outputServer,
                        exitToken)
                    .WaitForPortNumber();
            });

        Target("benchmark", DependsOn("publish-client", "start-server"),
            async () =>
            {
                string finalUrl;

                if (!Uri.TryCreate(listenUrl, UriKind.Absolute, out var uri))
                {
                    throw new ArgumentException("Invalid uri format for server");
                }

                finalUrl = $"{uri.Scheme}://{uri.Host}:{serverPortNumber}";

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