using System.Net;
using System.Net.Sockets;
using System.Text;
using floody.common;

namespace floody
{
    public class FloodExecutor : IDisposable
    {
        private static readonly int MaxRequestBodyLengthBuffer = 4 * 1024 * 1024;

        private readonly FloodyOptions _options;
        private readonly TimeSpan _timeout;

        private int _count;
        private int _successCount;
        private int _failCount;
        private int _networkFailCount;
        private long _totalReceived;
        private long _totalSent;

        private bool _startMeasure;
        private int _httpVersionLogged;

        private readonly HttpClient _client;

        private readonly SemaphoreSlim _maxHttpClient;

        private readonly byte[] _requestBuffer;

        public FloodExecutor(FloodyOptions options)
        {
            _options = options;
            var socketHandler = new SocketsHttpHandler();

            var proxy = options.HttpSettings.GetWebProxy();

            if (proxy != null)
            {
                if (options.HttpSettings.HttpConnect)
                {
                    socketHandler.Proxy = proxy;
                    socketHandler.UseProxy = true;
                }
                else
                {
                    var proxyUri = proxy.Address!;
                    socketHandler.ConnectCallback = async (context, cancellationToken) =>
                    {
                        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        await socket.ConnectAsync(proxyUri.Host, proxyUri.Port, cancellationToken);

                        var stream = new NetworkStream(socket, ownsSocket: true);
                        await PerformSocks5Handshake(stream, context.DnsEndPoint, cancellationToken);

                        return stream;
                    };
                }
            }

            _maxHttpClient = new SemaphoreSlim(Math.Max(256, options.HttpSettings.ConcurrentConnection) + 4);

            socketHandler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            socketHandler.MaxConnectionsPerServer = options.HttpSettings.ConcurrentConnection;
            socketHandler.PlaintextStreamFilter = (context, _) => new ValueTask<Stream>(new ByteCounterStream(context.PlaintextStream, OnRead, OnWrite));

            _client = new HttpClient(socketHandler);

            _timeout = options.StartupSettings.Duration;

            var handledLength = (int) Math.Min(MaxRequestBodyLengthBuffer, options.HttpSettings.RequestBodyLength);
            _requestBuffer = new byte[handledLength];
        }

        private static async Task PerformSocks5Handshake(NetworkStream stream, DnsEndPoint target, CancellationToken cancellationToken)
        {
            // Greeting: VER=5, NMETHODS=1, METHOD=0 (no auth)
            await stream.WriteAsync(new byte[] { 0x05, 0x01, 0x00 }, cancellationToken);

            var response = new byte[2];
            await stream.ReadExactlyAsync(response, cancellationToken);

            if (response[0] != 0x05 || response[1] != 0x00)
                throw new IOException("SOCKS5 handshake failed: server rejected no-auth method");

            // CONNECT request: VER=5, CMD=1 (connect), RSV=0, ATYP=3 (domain)
            var hostBytes = Encoding.ASCII.GetBytes(target.Host);
            var request = new byte[4 + 1 + hostBytes.Length + 2];
            request[0] = 0x05; // VER
            request[1] = 0x01; // CMD: CONNECT
            request[2] = 0x00; // RSV
            request[3] = 0x03; // ATYP: domain name
            request[4] = (byte)hostBytes.Length;
            hostBytes.CopyTo(request, 5);
            request[^2] = (byte)(target.Port >> 8);
            request[^1] = (byte)(target.Port & 0xFF);

            await stream.WriteAsync(request, cancellationToken);

            // Read reply header (4 bytes minimum)
            var reply = new byte[4];
            await stream.ReadExactlyAsync(reply, cancellationToken);

            if (reply[0] != 0x05 || reply[1] != 0x00)
                throw new IOException($"SOCKS5 CONNECT failed with status: {reply[1]}");

            // Drain the bound address based on ATYP
            var skipBytes = reply[3] switch
            {
                0x01 => 4 + 2, // IPv4 + port
                0x04 => 16 + 2, // IPv6 + port
                0x03 => 1, // domain length byte (then domain + port)
                _ => throw new IOException($"SOCKS5 unexpected ATYP: {reply[3]}")
            };

            var skip = new byte[skipBytes];
            await stream.ReadExactlyAsync(skip, cancellationToken);

            // If domain, still need to read the domain bytes + 2 port bytes
            if (reply[3] == 0x03)
            {
                var domainAndPort = new byte[skip[0] + 2];
                await stream.ReadExactlyAsync(domainAndPort, cancellationToken);
            }
        }

        private void OnWrite(int length)
        {
            if (_startMeasure)
            {
                Interlocked.Add(ref _totalSent, length);
            }
        }

        private void OnRead(int length)
        {
            if (_startMeasure)
            {
                Interlocked.Add(ref _totalReceived, length);
            }
        }

        private HttpRequestMessage CreateRequest(FloodyOptions options)
        {
            var method = new HttpMethod(options.HttpSettings.Method);

            var requestMessage = options.HttpSettings.ResponseBodyLength == 0
                ? new HttpRequestMessage(method, options.HttpSettings.UriString) :
                  new HttpRequestMessage(method,
                    $"{options.HttpSettings.UriString}?&length={options.HttpSettings.ResponseBodyLength}");

            requestMessage.Version = HttpVersion.Version20;
            requestMessage.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

            foreach (var header in options.HttpSettings.AdditionalHeaders)
            {
                requestMessage.Headers.Add(header.Name, header.Value);
            }

            if (options.HttpSettings.RequestBodyLength > 0)
            {
                if (requestMessage.Method == HttpMethod.Get || requestMessage.Method == HttpMethod.Delete)
                {
                    // FORCE POST when request body is not empty
                    requestMessage.Method = HttpMethod.Post;
                }

                requestMessage.Content = new ByteArrayContent(_requestBuffer);
            }

            return requestMessage;
        }

        public async Task<FloodResult> ExecuteAsync()
        {
            Console.WriteLine("Warming up...for {0}s", (int)_options.StartupSettings.WarmupDuration.TotalSeconds);
            await InternalExecute(_options.StartupSettings.WarmupDuration, false);

            // wait for 1s
            await Task.Delay(1000);
            _startMeasure = true;

            Console.WriteLine($"Flooding {_options.HttpSettings.UriString}...for {(int)_timeout.TotalSeconds}s");

            await InternalExecute(_timeout, true);

            return new FloodResult(_count, _successCount,
                _failCount, _networkFailCount,
                _options, _totalReceived, _totalSent);
        }

        private async Task InternalExecute(TimeSpan timeout, bool updateStat)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout);

            var token = cancellationTokenSource.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await _maxHttpClient.WaitAsync(token);
                    _ = InternalQueryAsync(token, updateStat);
                }

                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {

            }
        }

        private async ValueTask InternalQueryAsync(CancellationToken token, bool updateStatistics)
        {
            try
            {
                var requestMessage = CreateRequest(_options);

                using var response = await _client.SendAsync(requestMessage,
                    HttpCompletionOption.ResponseContentRead,
                    token);

                if (Interlocked.Exchange(ref _httpVersionLogged, 1) == 0)
                {
                    Console.WriteLine($"[debug] HTTP version: {response.Version}");
                }

                await using var bodyStream = await response.Content.ReadAsStreamAsync(token);

                _ = await bodyStream.DrainAsync(token);

                if (updateStatistics)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        Interlocked.Increment(ref _successCount);
                    }
                    else
                    {
                        if ((int)response.StatusCode == 528)
                        {
                            Interlocked.Increment(ref _networkFailCount);
                        }
                        else
                        {
                            Interlocked.Increment(ref _failCount);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Natural exit
                return;
            }
            catch
            {
                if (updateStatistics)
                {
                    Interlocked.Increment(ref _networkFailCount);
                }
            }
            finally
            {
                if (updateStatistics)
                {
                    Interlocked.Increment(ref _count);
                }

                _maxHttpClient.Release();
            }
        }

        public void Dispose()
        {
            _client.Dispose();
            _maxHttpClient.Dispose();
        }
    }
}