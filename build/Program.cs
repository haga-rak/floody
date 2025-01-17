// See https://aka.ms/new-console-template for more information

namespace build;

using Bullseye.Internal;
using static Bullseye.Targets;
using static SimpleExec.Command;

public class Program
{
    public static async Task Main(string[] args)
    {
        var workRootPath = $"_work/{DateTime.Now.ToString("yyyyMMddYYmmss")}";

        var outputServer = Path.Combine(workRootPath, "floodys");
        var outputClient = Path.Combine(workRootPath, "floody");

        var serverExecutable = Path.Combine(outputServer, "floodys");
        var clientExecutable = Path.Combine(outputClient, "floody");

        Target("build-server", () => 
            RunAsync("dotnet", "build --configuration Release --verbosity quiet src/floody.server"));

        Target("publish-server", DependsOn("build-server"), 
            () => RunAsync("dotnet", $"publish --configuration Release --verbosity quiet src/floody.server -o {outputServer}"));

        Target("build-client", () => 
            RunAsync("dotnet", "build --configuration Release --verbosity quiet src/floody"));

        Target("publish-client", DependsOn("build-client"),
            () => RunAsync("dotnet", $"publish --configuration Release --verbosity quiet src/floody -o {outputClient}"));

        Target("publish", DependsOn("publish-client", "publish-server", "build-client"));

        Target("start-server", DependsOn("publish-server"), () => RunAsync("floodys", $""));

        Target("run", DependsOn("publish", "build-client"));

        await RunTargetsAndExitAsync(args, ex => ex is SimpleExec.ExitCodeException);
    }
}