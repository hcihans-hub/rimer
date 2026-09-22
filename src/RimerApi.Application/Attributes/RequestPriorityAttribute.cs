using System;
using RimerApi.Application.Enums;

namespace RimerApi.Application.Attributes;

/// <summary>
/// Decorates an endpoint with a specific priority for Adaptive Load Shedding.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequestPriorityAttribute : Attribute
{
    public RequestPriority Priority { get; }

    public RequestPriorityAttribute(RequestPriority priority)
    {
        Priority = priority;
    }
}
