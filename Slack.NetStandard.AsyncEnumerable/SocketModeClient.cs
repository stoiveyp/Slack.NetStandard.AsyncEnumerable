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
    public class SocketModeClient
    {
        public ClientWebSocket WebSocket { get; }
        public SlackWebApiClient WebClient { get; protected set; }

        protected SocketModeClient() : this(new ClientWebSocket())
        {

        }

        protected SocketModeClient(ClientWebSocket socket)
        {
            WebSocket = socket;
        }

        public Task ConnectAsync(string appToken, CancellationToken token = default)
        {
            return ConnectAsync(new SlackWebApiClient(appToken), token);
        }

        public virtual async Task ConnectAsync(SlackWebApiClient webClient, CancellationToken token = default)
        {
            WebClient = webClient;
            var connection = await WebClient.Apps.OpenConnection();
            if (!connection.OK)
            {
                throw new InvalidOperationException($"Error from slack: " + connection.Error);
            }

            await WebSocket.ConnectAsync(new Uri(connection.Url, UriKind.Absolute), token);
        }

        public virtual async IAsyncEnumerable<Envelope> EnvelopeAsyncEnumerable([EnumeratorCancellation] CancellationToken token)
        {
            while (!token.IsCancellationRequested && WebSocket.State == WebSocketState.Open)
            {
                Disconnect disconnect = null;
                await foreach (var socketModeObject in WebSocket.SocketModeObjects(token))
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
                }
            }
        }

        public Task Send(string message, CancellationToken token = default)
        {
            return WebSocket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, token);
        }

        protected virtual Task OnHello(Hello hello, CancellationToken token)
        {
            return Send("hello", token);
        }

        protected virtual async Task OnDisconnect(Disconnect connect, CancellationToken token)
        {
            await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect message received from slack",
                token);
            if (WebClient != null)
            {
                await ConnectAsync(WebClient, token);
            }
        }
    }
}
