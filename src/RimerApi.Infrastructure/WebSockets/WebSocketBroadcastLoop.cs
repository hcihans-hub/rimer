using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RimerApi.Infrastructure.WebSockets;

public interface IMetricsProvider
{
    (int QueueDepth, int Throughput, int DroppedRequests) GetCurrentMetrics();
}

// Temporary mock provider for testing the broadcast loop.
public class MockMetricsProvider : IMetricsProvider
{
    private readonly Random _random = new();
    public (int QueueDepth, int Throughput, int DroppedRequests) GetCurrentMetrics()
    {
        // Simulate fluctuating metrics around the thresholds
        return (
            QueueDepth: _random.Next(70000, 110000), 
            Throughput: _random.Next(100, 500), 
            DroppedRequests: _random.Next(0, 50)
        );
    }
}

public class WebSocketBroadcastLoop : BackgroundService
{
    private readonly StateEngine _stateEngine;
    private readonly ILogger<WebSocketBroadcastLoop> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ArrayBufferWriter<byte> _buffer = new(1024);

    private readonly IMetricsProvider _metricsProvider; 
    private readonly IWebSocketConnectionManager _socketManager;

    public WebSocketBroadcastLoop(
        IMetricsProvider metricsProvider,
        IWebSocketConnectionManager socketManager,
        ILogger<WebSocketBroadcastLoop> logger)
    {
        _metricsProvider = metricsProvider;
        _socketManager = socketManager;
        _logger = logger;
        _stateEngine = new StateEngine();
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WebSocket Broadcast Loop started.");
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // BONUS: Prevent useless serialize + allocate
                if (!_socketManager.HasActiveConnections)
                    continue;

                // 1. Collect Metrics
                var metrics = _metricsProvider.GetCurrentMetrics();

                // 2. Run State Engine
                var (events, systemMode) = _stateEngine.Evaluate(metrics.QueueDepth, metrics.DroppedRequests);

                // 3. Broadcast High Priority Events FIRST (Alerts & Mode Changes)
                if (events.Count > 0)
                {
                    foreach (var evt in events)
                    {
                        await BroadcastAsync(evt, stoppingToken);
                    }
                }

                // 4. Broadcast Metrics LAST
                var metricsEvent = new MetricsEvent
                {
                    Data = new MetricsData
                    {
                        QueueDepth = metrics.QueueDepth,
                        Throughput = metrics.Throughput,
                        DroppedRequests = metrics.DroppedRequests,
                        SystemMode = systemMode,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    }
                };

                await BroadcastAsync(metricsEvent, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred in WebSocket broadcast loop.");
            }
        }
    }

    private async Task BroadcastAsync(BaseEvent evt, CancellationToken token)
    {
        _buffer.Clear();
        using (var writer = new Utf8JsonWriter(_buffer))
        {
            JsonSerializer.Serialize(writer, evt, evt.GetType(), _jsonOptions);
        }
        
        // Safe Copy to avoid data corruption in async shared buffer
        var payload = _buffer.WrittenMemory.ToArray();
        
        await _socketManager.BroadcastAsync(payload, WebSocketMessageType.Text, true, token);
    }
}
