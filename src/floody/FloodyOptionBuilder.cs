using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Net;

namespace floody;

public static class FloodyOptionBuilder
{
    public static IEnumerable<Symbol> BuildSymbols()
    {
        yield return new Argument<Uri>("uri", parse: ParseUri) { Arity = ArgumentArity.ExactlyOne };

        {
            var option = new Option<string>(["--method", "-X"], parseArgument: ParseMethod) {
                Arity = ArgumentArity.ZeroOrOne,
                Description = "Method to used"
            };

            option.SetDefaultValue("GET");

            yield return option;
        }

        {
            var option = new Option<int>(["--concurrent-connection", "-c"], parseArgument: ParseConcurrentConnection) {
                Arity = ArgumentArity.ZeroOrOne,
                Description = "Concurrent connection count to the remote"
            };

            option.SetDefaultValue(8);

            yield return option;
        }

        {
            var option = new Option<WebProxy?>(["--proxy", "-x"], parseArgument: ParseWebProxy)
            {
                Arity = ArgumentArity.ZeroOrOne,
                Description = "Address of HTTP proxy"
            };

            yield return option;
        }

        {
            var option = new Option<IReadOnlyCollection<Header>>(["--header", "-H"], parseArgument: ParseHeaders)
            {
                Arity = ArgumentArity.ZeroOrMore,
                Description = "Additional HTTP headers"
            };

            option.SetDefaultValue(Array.Empty<Header>());
            yield return option;
        }

        {
            var option = new Option<TimeSpan>(["--duration", "-d"], parseArgument: ParseDuration)
            {
                Arity = ArgumentArity.ZeroOrOne,
                Description = "Test duration (unit accepted: ms, s, mn, h)"
            };

            option.SetDefaultValue(TimeSpan.FromSeconds(30));

            yield return option;
        }
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


        return new HttpSettings(uri, method, concurrentConnection, webProxy, headers);
    }

    public static StartupSettings CreateStartupSettings(InvocationContext invocationContext,
        IReadOnlyCollection<Symbol> symbols)
    {
        var duration = invocationContext.ParseResult
            .GetValueForOption(
                symbols.OfType<Option<TimeSpan>>().First(a => a.Name == "duration"));

        return new StartupSettings(duration);
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
        return result.GetValueOrDefault<string>();
    }

    public static int ParseConcurrentConnection(ArgumentResult result)
    {
        var value =  result.GetValueOrDefault<int>();

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
        var rawProxy =  result.GetValueOrDefault<string>();

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