using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace RimerApi.Infrastructure.WebSockets;

public interface IWebSocketConnectionManager
{
    int ConnectionCount { get; }
    bool HasActiveConnections { get; }
    Task AcceptConnectionAsync(string connectionId, WebSocket socket);
    Task RemoveConnectionAsync(string connectionId);
    Task BroadcastAsync(byte[] payload, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
}

public class WebSocketConnectionManager : IWebSocketConnectionManager
{
    private int _connectionCount;
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

    public int ConnectionCount => Volatile.Read(ref _connectionCount);
    public bool HasActiveConnections => ConnectionCount > 0;

    public async Task AcceptConnectionAsync(string connectionId, WebSocket socket)
    {
        if (ConnectionCount >= 5000)
        {
            // Close immediately if overloaded
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Server too busy", CancellationToken.None);
            return;
        }

        if (_sockets.TryAdd(connectionId, socket))
        {
            Interlocked.Increment(ref _connectionCount);
        }
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        if (_sockets.TryGetValue(connectionId, out var socket))
        {
            // CORRECT ORDER: Close FIRST, then Remove, to avoid race conditions.
            if (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted)
            {
                try
                {
                    // If it is completely broken, CloseAsync might throw, so we fallback to Abort.
                    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived || socket.State == WebSocketState.CloseSent)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                    else
                    {
                        socket.Abort();
                    }
                }
                catch
                {
                    socket.Abort();
                }
            }

            // After successfully closing/aborting, safely remove from dictionary
            if (_sockets.TryRemove(connectionId, out _))
            {
                Interlocked.Decrement(ref _connectionCount);
            }
        }
    }

    public async Task BroadcastAsync(byte[] payload, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
    {
        var buffer = new ArraySegment<byte>(payload);

        foreach (var kvp in _sockets)
        {
            var connectionId = kvp.Key;
            var socket = kvp.Value;

            if (socket.State != WebSocketState.Open)
                continue;

            try
            {
                var sendTask = socket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);

                if (await Task.WhenAny(sendTask, Task.Delay(2000, cancellationToken)) != sendTask)
                {
                    // Client is too slow -> Abort first, then remove
                    socket.Abort();
                    _ = RemoveConnectionAsync(connectionId); // Fire and forget removal since we are broadcasting
                    continue;
                }

                await sendTask;
            }
            catch
            {
                socket.Abort();
                _ = RemoveConnectionAsync(connectionId);
            }
        }
    }
}
