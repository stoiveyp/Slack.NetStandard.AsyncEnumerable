using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Slack.NetStandard.Socket;

namespace Slack.NetStandard.AsyncEnumerable
{
    public class SocketModeClient:IDisposable
    {
        public ClientWebSocket WebSocket { get; protected set; }
        public SlackWebApiClient WebClient { get; protected set; }

        public SocketModeClient() : this(() => new ClientWebSocket())
        {

        }

        public SocketModeClient(Func<ClientWebSocket> factory)
        {
            _factory = factory;
        }

        public Task ConnectAsync(SlackWebApiClient client, CancellationToken token = default)
        {
            WebClient = client;
            return ConnectAsync(token);
        }

        public Task ConnectAsync(string appToken, CancellationToken token = default)
        {
            WebClient = new SlackWebApiClient(appToken);
            return ConnectAsync(token);
        }

        public virtual async Task ConnectAsync(CancellationToken token = default)
        {
            var connection = await WebClient.Apps.OpenConnection();
            if (!connection.OK)
            {
                throw new InvalidOperationException($"Error from slack: " + connection.Error);
            }

            try
            {
                WebSocket = _factory();
                await WebSocket.ConnectAsync(new Uri(connection.Url, UriKind.Absolute),
                    token);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error connecting to socket mode url: " + connection.Url,ex);
            }
        }

        public virtual async IAsyncEnumerable<Envelope> EnvelopeAsyncEnumerable([EnumeratorCancellation] CancellationToken token)
        {
            while (!token.IsCancellationRequested && WebSocket.State == WebSocketState.Open)
            {
                Disconnect disconnect = null;
                await foreach (var socketModeObject in WebSocket.SocketModeObjects(token))
                {
                    switch (socketModeObject)
                    {
                        case Hello hello:
                            await OnHello(hello, token);
                            break;
                        case Envelope env:
                            yield return env;
                            break;
                        case Disconnect dis:
                            disconnect = dis;
                            break;
                    }

                    if (disconnect != null && !token.IsCancellationRequested)
                    {
                        await OnDisconnect(disconnect, token);
                        disconnect = null;
                    }
                }
            }
        }

        public virtual Task Send(string message, CancellationToken token = default)
        {
            return WebSocket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, token);
        }

        protected virtual Task OnHello(Hello hello, CancellationToken token)
        {
            return Send("hello", token);
        }

        protected virtual async Task OnDisconnect(Disconnect connect, CancellationToken token)
        {
            if (WebSocket.State == WebSocketState.Open || WebSocket.State == WebSocketState.Aborted)
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect message received from slack",
                    token);
            }

            if (WebClient != null)
            {
                await ConnectAsync(token);
            }
        }

        private bool _disposed;
        private readonly Func<ClientWebSocket> _factory;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            
            WebSocket.Dispose();
        }

    }
}
