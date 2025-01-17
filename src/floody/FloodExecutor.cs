namespace floody
{
    public class FloodExecutor
    {
        private readonly FloodyOptions _options;
        private readonly TimeSpan _timeout;

        private int _count;
        private int _successCount;
        private int _failCount;
        private int _networkFailCount;
        private long _totalReceived;

        private readonly HttpClient _client;

        private readonly SemaphoreSlim _maxHttpClient;

        public FloodExecutor(FloodyOptions options)
        {
            _options = options;
            var httpClientHandler = new HttpClientHandler();

            if (options.HttpSettings.WebProxy != null)
            {
                httpClientHandler.Proxy = options.HttpSettings.WebProxy;
                httpClientHandler.UseProxy = true;
            }

            _maxHttpClient = new SemaphoreSlim(Math.Max(128, options.HttpSettings.ConcurrentConnection) + 4);

            httpClientHandler.ServerCertificateCustomValidationCallback =
                (_, _, _, _) => true;

            httpClientHandler.MaxConnectionsPerServer = options.HttpSettings.ConcurrentConnection;

            CreateRequest(options);

            _client = new HttpClient(httpClientHandler)
            {

            };

            _timeout = options.StartupSettings.Duration;
        }

        private static HttpRequestMessage CreateRequest(FloodyOptions options)
        {
            var requestMessage = new HttpRequestMessage(new HttpMethod(options.HttpSettings.Method),
                options.HttpSettings.Uri);

            foreach (var header in options.HttpSettings.AdditionalHeaders)
            {
                requestMessage.Headers.Add(header.Name, header.Value);
            }

            return requestMessage;
        }

        public async Task<FloodResult> ExecuteAsync()
        {
            Console.WriteLine("Warming up...for {0}s", (int)_options.StartupSettings.WarmupDuration.TotalSeconds);
            await InternalExecute(_options.StartupSettings.WarmupDuration, false);

            Console.WriteLine($"Flooding {_options.HttpSettings.Uri}...for {(int)_timeout.TotalSeconds}s");
            await InternalExecute(_timeout, true);

            return new FloodResult(_count, _successCount, _failCount, _networkFailCount, _options, _totalReceived);
        }

        private async Task InternalExecute(TimeSpan timeout, bool updateStat)
        {
            using var cts = new CancellationTokenSource(timeout);

            var token = cts.Token;

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

                using var response = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead,
                    token);

                await using var bodyStream = await response.Content.ReadAsStreamAsync(token);

                var totalBodySize = await bodyStream.DrainAsync(token);

                if (updateStatistics)
                {
                    Interlocked.Add(ref _totalReceived, totalBodySize);

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
    }
}