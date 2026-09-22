using System.Collections.Generic;

namespace RimerApi.Infrastructure.WebSockets;

public class StateEngine
{
    private readonly HashSet<string> _activeAlerts = new();
    private readonly List<BaseEvent> _generatedEvents = new();
    private string _lastSystemMode = "NORMAL";
    
    private const string CriticalQueueId = "critical_queue_100k";
    private const string HighQueueId = "high_queue_80k";
    
    private const string CriticalMessage = "Queue depth > 100k — PROTECTION ACTIVE";
    private const string HighMessage = "Queue depth > 80k — HIGH LOAD";

    public (List<BaseEvent> Events, string SystemMode) Evaluate(int queueDepth, int droppedRequests)
    {
        _generatedEvents.Clear();
        string currentSystemMode = "NORMAL";

        if (queueDepth > 100000)
        {
            currentSystemMode = "PROTECTION";
        }
        else if (queueDepth > 80000)
        {
            currentSystemMode = "HIGH_LOAD";
        }

        if (currentSystemMode != _lastSystemMode)
        {
            _generatedEvents.Add(new SystemModeChangeEvent
            {
                Data = new SystemModeChangeData { Mode = currentSystemMode }
            });
            _lastSystemMode = currentSystemMode;
        }

        if (queueDepth > 100000)
        {
            if (_activeAlerts.Add(CriticalQueueId))
            {
                _generatedEvents.Add(CreateTrigger(CriticalQueueId, "critical", CriticalMessage));
            }
            if (_activeAlerts.Remove(HighQueueId))
            {
                _generatedEvents.Add(CreateClear(HighQueueId));
            }
        }
        else if (queueDepth > 80000)
        {
            if (_activeAlerts.Add(HighQueueId))
            {
                _generatedEvents.Add(CreateTrigger(HighQueueId, "high", HighMessage));
            }
            if (_activeAlerts.Remove(CriticalQueueId))
            {
                _generatedEvents.Add(CreateClear(CriticalQueueId));
            }
        }
        else
        {
            if (_activeAlerts.Remove(CriticalQueueId))
            {
                _generatedEvents.Add(CreateClear(CriticalQueueId));
            }
            if (_activeAlerts.Remove(HighQueueId))
            {
                _generatedEvents.Add(CreateClear(HighQueueId));
            }
        }

        return (_generatedEvents, currentSystemMode);
    }

    private static AlertTriggerEvent CreateTrigger(string id, string level, string message) => new()
    {
        Data = new AlertTriggerData { Id = id, Level = level, Message = message }
    };

    private static AlertClearEvent CreateClear(string id) => new()
    {
        Data = new AlertClearData { Id = id }
    };
}
