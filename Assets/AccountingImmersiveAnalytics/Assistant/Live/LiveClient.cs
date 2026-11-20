using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace AccountingImmersiveAnalytics.Assistant.Live
{
    public sealed class LiveClient
    {
        private const string Endpoint =
            "wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent";
        private static readonly TimeSpan SetupTimeout = TimeSpan.FromSeconds(20);

        private readonly string _apiKey;

        public LiveClient(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<LiveSession> ConnectAsync(string model, LiveConnectConfig config)
        {
            var socket = new ClientWebSocket();
            socket.Options.SetRequestHeader("x-goog-api-key", _apiKey);
            var session = new LiveSession(socket);

            using (var timeout = new CancellationTokenSource(SetupTimeout))
            {
                try
                {
                    await socket.ConnectAsync(new Uri(Endpoint), timeout.Token).ConfigureAwait(false);
                    await session.SendAsync(LiveWire.Setup(model, config)).ConfigureAwait(false);
                    await session.ReadSetupCompleteAsync(timeout.Token).ConfigureAwait(false);
                    return session;
                }
                catch
                {
                    await session.CloseAsync().ConfigureAwait(false);
                    throw;
                }
            }
        }
    }
}
