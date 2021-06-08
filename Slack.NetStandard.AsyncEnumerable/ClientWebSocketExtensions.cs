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
            var closing = false;
            serializer ??= JsonSerializer.CreateDefault();

            while (!token.IsCancellationRequested)
            {
                var result = await client.ReceiveAsync(memory, token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    closing = true;
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

                object returnVal = null;

                try
                {
                    using var reader = new JsonTextReader(new StringReader(msg));
                    if (msg.Contains("envelope_id"))
                    {
                        returnVal = serializer.Deserialize<Envelope>(reader);
                    }
                    else if (msg.Contains("hello"))
                    {
                        returnVal = serializer.Deserialize<Hello>(reader);
                    }
                    else
                    {
                        returnVal = serializer.Deserialize<Disconnect>(reader);
                    }
                }
                catch (Exception ex)
                {
                    mem.Seek(0, SeekOrigin.Begin);
                    using var sr = new StringReader(msg);
                    returnVal = new SerializationProblem(sr.ReadToEnd(), ex);
                }

                yield return returnVal;

                if (memory.Length > 0)
                {
                    mem.SetLength(0);
                }
            }

            if (closing)
            {
                yield return new Disconnect {Reason = "Close Message Received"};
            }
        }
    }
}
