using System;
using System.Threading.Tasks;

namespace RimerApi.Application.Interfaces;

/// <summary>
/// Olay tabanlı (Event-Driven) sistemin arkaplanda çalıştırılacak gerçek donanım/servis bağımlı işlemlerini icra eder.
/// Hangfire bu metotları kendi kuyruğundan çalıştıracaktır.
/// </summary>
public interface IEventBackgroundWorker
{
    Task ProcessTicketCreatedAsync(Guid ticketId, Guid? creatorId, string title);
    Task ProcessTicketAssignedAsync(Guid ticketId, Guid assignedToId);
    Task ProcessTicketClosedAsync(Guid ticketId, Guid? creatorId);
}
