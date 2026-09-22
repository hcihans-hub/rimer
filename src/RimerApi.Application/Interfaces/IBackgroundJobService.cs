using System;
using System.Linq.Expressions;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Hangfire veya benzeri kuyruk sistemlerinin (Queue) soyutlanmış hali.
/// Application katmanı, altyapı detayını bilmeden arka plana iş gönderebilir.
/// </summary>
public interface IBackgroundJobService
{
    string Enqueue<T>(Expression<Action<T>> methodCall);
}
