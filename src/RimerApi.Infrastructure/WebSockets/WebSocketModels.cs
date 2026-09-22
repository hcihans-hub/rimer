using System.Text.Json.Serialization;

namespace RimerApi.Infrastructure.WebSockets;

public abstract class BaseEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

public class MetricsEvent : BaseEvent
{
    public MetricsEvent() => Type = "metrics_update";

    [JsonPropertyName("data")]
    public MetricsData Data { get; set; } = new();
}

public class MetricsData
{
    [JsonPropertyName("queueDepth")]
    public int QueueDepth { get; set; }
    
    [JsonPropertyName("throughput")]
    public int Throughput { get; set; }
    
    [JsonPropertyName("droppedRequests")]
    public int DroppedRequests { get; set; }
    
    [JsonPropertyName("systemMode")]
    public string SystemMode { get; set; } = "NORMAL";
    
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

public class AlertTriggerEvent : BaseEvent
{
    public AlertTriggerEvent() => Type = "alert_trigger";

    [JsonPropertyName("data")]
    public AlertTriggerData Data { get; set; } = new();
}

public class AlertTriggerData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("level")]
    public string Level { get; set; } = string.Empty;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class AlertClearEvent : BaseEvent
{
    public AlertClearEvent() => Type = "alert_clear";

    [JsonPropertyName("data")]
    public AlertClearData Data { get; set; } = new();
}

public class AlertClearData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class SystemModeChangeEvent : BaseEvent
{
    public SystemModeChangeEvent() => Type = "system_mode_change";

    [JsonPropertyName("data")]
    public SystemModeChangeData Data { get; set; } = new();
}

public class SystemModeChangeData
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;
}
