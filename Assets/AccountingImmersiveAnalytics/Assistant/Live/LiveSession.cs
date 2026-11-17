using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AccountingImmersiveAnalytics.Assistant.Live {

    public sealed class Client {
        public LiveClient Live { get; }

        public Client(string apiKey) {
            Live = new LiveClient(apiKey);
        }
    }

    public sealed class LiveClient {
        private const string Endpoint =
            "wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent";
        private static readonly TimeSpan SetupTimeout = TimeSpan.FromSeconds(20);

        private readonly string apiKey;

        public LiveClient(string apiKey) {
            this.apiKey = apiKey;
        }

        public async Task<AsyncSession> ConnectAsync(string model, LiveConnectConfig config) {
            var socket = new ClientWebSocket();
            socket.Options.SetRequestHeader("x-goog-api-key", apiKey);
            var session = new AsyncSession(socket);

            using (var timeout = new CancellationTokenSource(SetupTimeout)) {
                try {
                    await socket.ConnectAsync(new Uri(Endpoint), timeout.Token).ConfigureAwait(false);
                    await session.SendAsync(LiveWire.Setup(model, config)).ConfigureAwait(false);
                    await session.ReadSetupCompleteAsync(timeout.Token).ConfigureAwait(false);
                    return session;
                }
                catch {
                    await session.CloseAsync().ConfigureAwait(false);
                    throw;
                }
            }
        }
    }

    public sealed class AsyncSession {
        private const int ReceiveBufferBytes = 64 * 1024;
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(1);

        private readonly ClientWebSocket socket;
        private readonly SemaphoreSlim sendGate = new SemaphoreSlim(1, 1);
        private readonly byte[] receiveBuffer = new byte[ReceiveBufferBytes];
        private readonly MemoryStream receiveMessage = new MemoryStream(ReceiveBufferBytes);

        private LiveServerMessage pendingSetupComplete;
        private int closed;

        internal AsyncSession(ClientWebSocket socket) {
            this.socket = socket;
        }

        internal async Task ReadSetupCompleteAsync(CancellationToken token) {
            LiveServerMessage message = await ReadAsync(token).ConfigureAwait(false);
            if (message?.SetupComplete == null)
                throw new InvalidOperationException("The server closed the connection before finishing setup.");
            pendingSetupComplete = message;
        }

        public Task<LiveServerMessage> ReceiveAsync() {
            LiveServerMessage pending = pendingSetupComplete;
            if (pending != null) {
                pendingSetupComplete = null;
                return Task.FromResult(pending);
            }
            return ReadAsync(CancellationToken.None);
        }

        public Task SendRealtimeInputAsync(LiveSendRealtimeInputParameters input) => SendAsync(LiveWire.RealtimeAudio(input));

        public Task SendClientContentAsync(LiveSendClientContentParameters content) => SendAsync(LiveWire.ClientContent(content));

        public Task SendToolResponseAsync(LiveSendToolResponseParameters response) => SendAsync(LiveWire.ToolResponse(response));

        internal async Task SendAsync(string json) {
            if (Volatile.Read(ref closed) != 0) throw new InvalidOperationException("The session is closed.");

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await sendGate.WaitAsync().ConfigureAwait(false);
            try {
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            finally {
                sendGate.Release();
            }
        }

        public async Task CloseAsync() {
            if (Interlocked.Exchange(ref closed, 1) != 0) return;

            try {
                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived) {
                    using (var timeout = new CancellationTokenSource(CloseTimeout))
                        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", timeout.Token).ConfigureAwait(false);
                }
            }
            catch (Exception) {
            }

            socket.Abort();
            socket.Dispose();
        }

        private async Task<LiveServerMessage> ReadAsync(CancellationToken token) {
            receiveMessage.SetLength(0);
            while (true) {
                WebSocketReceiveResult result;
                try {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), token).ConfigureAwait(false);
                }
                catch (Exception) when (Volatile.Read(ref closed) != 0) {
                    return null;
                }

                if (result.MessageType == WebSocketMessageType.Close) {
                    WebSocketCloseStatus? status = socket.CloseStatus;
                    if (status == null || status == WebSocketCloseStatus.NormalClosure) return null;
                    throw new InvalidOperationException(
                        $"Server closed the WebSocket connection: {socket.CloseStatusDescription} ({(int)status.Value})");
                }

                receiveMessage.Write(receiveBuffer, 0, result.Count);
                if (!result.EndOfMessage) continue;

                string json = Encoding.UTF8.GetString(receiveMessage.GetBuffer(), 0, (int)receiveMessage.Length);
                JObject root = LiveWire.Parse(json);
                return LiveWire.Message(root);
            }
        }
    }
}
