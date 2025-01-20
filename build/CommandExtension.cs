using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace build;

public static class CommandExtension
{
    public static async Task<(int HttpPort, int HttpsPort)> WaitForPortNumbers(this IAsyncEnumerable<string> input)
    {
        // will fail when Ms will change kestrel's welcome message
            
        var regexHttp = new Regex($@"http://.*:(?<port>\d+)$");
        var regexHttps = new Regex($@"https://.*:(?<port>\d+)$");

        var httpPort = -1; 
        var httpsPort = -1; 

        await foreach (var line in input)
        {
#if DEBUG
            Console.WriteLine(line);
#endif

            var matchHttps = regexHttps.Match(line);
            if (matchHttps.Success)
            {
                httpsPort = int.Parse(matchHttps.Groups["port"].Value);
            }
            
            var matchHttp = regexHttp.Match(line);
            if (matchHttp.Success)
            {
                httpPort = int.Parse(matchHttp.Groups["port"].Value);
            }
            
            if (httpPort != -1 && httpsPort != -1)
            {
                return (httpPort, httpsPort);
            }
        }

        throw new InvalidOperationException("Port not found");
    }

    public static async IAsyncEnumerable<string> ReadBuffered(string path,
        string args, string workingDirectory, 
        [EnumeratorCancellation] CancellationToken token)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        var process = Process.Start(startInfo)!;

        while (!token.IsCancellationRequested && await process.StandardOutput.ReadLineAsync(token) is { } str)
        {
            yield return str;
        }

        var fullText = await process.StandardOutput.ReadToEndAsync(token);
            
#if DEBUG
        Console.WriteLine(fullText);
#endif
    }
}