using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RimerApi.Application.Interfaces;

namespace RimerApi.Infrastructure.Services;

public class EventBackgroundWorker : IEventBackgroundWorker
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly INotificationClientService _notificationClient;
    private readonly ILogger<EventBackgroundWorker> _logger;

    public EventBackgroundWorker(
        IUserService userService, 
        IEmailService emailService, 
        INotificationClientService notificationClient,
        ILogger<EventBackgroundWorker> logger)
    {
        _userService = userService;
        _emailService = emailService;
        _notificationClient = notificationClient;
        _logger = logger;
    }

    public async Task ProcessTicketCreatedAsync(Guid ticketId, Guid? creatorId, string title)
    {
        if (creatorId.HasValue)
        {
            var user = await _userService.GetByIdAsync(creatorId.Value);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                var subject = $"Ticket Created: {title}";
                var body = $"Hello {user.FullName},\n\nYour ticket '{title}' has been created successfully. You will be notified when it is assigned or updated.\n\nTicket ID: {ticketId}";
                
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            await _notificationClient.NotifyStaffAdminAsync("New Ticket Created", $"Ticket '{title}' was just created by {user?.FullName ?? "User"}.");
        }
        else
        {
            await _notificationClient.NotifyStaffAdminAsync("New Ticket Created", $"Ticket '{title}' was just created by an external applicant.");
        }
    }

    public async Task ProcessTicketAssignedAsync(Guid ticketId, Guid assignedToId)
    {
        var assignee = await _userService.GetByIdAsync(assignedToId);
        if (assignee == null || string.IsNullOrEmpty(assignee.Email)) return;

        var subject = $"New Ticket Assigned";
        var body = $"Hello {assignee.FullName},\n\nA ticket has been assigned to you for resolution.\n\nTicket ID: {ticketId}";
        
        await _emailService.SendEmailAsync(assignee.Email, subject, body);
        await _notificationClient.NotifyUserAsync(assignedToId, "Ticket Assigned", "A new ticket has been assigned to you.");
    }

    public async Task ProcessTicketClosedAsync(Guid ticketId, Guid? creatorId)
    {
        if (creatorId.HasValue)
        {
            var creator = await _userService.GetByIdAsync(creatorId.Value);
            if (creator != null && !string.IsNullOrEmpty(creator.Email))
            {
                var subject = $"Your Ticket has been Closed";
                var body = $"Hello {creator.FullName},\n\nYour ticket has been marked as closed by the support staff.\n\nTicket ID: {ticketId}\n\nThank you for reaching out!";
                
                await _emailService.SendEmailAsync(creator.Email, subject, body);
            }
            await _notificationClient.NotifyUserAsync(creatorId.Value, "Ticket Closed", "Your ticket has been closed. Thank you.");
        }
    }
}
