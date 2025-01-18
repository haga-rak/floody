using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace build
{
    public static class CommandExtension
    {
        public static async Task<int> WaitForPortNumber(this IAsyncEnumerable<string> input)
        {
            var regex = new Regex(@"://.*:(?<port>\d+)$");

            await foreach (var line in input)
            {
                var match = regex.Match(line);

                if (match.Success)
                {
                    return int.Parse(match.Groups["port"].Value);
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

            await process.StandardOutput.ReadToEndAsync(token);
        }
    }
}