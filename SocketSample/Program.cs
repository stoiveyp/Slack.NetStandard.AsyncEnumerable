﻿using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Slack.NetStandard.Socket;
using Slack.NetStandard.AsyncEnumerable;
using Slack.NetStandard.Interaction;
using Slack.NetStandard.Messages;
using Slack.NetStandard.Messages.Blocks;

namespace SocketSample
{
    class Program
    {
        private static string appToken = "xapp-1-A010P4LCK5Z-1715437354534-e6b624314f949769c043d3659301d3ebf5cd3520bb075417647ff20e83f59340";

        static async Task Main(string[] args)
        {
            using var client = new ClientWebSocket();
            var socketMode = new SocketModeClient();

            var cancel = new CancellationTokenSource();

            await socketMode.ConnectAsync(appToken, cancel.Token);
            var readKey = ReadKey(cancel);
            var process = ProcessMessages(socketMode, cancel.Token);

            try
            {
                await Task.WhenAll(readKey, process);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("You pressed escape. Shutting down");
            }
            finally
            {
                if (client.State == WebSocketState.Open)
                {
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "App shutting down", default);
                }
            }
        }

        private static Task ReadKey(CancellationTokenSource cancel)
        {
            return Task.Run(() =>
            {
                Console.WriteLine("Press ESC to quit");
                ConsoleKeyInfo cki = new ConsoleKeyInfo();
                do
                {
                    // true hides the pressed character from the console
                    cki = Console.ReadKey(true);

                    // Wait for an ESC
                } while (cki.Key != ConsoleKey.Escape);

                cancel.Cancel();
            }, default);
        }

        private static async Task ProcessMessages(SocketModeClient client, CancellationToken token)
        {
            await foreach (var envelope in client.EnvelopeAsyncEnumerable(token))
            {
                Console.WriteLine("processing envelope " + envelope.EnvelopeId);
                if (envelope.Payload is SlashCommand)
                {
                    await client.Send(JsonConvert.SerializeObject(new Acknowledge
                    {
                        EnvelopeId = envelope.EnvelopeId,
                        Payload = new Message
                        {
                            Blocks = new List<IMessageBlock>
                            {
                                new Section("This is from a socket....shh!")
                            }
                        }
                    }), token);
                }
            }
        }
    }
}