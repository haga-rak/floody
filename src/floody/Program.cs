using System.CommandLine;

namespace floody
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var symbols = FloodyOptionBuilder.BuildSymbols().ToList();

            var rootCommand = new RootCommand("A simple http load test tool supporting proxy");

            rootCommand.AddRange(symbols);

            rootCommand.SetHandler(async (context) =>
            {
                var httpSettings = FloodyOptionBuilder.CreateHttpSettings(context, symbols);
                var startupSettings = FloodyOptionBuilder.CreateStartupSettings(context, symbols);
                var options = new FloodyOptions(httpSettings, startupSettings);

                var floody = new FloodExecutor(options);

                var result = await floody.ExecuteAsync();

                // Pretty print the result with Console.WriteLine

                var prettyMessage = result.PrettyFormat(options);

                Console.WriteLine(prettyMessage);
            });

            await rootCommand.InvokeAsync(args);
        }

    }
}
