using System.Collections.Concurrent;
using System.CommandLine;
using System.Net.Http;

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
        private readonly HttpClientHandler _httpClientHandler;
        private readonly HttpClient _client;

        private readonly SemaphoreSlim _maxHttpClient = new(100);

        public FloodExecutor(FloodyOptions options)
        {
            _options = options;
            var httpClientHandler = new HttpClientHandler();
            
            if (options.HttpSettings.WebProxy != null)
            {
                httpClientHandler.Proxy = options.HttpSettings.WebProxy;
                httpClientHandler.UseProxy = true;
            }

            httpClientHandler.MaxConnectionsPerServer = options.HttpSettings.ConcurrentConnection;

            CreateRequest(options);

            _httpClientHandler = httpClientHandler;
            _client = new HttpClient(_httpClientHandler);

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
            using var cts = new CancellationTokenSource(_timeout);

            var token = cts.Token;
            
            while (!token.IsCancellationRequested)
            {
                await _maxHttpClient.WaitAsync(token);
                _ = InternalExecuteAsync(token);
            }

            try
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException)
            {

            }

            return new FloodResult(_count, _successCount, _failCount, _networkFailCount);
        }

        private async ValueTask InternalExecuteAsync(CancellationToken token)
        {
            try
            {
                var requestMessage = CreateRequest(_options);

                var response = await _client.SendAsync(requestMessage, token);

                var bodyStream = await response.Content.ReadAsStreamAsync(token);
                await bodyStream.CopyToAsync(Stream.Null, token);

                if (response.IsSuccessStatusCode)
                {
                    Interlocked.Increment(ref _successCount);
                }
                else
                {
                    Interlocked.Increment(ref _failCount);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch 
            {
                Interlocked.Increment(ref _networkFailCount);
            }
            finally
            {
                Interlocked.Increment(ref _count);
                _maxHttpClient.Release();
            }
        }
    }
}