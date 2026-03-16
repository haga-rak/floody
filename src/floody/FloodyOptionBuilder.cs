using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net;
using floody.common;

namespace floody;

public static class FloodyOptionBuilder
{
    public static IEnumerable<Symbol> EnumerateCommandLineSymbols()
    {
        yield return new Argument<Uri>("uri", parse: ParseUri) { Arity = ArgumentArity.ExactlyOne };

        yield return CreateOption(new[] { "--method", "-X" }, ParseMethod, ArgumentArity.ZeroOrOne,
            "Method to used", "GET");

        yield return CreateOption(new[] { "--concurrent-connection", "-c" }, ParseConcurrentConnection,
                        ArgumentArity.ZeroOrOne, "Concurrent connection count to the remote", 16);

        yield return CreateOption(new[] { "--proxy", "-x" }, ParseWebProxy, ArgumentArity.ZeroOrOne,
            "Address of proxy (SOCKS5 by default, use --http-connect for HTTP CONNECT)");

        yield return new Option<bool>("--http-connect", "Use HTTP CONNECT instead of SOCKS5 when proxying");

        yield return CreateOption(new[] { "--request-body-length", "-r" }, ParseLength, ArgumentArity.ZeroOrOne,
            "Request body length", 0L);

        yield return CreateOption(new[] { "--response-body-length", "-l" }, ParseLength, ArgumentArity.ZeroOrOne,
            "Response body length (sends `length` as query string, works only when used with floodys)", 0L);

        yield return CreateOption(new[] { "--header", "-H" }, ParseHeaders, ArgumentArity.ZeroOrMore,
            "Additional HTTP headers", Array.Empty<Header>());

        yield return CreateOption(new[] { "--output-file", "-o" }, ParseOutputFile, ArgumentArity.ZeroOrOne,
            "Output benchmark result into a json file", (FileInfo?)null);

        yield return CreateOption(new[] { "--duration", "-d" }, ParseDuration, ArgumentArity.ZeroOrOne,
            "Test duration (unit accepted: ms, s, mn, h)", TimeSpan.FromSeconds(30));

        yield return CreateOption(new[] { "--warm-up", "-w" }, ParseDuration, ArgumentArity.ZeroOrOne,
            "Warm up duration (unit accepted: ms, s, mn, h)", TimeSpan.FromSeconds(5));
    }

    private static Option<T> CreateOption<T>(string[] aliases, ParseArgument<T> parseArgument,
        ArgumentArity argumentArity, string description, T? defaultValue = default(T))
    {
        var option = new Option<T>(aliases, parseArgument: parseArgument)
        {
            Arity = argumentArity,
            Description = description
        };

        if (!Equals(defaultValue, default(T)))
        {
            option.SetDefaultValue(defaultValue);
        }

        return option;
    }

    private static FileInfo? ParseOutputFile(ArgumentResult result)
    {
        var rawValue = result.Tokens.First().Value;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        return new FileInfo(rawValue);
    }


    public static HttpSettings CreateHttpSettings(
        InvocationContext invocationContext,
        IReadOnlyCollection<Symbol> symbols)
    {
        var uri = invocationContext.ParseResult
            .GetValueForArgument(
                symbols.OfType<Argument<Uri>>().First(a => a.Name == "uri"));

        var method = invocationContext.ParseResult
            .GetValueForOption(
                symbols.OfType<Option<string>>().First(a => a.Name == "method"))!;

        var concurrentConnection = invocationContext.ParseResult
            .GetValueForOption(
                symbols.OfType<Option<int>>().First(a => a.Name == "concurrent-connection"));

        var webProxy = invocationContext.ParseResult
            .GetValueForOption(
                symbols.OfType<Option<WebProxy?>>().First(a => a.Name == "proxy"));

        var headers = invocationContext.ParseResult
            .GetValueForOption(
                symbols.OfType<Option<IReadOnlyCollection<Header>>>().First(a => a.Name == "header"))!;

        var requestBodyLength = invocationContext.ParseResult
            .GetValueForOption(
                symbols.OfType<Option<long>>().First(a => a.Name == "request-body-length"));

        var responseBodyLength = invocationContext.ParseResult
            .GetValueForOption(
                symbols.OfType<Option<long>>().First(a => a.Name == "response-body-length"));

        var httpConnect = invocationContext.ParseResult
            .GetValueForOption(
                symbols.OfType<Option<bool>>().First(a => a.Name == "http-connect"));

        return new HttpSettings(uri.ToString(),
            method, concurrentConnection,
            webProxy?.Address?.ToString(), headers, requestBodyLength, responseBodyLength, httpConnect);
    }

    public static StartupSettings CreateStartupSettings(InvocationContext invocationContext,
        IReadOnlyCollection<Symbol> symbols)
    {
        var duration = invocationContext.ParseResult
            .GetValueForOption(
                symbols.OfType<Option<TimeSpan>>().First(a => a.Name == "duration"));

        var warmup = invocationContext.ParseResult
            .GetValueForOption(
                symbols.OfType<Option<TimeSpan>>().First(a => a.Name == "warm-up"));

        var outputFile = invocationContext.ParseResult
            .GetValueForOption(
                symbols.OfType<Option<FileInfo>>().First(a => a.Name == "output-file"));

        return new StartupSettings(duration, warmup, outputFile?.FullName);
    }


    public static long ParseLength(ArgumentResult result)
    {
        var t = result.Tokens.Select(token => token.Value).First();

        if (!long.TryParse(t, out var value))
        {
            throw new ArgumentException("Invalid length format");
        }

        if (value < 0)
        {
            throw new ArgumentException("Length must be greater than 0");
        }

        return value;
    }

    public static Uri ParseUri(ArgumentResult result)
    {
        var t = result.Tokens.Select(token => token.Value).First();

        if (!Uri.TryCreate(t, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid uri format");
        }

        if (!uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only HTTP(s) is supported");
        }

        return uri;
    }

    public static string ParseMethod(ArgumentResult result)
    {
        return result.Tokens.Select(token => token.Value).First();
    }

    private static TimeSpan ParseDuration(ArgumentResult result)
    {
        var stringValue = result.Tokens.First().Value;
        stringValue = stringValue.Replace(" ", string.Empty);

        if (int.TryParse(stringValue, out var valueSeconds))
        {
            return TimeSpan.FromSeconds(valueSeconds);
        }

        // Now consider unit ms, s, mn, h

        if (stringValue.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(stringValue[..^2], out var valueMs))
            {
                return TimeSpan.FromMilliseconds(valueMs);
            }

            throw new ArgumentException("Invalid duration format");
        }

        if (stringValue.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(stringValue[..^1], out valueSeconds))
            {
                return TimeSpan.FromSeconds(valueSeconds);
            }

            throw new ArgumentException("Invalid duration format");
        }

        if (stringValue.EndsWith("mn", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(stringValue[..^2], out var valueMinutes))
            {
                return TimeSpan.FromMinutes(valueMinutes);
            }

            throw new ArgumentException("Invalid duration format");
        }

        if (stringValue.EndsWith("h", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(stringValue[..^1], out var valueHours))
            {
                return TimeSpan.FromHours(valueHours);
            }

            throw new ArgumentException("Invalid duration format");
        }

        throw new ArgumentException("Invalid duration format");
    }

    public static int ParseConcurrentConnection(ArgumentResult result)
    {
        var rawValue = result.Tokens.First().Value;

        if (!int.TryParse(rawValue, out var value))
        {
            throw new ArgumentException("Invalid concurrent connection format: must be integer");
        }

        if (value <= 0)
        {
            throw new ArgumentException("Concurrent connection must be greater than 0");
        }

        return value;
    }

    public static IReadOnlyCollection<Header> ParseHeaders(ArgumentResult result)
    {
        var rawHeaders = result.Tokens.Select(s => s.Value).ToList();

        var headers = new List<Header>();

        foreach (var rawHeader in rawHeaders)
        {
            var tab = rawHeader.Split(':', 2);
            if (tab.Length < 2)
            {
                throw new ArgumentException("Invalid header format. Format should be name:value");
            }

            headers.Add(new Header(tab[0].Trim(), tab[1].TrimStart()));
        }

        return headers;
    }

    public static WebProxy ParseWebProxy(ArgumentResult result)
    {
        var rawProxy = result.Tokens.First().Value;

        if (Uri.TryCreate(rawProxy, UriKind.Absolute, out var uri))
        {
            return new WebProxy(uri);
        }

        // Slip last to accept IPv6 ADDRESS 

        var tab = rawProxy.Split(':');

        if (tab.Length < 2)
        {
            throw new ArgumentException("Invalid proxy format. Format should be host:port");
        }

        var portString = tab[^1];

        if (!int.TryParse(portString, out var port) || port < 1 || port > 65535)
        {
            throw new ArgumentException("Invalid port format or port out of range (1-65535)");
        }

        var host = string.Join(":", tab[..^1]);

        return new WebProxy(host, port);
    }
}