using System.CommandLine;
using System.Text.Json;

namespace floody
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var symbols = FloodyOptionBuilder.EnumerateCommandLineSymbols().ToList();

            var rootCommand = new RootCommand("A simple http load test tool supporting proxy");

            rootCommand.AddRange(symbols);

            rootCommand.SetHandler(async (context) =>
            {
                try
                {
                    var httpSettings = FloodyOptionBuilder.CreateHttpSettings(context, symbols);
                    var startupSettings = FloodyOptionBuilder.CreateStartupSettings(context, symbols);
                    var options = new FloodyOptions(httpSettings, startupSettings);

                    var floody = new FloodExecutor(options);

                    var result = await floody.ExecuteAsync();

                    var prettyMessage = result.PrettyFormat();

                    if (startupSettings.OutputFile != null)
                    {
                        var parentDirectory = startupSettings.OutputFile.Directory;

                        if (parentDirectory != null && !parentDirectory.Exists)
                        {
                            parentDirectory.Create();
                        }

                        await File.WriteAllTextAsync(startupSettings.OutputFile.FullName, prettyMessage);
                    }

                    Console.WriteLine(prettyMessage);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });

            return await rootCommand.InvokeAsync(args);
        }

    }
}
