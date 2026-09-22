using Microsoft.Extensions.Logging;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Public;
using RimerApi.Application.DTOs.Ticket;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Enums;
using RimerApi.Domain.Interfaces;

namespace RimerApi.Application.Services;

/// <summary>
/// Handles public (external) ticket creation and tracking.
/// No authentication required for these operations.
/// </summary>
public class PublicTicketService
{
    private readonly ITicketRepository _ticketRepo;
    private readonly IRepository<Department> _deptRepo;
    private readonly IEmailService _emailService;
    private readonly ISystemLogService _systemLogService;
    private readonly INotificationService _notificationService;
    private readonly IRepository<TicketHistory> _historyRepo;
    private readonly ILogger<PublicTicketService> _logger;

    public PublicTicketService(
        ITicketRepository ticketRepo,
        IRepository<Department> deptRepo,
        IEmailService emailService,
        ISystemLogService systemLogService,
        INotificationService notificationService,
        IRepository<TicketHistory> historyRepo,
        ILogger<PublicTicketService> logger)
    {
        _ticketRepo = ticketRepo;
        _deptRepo = deptRepo;
        _emailService = emailService;
        _systemLogService = systemLogService;
        _notificationService = notificationService;
        _historyRepo = historyRepo;
        _logger = logger;
    }

    /// <summary>
    /// Gets the most recent generic ticket events for the public ticker.
    /// </summary>
    public async Task<ServiceResult<List<PublicTickerDto>>> GetPublicTickerAsync()
    {
        try
        {
            // Fetch recent 10 tickets
            var resultPage = await _ticketRepo.GetFilteredAsync(null, null, null, null, null, null, 1, 10);
            var recentTickets = resultPage.Items;

            var result = new List<PublicTickerDto>();
            foreach(var t in recentTickets)
            {
                if (result.Count >= 5) break;

                string categoryLabel = t.Category switch {
                    TicketCategory.Complaint => "Şikayet talebi",
                    TicketCategory.Suggestion => "Öneri",
                    TicketCategory.Request => "Talep",
                    TicketCategory.Thanks => "Teşekkür",
                    TicketCategory.Question => "Soru",
                    TicketCategory.InfoRequest => "Bilgi talebi",
                    _ => "Talep"
                };

                string msg = "";
                
                // If it is newly submitted
                if (t.Status == TicketStatus.Submitted)
                {
                    msg = $"Yeni bir {categoryLabel} sisteme ulaştı.";
                }
                else if (t.Status == TicketStatus.WaitingDepartment && t.Department != null)
                {
                    msg = $"Bir {categoryLabel} ilgili birime iletildi.";
                }
                else if (t.Status == TicketStatus.Answered)
                {
                    msg = $"Bir {categoryLabel} kurum tarafından cevaplandı.";
                }
                else if (t.Status == TicketStatus.Closed)
                {
                    msg = $"Bir {categoryLabel} sonuçlandırılarak kapatıldı.";
                }
                else if (t.Status == TicketStatus.Reviewing)
                {
                    msg = $"Bir {categoryLabel} şu an inceleniyor.";
                }

                if (!string.IsNullOrEmpty(msg))
                {
                    // Use LastTransferredAt if it's in a department, else CreatedAt or UpdatedAt
                    var time = t.LastTransferredAt ?? t.CreatedAt;
                    // Eğer ticket kapatılmış veya cevaplanmışsa updatedAt daha uygundur ama basitçe:
                    if (t.Status == TicketStatus.Answered || t.Status == TicketStatus.Closed)
                        time = t.CreatedAt; // Idealde LastUpdatedAt vs olur
                    
                    result.Add(new PublicTickerDto { Message = msg, Timestamp = time });
                }
            }

            // Ensure it is strictly ordered by descending timestamp
            result = result.OrderByDescending(x => x.Timestamp).ToList();

            return ServiceResult<List<PublicTickerDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public ticker");
            return ServiceResult<List<PublicTickerDto>>.Failure("Ticker load failed.");
        }
    }

    /// <summary>
    /// Create a public ticket from an external applicant.
    /// </summary>
    public async Task<ServiceResult<PublicApplyResponseDto>> CreatePublicTicketAsync(PublicApplyDto dto)
    {
        if (!dto.TermsAccepted)
            return ServiceResult<PublicApplyResponseDto>.Failure("Terms must be accepted.");

        // Generate tracking code
        var trackingCode = await GenerateTrackingCodeAsync();

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Message,
            ReferenceNo = trackingCode,
            Category = (TicketCategory)dto.Category,
            Status = TicketStatus.Submitted,
            Priority = ((TicketCategory)dto.Category) == TicketCategory.Complaint ? TicketPriority.Critical : TicketPriority.Normal,
            TermsAccepted = true,
            IsExternal = true,
            TrackingCode = trackingCode,
            GuestIdentityNumber = dto.IdentityNumber,
            GuestTitle = dto.TitleName,
            GuestName = dto.FirstName,
            GuestSurname = dto.LastName,
            GuestEmail = dto.Email,
            GuestPhone = dto.Phone,
            GuestAddress = dto.Address,
            HidePersonalInfo = dto.HidePersonalInfo,
            DepartmentId = dto.DepartmentId,
            AssignedDepartmentId = dto.DepartmentId, // Directly assigned to selected department so it appears in DeptDashboard
            AttachmentUrl = dto.AttachmentUrl,
            CreatorId = null, // No account for external users
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _ticketRepo.AddAsync(ticket);
        await _ticketRepo.SaveChangesAsync();

        // System Log Audit
        await _systemLogService.LogAsync(
            null, 
            "Create (Public)", 
            "Ticket", 
            ticket.Id.ToString(), 
            $"External ticket created by {dto.FirstName} {dto.LastName} ({dto.Email}). Tracking: {trackingCode}"
        );

        _logger.LogInformation("Public ticket created: {TrackingCode} by {FirstName} {LastName} ({GuestEmail})",
            trackingCode, dto.FirstName, dto.LastName, dto.Email);

        // Notify department users about the new public ticket
        if (ticket.AssignedDepartmentId.HasValue)
        {
            await _notificationService.NotifyDepartmentUsersAsync(
                ticket.AssignedDepartmentId.Value,
                $"Yeni halk başvurusu: {ticket.Title} (#{trackingCode})",
                ticket.Id, "NewTicket");
        }

        // Send confirmation email
        await _emailService.SendEmailAsync(
            dto.Email,
            $"RİMER Başvurunuz Alındı — {trackingCode}",
            $"Sayın {dto.FirstName} {dto.LastName},\n\n" +
            $"Başvurunuz başarıyla alınmıştır.\n\n" +
            $"Takip Numaranız: {trackingCode}\n\n" +
            $"Bu numara ile başvurunuzun durumunu takip edebilirsiniz.\n\n" +
            $"Saygılarımızla,\nRİMER İletişim Merkezi");

        return ServiceResult<PublicApplyResponseDto>.Success(new PublicApplyResponseDto
        {
            TrackingCode = trackingCode,
            Message = "Başvurunuz başarıyla alınmıştır."
        });
    }

    /// <summary>
    /// Track a public ticket by tracking code and email verification.
    /// </summary>
    public async Task<ServiceResult<PublicTrackResponseDto>> TrackTicketAsync(PublicTrackRequestDto dto)
    {
        var tickets = await _ticketRepo.FindAsync(t =>
            t.TrackingCode == dto.TrackingCode &&
            t.GuestEmail == dto.Email &&
            t.IsExternal &&
            t.CreatorId == null);

        var baseTicket = tickets.FirstOrDefault();
        if (baseTicket == null)
            return ServiceResult<PublicTrackResponseDto>.NotFound("Başvuru bulunamadı. Takip numarasını ve e-posta adresinizi kontrol edin.");

        var ticket = await _ticketRepo.GetByIdWithDetailsAsync(baseTicket.Id) ?? baseTicket;

        // Audit tracking access
        await _systemLogService.LogAsync(
            null,
            "Track (Public)",
            "Ticket",
            ticket.Id.ToString(),
            $"External ticket tracking accessed for {ticket.TrackingCode} from {dto.Email}"
        );

        string? deptName = ticket.AssignedDepartment?.Name;

        var timeline = new List<PublicTimelineItemDto>
        {
            new() { Action = "Başvuru alındı", Timestamp = ticket.CreatedAt, Detail = "Başvurunuz sisteme kaydedildi." }
        };

        if (ticket.Histories != null)
        {
            foreach (var h in ticket.Histories.OrderBy(x => x.ChangedAt))
            {
                if (h.Action == "Created") continue;
                
                string actionName = h.Action switch
                {
                    "Transferred" => "Yönlendirildi",
                    "Assigned" => "Görevlendirildi",
                    "StatusChanged" => "Durum Güncellendi",
                    "TakenOwnership" => "İşleme Alındı",
                    "Closed" => "Kapatıldı",
                    "InternalStatusChanged" => "İç Durum Güncellendi",
                    _ => h.Action
                };

                string finalAction = !string.IsNullOrEmpty(h.DepartmentName) ? $"{h.DepartmentName} — {actionName}" : actionName;

                string detail = "";
                if (h.Action == "Transferred") detail = $"Başvurunuz yönlendirildi. {(h.DepartmentName != null ? $"Birim: {h.DepartmentName} " : "")}{(h.Note != null ? $"Not: {h.Note}" : "")}";
                else if (h.Action == "Assigned") detail = "İlgili personele atandı.";
                else if (h.Action == "StatusChanged") detail = $"Durum güncellendi: {h.NewValue}";
                else if (h.Action == "TakenOwnership") detail = "İlgili personel başvuruyu işleme aldı.";
                else if (h.Action == "Closed") detail = "Başvuru sonuçlandırıldı.";

                if (!string.IsNullOrEmpty(detail))
                    timeline.Add(new() { Action = finalAction, Timestamp = h.ChangedAt, Detail = detail });
            }
        }

        string? latestReply = null;
        if (ticket.Replies != null)
        {
            foreach (var r in ticket.Replies.OrderBy(x => x.CreatedAt))
            {
                timeline.Add(new() { Action = "Cevaplandı", Timestamp = r.CreatedAt, Detail = r.Message });
                latestReply = r.Message;
            }
        }

        timeline = timeline.OrderBy(x => x.Timestamp).ToList();

        var pastDepartments = new List<string>();
        if (ticket.Transfers != null)
        {
            foreach (var t in ticket.Transfers.OrderBy(x => x.CreatedAt))
            {
                if (t.FromDepartment != null)
                    pastDepartments.Add(t.FromDepartment.Name);
            }
        }
        if (ticket.Department != null)
        {
            pastDepartments.Add(ticket.Department.Name);
        }
        pastDepartments = pastDepartments.Distinct().Where(d => d != deptName).ToList();

        return ServiceResult<PublicTrackResponseDto>.Success(new PublicTrackResponseDto
        {
            TrackingCode = ticket.TrackingCode!,
            Title = ticket.Title,
            Description = ticket.Description,
            Category = ticket.Category.ToString(),
            Status = ticket.Status.ToString(),
            StatusCode = (int)ticket.Status,
            Department = deptName,
            CreatedAt = ticket.CreatedAt,
            LastUpdatedAt = ticket.UpdatedAt,
            LatestReply = latestReply,
            GuestFirstName = ticket.GuestName,
            GuestLastName = ticket.GuestSurname,
            GuestEmail = ticket.GuestEmail,
            GuestPhone = ticket.GuestPhone,
            GuestAddress = ticket.GuestAddress,
            GuestIdentityNumber = ticket.GuestIdentityNumber,
            GuestTitle = ticket.GuestTitle,
            HidePersonalInfo = ticket.HidePersonalInfo,
            Timeline = timeline,
            PastDepartments = pastDepartments
        });
    }

    /// <summary>
    /// List all active departments for the public form dropdown.
    /// </summary>
    public async Task<List<PublicDepartmentDto>> GetDepartmentsAsync()
    {
        var depts = await _deptRepo.FindAsync(d => d.IsActive);
        return depts
            .OrderBy(d => d.Name)
            .Select(d => new PublicDepartmentDto { Id = d.Id, Name = d.Name })
            .ToList();
    }

    /// <summary>
    /// Generate a unique tracking code in format RIM-YYYY-NNNNNN.
    /// </summary>
    private async Task<string> GenerateTrackingCodeAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"RIM-{year}-";

        // Get the max sequence number for this year
        var allTickets = await _ticketRepo.FindAsync(t =>
            t.TrackingCode != null && t.TrackingCode.StartsWith(prefix));

        var maxSeq = 0;
        foreach (var t in allTickets)
        {
            if (t.TrackingCode != null && t.TrackingCode.Length > prefix.Length)
            {
                if (int.TryParse(t.TrackingCode[prefix.Length..], out var seq) && seq > maxSeq)
                    maxSeq = seq;
            }
        }

        // Also check ReferenceNo for existing sequence numbers
        var allRefs = await _ticketRepo.FindAsync(t =>
            t.ReferenceNo.StartsWith(prefix));

        foreach (var t in allRefs)
        {
            if (t.ReferenceNo.Length > prefix.Length)
            {
                if (int.TryParse(t.ReferenceNo[prefix.Length..], out var seq) && seq > maxSeq)
                    maxSeq = seq;
            }
        }

        return $"{prefix}{(maxSeq + 1):D6}";
    }
}
