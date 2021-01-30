using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Slack.NetStandard.Socket;

namespace Slack.NetStandard.AsyncEnumerable
{
    public static class ClientWebSocketExtensions
    {
        public static async IAsyncEnumerable<object> SocketModeObjects(this ClientWebSocket client, [EnumeratorCancellation] CancellationToken token, JsonSerializer serializer = default, int bufferSize = 1024)
        {
            var mem = new MemoryStream(bufferSize);
            var memory = new Memory<byte>(new byte[bufferSize]);
            serializer ??= JsonSerializer.CreateDefault();

            while (!token.IsCancellationRequested)
            {
                var result = await client.ReceiveAsync(memory, token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (!result.EndOfMessage || mem.Length > 0)
                {
                    mem.Write(memory.Span);
                }

                if (!result.EndOfMessage)
                {
                    continue;
                }

                var msg = mem.Length > 0 ? Encoding.UTF8.GetString(mem.ToArray()) : Encoding.UTF8.GetString(memory.Span);


                using var reader = new JsonTextReader(new StreamReader(mem, Encoding.UTF8, false, 1024, true));
                if (msg.Contains("envelope_id"))
                {
                    yield return serializer.Deserialize<Envelope>(reader);
                }
                else if (msg.Contains("hello"))
                {
                    yield return serializer.Deserialize<Hello>(reader);
                }
                else
                {
                    yield return serializer.Deserialize<Disconnect>(reader);
                    yield break;
                }
                
                if (memory.Length > 0)
                {
                    mem.SetLength(0);
                }
            }
        }
    }
}
