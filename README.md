# Slack.NetStandard.AsyncEnumerable
Additional support for Slack.NetStandard apps running Socket Mode

## Creating a new client

```csharp
var socketMode = new SocketModeClient(); //Can pass in your own ClientWebSocket instance
await socketMode.ConnectAsync(appToken); //Can use an existing SlackWebApiClient
```

## Handling Envelopes

```csharp
await foreach (var envelope in client.EnvelopeAsyncEnumerable(token))
{
    Console.WriteLine("processing envelope " + envelope.EnvelopeId);
    if (envelope.Payload is SlashCommand) //for example
    {
      //your logic here
    }
}
```

## Hello and Disconnect

Hello and Disconnect messages are automatically handled by the ``SocketModeClient`` class.

``Hello`` messages are sent "hello" in response

``Disconnect`` closes the ClientWebSocket.
If there is a valid SlackWebApiClient, or the class was able to create one from an app token, then the class will attempt to retrieve a new connection URL and re-connect the ClientWebSocket automatically - allowing the ``foreach`` to continue without interruption.

If you require different functionality in these cases, there is an OnHello and OnDisconnect method which can be overwritten in a subclass

## Sample Application

The SocketSample app in the repo is a working example of a console application that uses this library, just supply your own app token