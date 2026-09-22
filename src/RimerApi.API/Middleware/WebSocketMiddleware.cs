using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RimerApi.Infrastructure.WebSockets;

namespace RimerApi.API.Middleware;

public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WebSocketMiddleware> _logger;

    public WebSocketMiddleware(RequestDelegate next, ILogger<WebSocketMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IWebSocketConnectionManager socketManager)
    {
        if (context.Request.Path == "/ws")
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var connectionId = Guid.NewGuid().ToString();

                await socketManager.AcceptConnectionAsync(connectionId, webSocket);

                // Keep the connection alive until the socket is closed or aborted
                await ReceiveLoopAsync(webSocket, connectionId, socketManager);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
        else
        {
            await _next(context);
        }
    }

    private async Task ReceiveLoopAsync(WebSocket socket, string connectionId, IWebSocketConnectionManager socketManager)
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socketManager.RemoveConnectionAsync(connectionId);
                }
            }
        }
        catch (WebSocketException)
        {
            // Client likely disconnected ungracefully
            await socketManager.RemoveConnectionAsync(connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in WebSocket receive loop for {ConnectionId}", connectionId);
            await socketManager.RemoveConnectionAsync(connectionId);
        }
    }
}
