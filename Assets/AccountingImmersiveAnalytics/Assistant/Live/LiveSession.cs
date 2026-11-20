using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace AccountingImmersiveAnalytics.Assistant.Live
{
    public sealed class LiveSession
    {
        private const int ReceiveBufferBytes = 64 * 1024;
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(1);

        private readonly ClientWebSocket _socket;
        private readonly SemaphoreSlim _sendGate = new SemaphoreSlim(1, 1);
        private readonly byte[] _receiveBuffer = new byte[ReceiveBufferBytes];
        private readonly MemoryStream _receiveMessage = new MemoryStream(ReceiveBufferBytes);

        private LiveServerMessage _pendingSetupComplete;
        private int _closed;

        internal LiveSession(ClientWebSocket socket)
        {
            _socket = socket;
        }

        internal async Task ReadSetupCompleteAsync(CancellationToken token)
        {
            LiveServerMessage message = await ReadAsync(token).ConfigureAwait(false);
            if (message?.SetupComplete == null)
                throw new InvalidOperationException("The server closed the connection before finishing setup.");
            _pendingSetupComplete = message;
        }

        public Task<LiveServerMessage> ReceiveAsync()
        {
            LiveServerMessage pending = _pendingSetupComplete;
            if (pending != null)
            {
                _pendingSetupComplete = null;
                return Task.FromResult(pending);
            }
            return ReadAsync(CancellationToken.None);
        }

        public Task SendRealtimeInputAsync(LiveSendRealtimeInputParameters input) => SendAsync(LiveWire.RealtimeAudio(input));

        public Task SendClientContentAsync(LiveSendClientContentParameters content) => SendAsync(LiveWire.ClientContent(content));

        public Task SendToolResponseAsync(LiveSendToolResponseParameters response) => SendAsync(LiveWire.ToolResponse(response));

        internal async Task SendAsync(string json)
        {
            if (Volatile.Read(ref _closed) != 0) throw new InvalidOperationException("The session is closed.");

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await _sendGate.WaitAsync().ConfigureAwait(false);
            try
            {
                await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            finally
            {
                _sendGate.Release();
            }
        }

        public async Task CloseAsync()
        {
            if (Interlocked.Exchange(ref _closed, 1) != 0) return;

            try
            {
                if (_socket.State == WebSocketState.Open || _socket.State == WebSocketState.CloseReceived)
                {
                    using (var timeout = new CancellationTokenSource(CloseTimeout))
                        await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", timeout.Token).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
            }

            _socket.Abort();
            _socket.Dispose();
        }

        private async Task<LiveServerMessage> ReadAsync(CancellationToken token)
        {
            _receiveMessage.SetLength(0);
            while (true)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(_receiveBuffer), token).ConfigureAwait(false);
                }
                catch (Exception) when (Volatile.Read(ref _closed) != 0)
                {
                    return null;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    WebSocketCloseStatus? status = _socket.CloseStatus;
                    if (status == null || status == WebSocketCloseStatus.NormalClosure) return null;
                    throw new InvalidOperationException(
                        $"Server closed the WebSocket connection: {_socket.CloseStatusDescription} ({(int)status.Value})");
                }

                _receiveMessage.Write(_receiveBuffer, 0, result.Count);
                if (!result.EndOfMessage) continue;

                string json = Encoding.UTF8.GetString(_receiveMessage.GetBuffer(), 0, (int)_receiveMessage.Length);
                JObject root = LiveWire.Parse(json);
                return LiveWire.Message(root);
            }
        }
    }
}
