using MediatR;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Ticket;
using RimerApi.Application.DTOs.Analytics;
using RimerApi.Application.Interfaces;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Enums;
using RimerApi.Domain.Interfaces;
using RimerApi.Domain.Events;
using RimerApi.Application.Events;

namespace RimerApi.Application.Services;

/// <summary>
/// Business logic for ticket operations.
/// Authorization is enforced here — controllers only pass CurrentUser.
/// </summary>
public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateTicketDto> _createValidator;
    private readonly IValidator<UpdateTicketStatusDto> _statusValidator;
    private readonly IValidator<AssignTicketDto> _assignValidator;
    private readonly ILogger<TicketService> _logger;
    private readonly IPublisher _publisher;
    private readonly ISystemLogService _systemLogService;
    private readonly IEmailService _emailService;

    private readonly IRepository<TicketReminder> _reminderRepository;

    public TicketService(
        ITicketRepository ticketRepository,
        IDepartmentRepository departmentRepository,
        IUserService userService,
        INotificationService notificationService,
        IMapper mapper,
        IValidator<CreateTicketDto> createValidator,
        IValidator<UpdateTicketStatusDto> statusValidator,
        IValidator<AssignTicketDto> assignValidator,
        ILogger<TicketService> logger,
        IPublisher publisher,
        ISystemLogService systemLogService,
        IEmailService emailService,
        IRepository<TicketReminder> reminderRepository)
    {
        _ticketRepository = ticketRepository;
        _departmentRepository = departmentRepository;
        _userService = userService;
        _notificationService = notificationService;
        _mapper = mapper;
        _createValidator = createValidator;
        _statusValidator = statusValidator;
        _assignValidator = assignValidator;
        _logger = logger;
        _publisher = publisher;
        _systemLogService = systemLogService;
        _emailService = emailService;
        _reminderRepository = reminderRepository;
    }

    // ══════════════════════════════════════════════════════════════
    // CREATE — any authenticated user
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<TicketResponseDto>> CreateAsync(CreateTicketDto dto, string? attachmentUrl, CurrentUser currentUser)
    {
        // 1. Mandatory Terms Check
        if (!dto.TermsAccepted)
            return ServiceResult<TicketResponseDto>.Failure("Terms must be accepted.");

        if (currentUser.IsRector || currentUser.IsUnitUser)
            return ServiceResult<TicketResponseDto>.Forbidden($"{currentUser.Role} role is read-only for new tickets and cannot create them.");

        // Validate input
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<TicketResponseDto>.Failure(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        // Verify creator exists
        if (!await _userService.ExistsAsync(currentUser.Id))
            return ServiceResult<TicketResponseDto>.Failure("Creator user not found.", 404);

        // Verify department exists (if provided)
        if (dto.DepartmentId.HasValue)
        {
            var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId.Value);
            if (department is null)
                return ServiceResult<TicketResponseDto>.Failure("Department not found.", 404);
        }

        // Map and create
        var ticket = _mapper.Map<Ticket>(dto);
        ticket.CreatorId = currentUser.Id;
        ticket.ReferenceNo = await _ticketRepository.GenerateReferenceNoAsync();
        
        // Assign new wizard fields
        ticket.AttachmentUrl = attachmentUrl;
        ticket.InstitutionName = dto.InstitutionName;
        ticket.TermsAccepted = dto.TermsAccepted;

        // AssignedDepartmentId mirrors DepartmentId on creation (the authoritative routing field)
        ticket.AssignedDepartmentId = dto.DepartmentId;

        // Ensure complaints are always Critical priority
        if (ticket.Category == TicketCategory.Complaint)
        {
            ticket.Priority = TicketPriority.Critical;
        }

        await _ticketRepository.AddAsync(ticket);
        await _ticketRepository.SaveChangesAsync();

        _logger.LogInformation("Ticket {TicketId} created by user {UserId} ({Role}).",
            ticket.Id, currentUser.Id, currentUser.Role);

        await _systemLogService.LogAsync(
            currentUser.Id.ToString(), 
            "Create Ticket", 
            "Ticket", 
            ticket.Id.ToString(), 
            $"Talep Oluşturuldu: {ticket.Title}"
        );

        // Record creation in history
        await AddHistoryAsync(ticket.Id, currentUser.Id, "Created",
            oldValue: null,
            newValue: $"Ticket created: {ticket.Title}");

        await _publisher.Publish(new DomainEventNotification<TicketCreatedEvent>(
            new TicketCreatedEvent(ticket.Id, ticket.CreatorId, ticket.Title)));

        // Notify department users when a new ticket arrives
        if (ticket.AssignedDepartmentId.HasValue)
            await _notificationService.NotifyDepartmentUsersAsync(
                ticket.AssignedDepartmentId.Value,
                $"1 yeni talebiniz var: {ticket.Title}",
                ticket.Id, "NewTicket");

        return await BuildResponseAsync(ticket);
    }

    // ══════════════════════════════════════════════════════════════
    // GET BY ID — ownership check for students
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<TicketResponseDto>> GetByIdAsync(Guid ticketId, CurrentUser currentUser)
    {
        var ticket = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
        if (ticket is null)
            return ServiceResult<TicketResponseDto>.NotFound("Ticket not found.");

        // ── Authorization ──────────────────────────────────────────
        if (currentUser.IsRector)
            return ServiceResult<TicketResponseDto>.Forbidden("Rector role has analytics-only access. Viewing ticket details is forbidden.");

        // Operator can view any ticket (no department restriction)
        if (!currentUser.IsOperator)
        {
            if (currentUser.IsUnitUser || currentUser.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            {
                if (!currentUser.DepartmentId.HasValue)
                    return ServiceResult<TicketResponseDto>.Forbidden(
                        "Birim yetkilisi olarak atanmış bir biriminiz bulunmuyor. Lütfen sistem yöneticisi ile iletişime geçin.");

                // UnitUser and Staff can only see tickets in their department
                if (ticket.AssignedDepartmentId != currentUser.DepartmentId)
                {
                    _logger.LogWarning("Unauthorized access attempt: User {UserId} from Dept {UserDeptId} tried to access Ticket {TicketId} assigned to Dept {TicketDeptId}",
                        currentUser.Id, currentUser.DepartmentId, ticket.Id, ticket.AssignedDepartmentId);

                    return ServiceResult<TicketResponseDto>.Forbidden(
                        "Sadece kendi biriminize atanmış talepleri görüntüleyebilirsiniz.");
                }
            }
            else if (!currentUser.IsAdmin && !currentUser.IsAuditor && ticket.CreatorId != currentUser.Id)
            {
                _logger.LogWarning(
                    "User {UserId} ({Role}) attempted to access ticket {TicketId} owned by {OwnerId}.",
                    currentUser.Id, currentUser.Role, ticketId, ticket.CreatorId);
                return ServiceResult<TicketResponseDto>.Forbidden(
                    "You do not have permission to view this ticket.");
            }
        }

        // ── Auto-Status Update (Read = Reviewing) ──────────────────
        // If a UnitUser views a 'Submitted' ticket assigned to their department, 
        // we automatically move it to 'Reviewing' (İşlemde) status.
        if (currentUser.IsUnitUser && 
            ticket.AssignedDepartmentId == currentUser.DepartmentId && 
            ticket.Status == TicketStatus.Submitted)
        {
            ticket.Status = TicketStatus.Reviewing;
            _ticketRepository.Update(ticket);
            await _ticketRepository.SaveChangesAsync();

            await AddHistoryAsync(ticket.Id, currentUser.Id, "StatusChanged",
                oldValue: "Submitted",
                newValue: "Reviewing (Auto-Read)");

            await _systemLogService.LogAsync(
                currentUser.Id.ToString(), 
                "Ticket Viewed/ReviewStarted", 
                "Ticket", 
                ticket.Id.ToString(), 
                $"Talep Birim Tarafından Okundu, Durum 'İnceleniyor' Olarak Güncellendi."
            );
        }

        return await BuildDetailResponseAsync(ticket);
    }

    // ══════════════════════════════════════════════════════════════
    // GET LIST — students automatically scoped to own tickets
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<PagedResult<TicketResponseDto>>> GetListAsync(
        TicketFilterDto filter, CurrentUser currentUser)
    {
        // ── Authorization ──────────────────────────────────────────
        if (currentUser.IsRector)
            return ServiceResult<PagedResult<TicketResponseDto>>.Forbidden("Rector role has analytics-only access. Listing tickets is forbidden.");

        // ── Role-Based Data Scoping ──────────────────────────────────
        if (currentUser.IsUnitUser || currentUser.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        {
            // Unit users/staff MUST have a department and are strictly isolated to it
            if (!currentUser.DepartmentId.HasValue)
                return ServiceResult<PagedResult<TicketResponseDto>>.Forbidden(
                    "Birim yetkilisi/personeli olarak atanmış bir biriminiz bulunmuyor.");

            filter.DepartmentId = currentUser.DepartmentId.Value;
        }
        else if (currentUser.Role.Equals("Student", StringComparison.OrdinalIgnoreCase))
        {
            // Students only see their own tickets
            filter.CreatorId = currentUser.Id;
            filter.DepartmentId = null; // Ignore any dept filter passed by student
        }
        // Admins, Operators and Auditors fall through — see all tickets (filtered by what they pass in)

        var (items, totalCount) = await _ticketRepository.GetFilteredAsync(
            filter.Status,
            filter.Category,
            filter.DepartmentId,
            filter.CreatorId,
            filter.AssignedToId,
            filter.SearchTerm,
            filter.PageNumber,
            filter.PageSize,
            filter.ExcludeClosed,
            filter.StartDate,
            filter.EndDate);

        var ticketList = items.ToList();

        // Batch-resolve all user ids for efficient lookup
        var userIds = ticketList
            .SelectMany(t => new[] { t.CreatorId ?? Guid.Empty, t.AssignedToId ?? Guid.Empty })
            .Where(id => id != Guid.Empty)
            .Distinct();

        var usersMap = await _userService.GetByIdsAsync(userIds, includeDeleted: true);

        // Map tickets to DTOs
        var ticketDtos = ticketList.Select(t =>
        {
            var dto = _mapper.Map<TicketResponseDto>(t);
            dto.Creator = t.CreatorId.HasValue ? usersMap.GetValueOrDefault(t.CreatorId.Value) : null;
            if (t.AssignedToId.HasValue)
                dto.AssignedTo = usersMap.GetValueOrDefault(t.AssignedToId.Value);
            return dto;
        }).ToList();

        var result = new PagedResult<TicketResponseDto>
        {
            Items = ticketDtos,
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };

        return ServiceResult<PagedResult<TicketResponseDto>>.Success(result);
    }

    // ══════════════════════════════════════════════════════════════
    // UPDATE STATUS — Staff and Admin only
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<TicketResponseDto>> UpdateStatusAsync(
        Guid ticketId, UpdateTicketStatusDto dto, CurrentUser currentUser)
    {
        // ── Authorization ──
        if (!currentUser.IsStaffOrAdmin || currentUser.IsRector)
            return ServiceResult<TicketResponseDto>.Forbidden(
                "Only Staff and Admin can update ticket status. Rector role is read-only.");

        // Validate input
        var validation = await _statusValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<TicketResponseDto>.Failure(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        // Get ticket
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult<TicketResponseDto>.NotFound("Ticket not found.");

        var oldStatus = ticket.Status;

        // Prevent no-op updates
        if (oldStatus == dto.Status)
            return ServiceResult<TicketResponseDto>.Failure("Ticket already has this status.");

        // ── Strict status transition rules ──────────────────────────
        // Open       → InProgress, Closed
        // InProgress → Closed
        // Closed     → (no transitions allowed)
        if (!IsValidTransition(oldStatus, dto.Status))
            return ServiceResult<TicketResponseDto>.Failure(
                $"Invalid status transition: {oldStatus} → {dto.Status}. " +
                GetTransitionHint(oldStatus));

        // Update status
        ticket.Status = dto.Status;
        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Ticket {TicketId} status changed {OldStatus} → {NewStatus} by {UserId} ({Role}).",
            ticketId, oldStatus, dto.Status, currentUser.Id, currentUser.Role);

        // Record in history
        await AddHistoryAsync(ticketId, currentUser.Id, "StatusChanged",
            oldValue: oldStatus.ToString(),
            newValue: $"{dto.Status}",
            customNote: dto.Note);

        if (dto.Note is not null)
        {
            await _systemLogService.LogAsync(
                currentUser.Id.ToString(), 
                "Reply to Ticket", 
                "Ticket", 
                ticketId.ToString(),
                $"Talebe Not Eklendi: {dto.Note}"
            );
        }

        if (dto.Status == TicketStatus.Closed && oldStatus != TicketStatus.Closed)
        {
            await _publisher.Publish(new DomainEventNotification<TicketClosedEvent>(
                new TicketClosedEvent(ticketId, currentUser.Id, ticket.CreatorId)));
        }

        var updatedTicket = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
        return await BuildResponseAsync(updatedTicket!);
    }

    // ══════════════════════════════════════════════════════════════
    // ASSIGN — Admin only
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<TicketResponseDto>> AssignAsync(
        Guid ticketId, AssignTicketDto dto, CurrentUser currentUser)
    {
        // ── Authorization: Only Admin can assign ──
        if (!currentUser.IsAdmin)
            return ServiceResult<TicketResponseDto>.Forbidden(
                "Only Admin can assign tickets.");

        // Validate input
        var validation = await _assignValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<TicketResponseDto>.Failure(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        // Get ticket
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult<TicketResponseDto>.NotFound("Ticket not found.");

        // Verify assignee exists
        if (dto.AssignedToId.HasValue && !await _userService.ExistsAsync(dto.AssignedToId.Value))
            return ServiceResult<TicketResponseDto>.Failure("Assigned user not found.", 404);

        // Verify department exists
        if (dto.DepartmentId.HasValue)
        {
            var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId.Value);
            if (department is null)
                return ServiceResult<TicketResponseDto>.Failure("Department not found.", 404);
        }

        // Track changes for history
        var changes = new List<string>();

        if (dto.AssignedToId.HasValue && ticket.AssignedToId != dto.AssignedToId)
        {
            var oldAssignee = ticket.AssignedToId.HasValue
                ? (await _userService.GetByIdAsync(ticket.AssignedToId.Value, includeDeleted: true))?.FullName ?? "Unknown"
                : "Unassigned";
            var newAssignee = (await _userService.GetByIdAsync(dto.AssignedToId.Value, includeDeleted: true))?.FullName ?? "Unknown";

            ticket.AssignedToId = dto.AssignedToId;
            changes.Add($"Assignee: {oldAssignee} → {newAssignee}");
        }

        if (dto.DepartmentId.HasValue && ticket.DepartmentId != dto.DepartmentId)
        {
            var oldDept = ticket.DepartmentId.HasValue
                ? (await _departmentRepository.GetByIdAsync(ticket.DepartmentId.Value))?.Name ?? "Unknown"
                : "Unassigned";
            var newDept = (await _departmentRepository.GetByIdAsync(dto.DepartmentId.Value))?.Name ?? "Unknown";

            ticket.DepartmentId = dto.DepartmentId;
            changes.Add($"Department: {oldDept} → {newDept}");
        }

        if (!changes.Any())
            return ServiceResult<TicketResponseDto>.Failure("No changes detected.");

        // Auto-promote Submitted → Reviewing on assignment
        if (ticket.Status == TicketStatus.Submitted)
            ticket.Status = TicketStatus.Reviewing;

        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChangesAsync();

        _logger.LogInformation("Ticket {TicketId} assigned by Admin {UserId}: {Changes}.",
            ticketId, currentUser.Id, string.Join("; ", changes));

        await AddHistoryAsync(ticketId, currentUser.Id, "Assigned",
            oldValue: null,
            newValue: string.Join("; ", changes));

        if (dto.AssignedToId.HasValue)
        {
            await _publisher.Publish(new DomainEventNotification<TicketAssignedEvent>(
                new TicketAssignedEvent(ticketId, dto.AssignedToId.Value)));
        }

        var updatedTicket = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
        return await BuildResponseAsync(updatedTicket!);
    }

    // ══════════════════════════════════════════════════════════════
    // TRANSFER — UnitUser (in current dept) or Admin
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<TicketResponseDto>> TransferAsync(
        Guid ticketId, TransferTicketDto dto, CurrentUser currentUser)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult<TicketResponseDto>.NotFound("Ticket not found.");

        // ── Authorization ──
        if (currentUser.IsUnitUser || currentUser.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        {
            if (ticket.AssignedDepartmentId != currentUser.DepartmentId)
                return ServiceResult<TicketResponseDto>.Forbidden(
                    "You can only transfer tickets from your own department.");
        }
        else if (!currentUser.IsAdmin && !currentUser.IsOperator)
        {
            if (currentUser.IsRector)
                return ServiceResult<TicketResponseDto>.Forbidden(
                    "Rector role is read-only.");
            return ServiceResult<TicketResponseDto>.Forbidden(
                "Only UnitUser (own dept), Operator or Admin can transfer tickets.");
        }

        var toDept = await _departmentRepository.GetByIdAsync(dto.ToDepartmentId);
        if (toDept is null)
            return ServiceResult<TicketResponseDto>.Failure("Target department not found.", 404);

        var fromDeptId = ticket.AssignedDepartmentId ?? ticket.DepartmentId;

        // Record the transfer
        var transfer = new TicketTransfer
        {
            TicketId = ticket.Id,
            FromDepartmentId = fromDeptId ?? dto.ToDepartmentId,
            ToDepartmentId = dto.ToDepartmentId,
            Note = dto.Note,
            TransferredById = currentUser.Id
        };

        ticket.AssignedDepartmentId = dto.ToDepartmentId;
        ticket.AssignedToId = null;
        ticket.LastTransferredAt = DateTime.UtcNow;
        ticket.InternalStatus = TicketInternalStatus.Unread;

        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChangesAsync();

        // Save transfer record via DbContext (need to add manually since no transfer repo)
        await AddTransferAsync(transfer);

        await AddHistoryAsync(ticketId, currentUser.Id, "Transferred",
            oldValue: fromDeptId?.ToString(),
            newValue: $"→ {toDept.Name}",
            customNote: dto.Note);

        await _systemLogService.LogAsync(
            currentUser.Id.ToString(), "Transfer Ticket", "Ticket",
            ticketId.ToString(), $"Talep Transfer Edildi → {toDept.Name}");

        // Notify new department users
        await _notificationService.NotifyDepartmentUsersAsync(
            dto.ToDepartmentId,
            $"Yeni talep departmanınıza transfer edildi: {ticket.Title}",
            ticketId, "Transfer");

        var updated = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
        return await BuildResponseAsync(updated!);
    }

    // ══════════════════════════════════════════════════════════════
    // REPLY — UnitUser / Staff / Admin
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<TicketReplyResponseDto>> ReplyAsync(
        Guid ticketId, ReplyTicketDto dto, CurrentUser currentUser)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return ServiceResult<TicketReplyResponseDto>.Failure("Reply message cannot be empty.");

        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult<TicketReplyResponseDto>.NotFound("Ticket not found.");

        // ── Authorization ──
        if (currentUser.IsUnitUser || currentUser.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        {
            if (ticket.AssignedDepartmentId != currentUser.DepartmentId)
                return ServiceResult<TicketReplyResponseDto>.Forbidden(
                    "You can only reply to tickets in your department.");
        }

        if (currentUser.IsRector)
            return ServiceResult<TicketReplyResponseDto>.Forbidden("Rector role is read-only and cannot reply to tickets.");

        if (!currentUser.IsStaffOrAdmin && !currentUser.IsUnitUser)
            return ServiceResult<TicketReplyResponseDto>.Forbidden(
                "Only Staff, UnitUser or Admin can reply to tickets.");

        var reply = new TicketReply
        {
            TicketId = ticketId,
            AuthorId = currentUser.Id,
            Message = dto.Message
        };

        var ticketWithDetails = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
        ticketWithDetails!.Replies.Add(reply);
        ticketWithDetails.Status = TicketStatus.Answered;
        _ticketRepository.Update(ticketWithDetails);
        await _ticketRepository.SaveChangesAsync();

        await AddHistoryAsync(ticketId, currentUser.Id, "Status Changed",
            oldValue: ticket.Status.ToString(), newValue: "Answered", customNote: "Talep cevaplandı");

        // Notify ticket owner
        if (ticket.CreatorId.HasValue)
        {
            await _notificationService.CreateAsync(
                ticket.CreatorId.Value,
                $"Talebinize yanıt geldi: {ticket.Title}",
                ticketId, "Reply");
        }

        // Send email to requester (Guest or Creator)
        string? targetEmail = ticket.GuestEmail;
        string applicantName = ticket.GuestName ?? "Değerli Başvuru Sahibimiz";
        
        if (string.IsNullOrEmpty(targetEmail) && ticket.CreatorId.HasValue)
        {
            var creatorUser = await _userService.GetByIdAsync(ticket.CreatorId.Value, includeDeleted: false);
            if (creatorUser != null && !string.IsNullOrEmpty(creatorUser.Email))
            {
                targetEmail = creatorUser.Email;
                applicantName = creatorUser.FullName;
            }
        }

        if (!string.IsNullOrEmpty(targetEmail))
        {
            var frontendUrl = "http://localhost:5173";
            var trackUrl = $"{frontendUrl}/#/track";
            
            var emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                <div style='background-color: #f8fafc; padding: 20px; text-align: center; border-bottom: 1px solid #e2e8f0;'>
                    <h2 style='color: #0f172a; margin: 0;'>RİMER Başvurunuza Yanıt Geldi</h2>
                </div>
                <div style='padding: 30px; background-color: #ffffff;'>
                    <p style='color: #334155; font-size: 16px; margin-top: 0;'>Sayın <strong>{applicantName}</strong>,</p>
                    <p style='color: #334155; font-size: 16px;'>
                        <strong>{ticket.ReferenceNo}</strong> referans numaralı başvurunuza yetkili birim tarafından resmi bir yanıt iletilmiştir.
                    </p>
                    <p style='color: #334155; font-size: 16px;'>
                        Başvurunuza verilen yanıtı okumak ve süreci takip etmek için aşağıdaki adımları izleyebilirsiniz:
                    </p>
                    <ol style='color: #334155; font-size: 15px; margin-bottom: 25px;'>
                        <li>Başvuru sorgulama sayfasına gidin.</li>
                        <li>Başvuru referans numaranızı (<strong>{ticket.ReferenceNo}</strong>) ilgili alana girin.</li>
                        <li>Sisteme kayıtlı e-posta adresinizi girerek durumu sorgulayın.</li>
                    </ol>
                    <div style='text-align: center; margin-bottom: 25px;'>
                        <a href='{trackUrl}' style='display: inline-block; background-color: #3b82f6; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px;'>Başvurumu Sorgula</a>
                    </div>
                </div>
                <div style='background-color: #f8fafc; padding: 20px; text-align: center; border-top: 1px solid #e2e8f0;'>
                    <p style='color: #64748b; font-size: 12px; margin: 0;'>
                        Bu e-posta otomatik olarak gönderilmiştir, lütfen yanıtlamayınız.
                    </p>
                </div>
            </div>";

            await _emailService.SendEmailAsync(
                targetEmail,
                $"RİMER Başvurunuza Yanıt Geldi (#{ticket.ReferenceNo})",
                emailBody);
        }

        var author = await _userService.GetByIdAsync(currentUser.Id, includeDeleted: false);
        var replyDto = _mapper.Map<TicketReplyResponseDto>(reply);
        replyDto.AuthorName = author?.FullName ?? "Unknown";

        return ServiceResult<TicketReplyResponseDto>.Success(replyDto);
    }

    // ══════════════════════════════════════════════════════════════
    // CLOSE — UnitUser (own dept) / Staff / Admin
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<TicketResponseDto>> CloseAsync(
        Guid ticketId, CurrentUser currentUser)
    {
        var ticket = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
        if (ticket is null)
            return ServiceResult<TicketResponseDto>.NotFound("Ticket not found.");

        if (ticket.Status == TicketStatus.Closed)
            return ServiceResult<TicketResponseDto>.Failure("Ticket is already closed.");

        if (ticket.Replies == null || !ticket.Replies.Any())
            return ServiceResult<TicketResponseDto>.Failure("Talep cevaplanmadan kapatılamaz.");

        // ── Authorization ──
        if (currentUser.IsUnitUser || currentUser.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        {
            if (ticket.AssignedDepartmentId != currentUser.DepartmentId)
                return ServiceResult<TicketResponseDto>.Forbidden(
                    "You can only close tickets in your department.");
        }

        if (currentUser.IsRector)
            return ServiceResult<TicketResponseDto>.Forbidden("Rector role is read-only and cannot close tickets.");

        if (!currentUser.IsStaffOrAdmin && !currentUser.IsUnitUser)
            return ServiceResult<TicketResponseDto>.Forbidden(
                "Only Staff, UnitUser or Admin can close tickets.");

        ticket.Status = TicketStatus.Closed;
        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChangesAsync();

        await AddHistoryAsync(ticketId, currentUser.Id, "Closed",
            oldValue: ticket.Status.ToString(), newValue: "Closed");

        // Notify ticket owner via internal notification
        if (ticket.CreatorId.HasValue)
        {
            await _notificationService.CreateAsync(
                ticket.CreatorId.Value,
                $"Talebiniz kapatıldı: {ticket.Title}",
                ticketId, "Close");
        }

        // Send email to requester (Guest or Creator)
        string? targetEmail = ticket.GuestEmail;
        string applicantName = ticket.GuestName ?? "Değerli Başvuru Sahibimiz";
        
        if (string.IsNullOrEmpty(targetEmail) && ticket.CreatorId.HasValue)
        {
            var creator = await _userService.GetByIdAsync(ticket.CreatorId.Value, includeDeleted: false);
            if (creator != null && !string.IsNullOrEmpty(creator.Email))
            {
                targetEmail = creator.Email;
                applicantName = creator.FullName;
            }
        }

        if (!string.IsNullOrEmpty(targetEmail))
        {
            var frontendUrl = "http://localhost:5173";
            var trackUrl = $"{frontendUrl}/#/track";
            
            var emailBody = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                <div style='background-color: #f8fafc; padding: 20px; text-align: center; border-bottom: 1px solid #e2e8f0;'>
                    <h2 style='color: #0f172a; margin: 0;'>RİMER Başvuru Sonucu</h2>
                </div>
                <div style='padding: 30px; background-color: #ffffff;'>
                    <p style='color: #334155; font-size: 16px; margin-top: 0;'>Sayın <strong>{applicantName}</strong>,</p>
                    <p style='color: #334155; font-size: 16px;'>
                        <strong>{ticket.ReferenceNo}</strong> referans numaralı başvurunuz değerlendirilmiş ve sonucunda kapatılmıştır.
                    </p>
                    <p style='color: #334155; font-size: 16px;'>
                        Başvurunuza verilen yanıtı ve işlem geçmişini okumak için aşağıdaki adımları izleyebilirsiniz:
                    </p>
                    <ol style='color: #334155; font-size: 15px; margin-bottom: 25px;'>
                        <li>Başvuru sorgulama sayfasına gidin.</li>
                        <li>Başvuru referans numaranızı (<strong>{ticket.ReferenceNo}</strong>) ilgili alana girin.</li>
                        <li>Sisteme kayıtlı e-posta adresinizi girerek durumu sorgulayın.</li>
                    </ol>
                    <div style='text-align: center; margin-bottom: 25px;'>
                        <a href='{trackUrl}' style='display: inline-block; background-color: #3b82f6; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px;'>Başvurumu Sorgula</a>
                    </div>
                </div>
                <div style='background-color: #f8fafc; padding: 20px; text-align: center; border-top: 1px solid #e2e8f0;'>
                    <p style='color: #64748b; font-size: 12px; margin: 0;'>
                        Bu e-posta otomatik olarak gönderilmiştir, lütfen yanıtlamayınız.
                    </p>
                </div>
            </div>";

            await _emailService.SendEmailAsync(
                targetEmail,
                $"RİMER Başvurunuz Sonuçlandı (#{ticket.ReferenceNo})",
                emailBody);
        }

        await _publisher.Publish(new DomainEventNotification<TicketClosedEvent>(
            new TicketClosedEvent(ticketId, currentUser.Id, ticket.CreatorId)));

        var updated = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
        return await BuildResponseAsync(updated!);
    }

    // ══════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════

    /// <summary>Add a history entry for a ticket.</summary>
    private async Task AddHistoryAsync(
        Guid ticketId, Guid changedById, string action, string? oldValue, string? newValue, string? customNote = null)
    {
        var ticketWithHistories = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
        var departmentName = ticketWithHistories?.AssignedDepartment?.Name;

        var history = new TicketHistory
        {
            TicketId = ticketId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            DepartmentName = departmentName,
            Note = customNote,
            ChangedById = changedById,
            ChangedAt = DateTime.UtcNow
        };

        ticketWithHistories!.Histories.Add(history);
        _ticketRepository.Update(ticketWithHistories);
        await _ticketRepository.SaveChangesAsync();
    }

    /// <summary>Add a transfer record for a ticket.</summary>
    private async Task AddTransferAsync(TicketTransfer transfer)
    {
        var ticketWithDetails = await _ticketRepository.GetByIdWithDetailsAsync(transfer.TicketId);
        ticketWithDetails!.Transfers.Add(transfer);
        _ticketRepository.Update(ticketWithDetails);
        await _ticketRepository.SaveChangesAsync();
    }

    /// <summary>Build a response DTO (without histories) with resolved user info.</summary>
    private async Task<ServiceResult<TicketResponseDto>> BuildResponseAsync(Ticket ticket)
    {
        var dto = _mapper.Map<TicketResponseDto>(ticket);

        if (ticket.CreatorId.HasValue)
            dto.Creator = await _userService.GetByIdAsync(ticket.CreatorId.Value, includeDeleted: true);

        if (ticket.AssignedToId.HasValue)
            dto.AssignedTo = await _userService.GetByIdAsync(ticket.AssignedToId.Value, includeDeleted: true);

        return ServiceResult<TicketResponseDto>.Success(dto);
    }

    /// <summary>Build a detail response DTO (with histories) with resolved user info.</summary>
    private async Task<ServiceResult<TicketResponseDto>> BuildDetailResponseAsync(Ticket ticket)
    {
        var dto = _mapper.Map<TicketResponseDto>(ticket);

        if (ticket.CreatorId.HasValue)
            dto.Creator = await _userService.GetByIdAsync(ticket.CreatorId.Value, includeDeleted: true);

        if (ticket.AssignedToId.HasValue)
            dto.AssignedTo = await _userService.GetByIdAsync(ticket.AssignedToId.Value, includeDeleted: true);

        if (ticket.Histories.Any())
        {
            var historyUserIds = ticket.Histories
                .Select(h => h.ChangedById)
                .Distinct();

            var historyUsersMap = await _userService.GetByIdsAsync(historyUserIds, includeDeleted: true);

            dto.Histories = ticket.Histories
                .OrderByDescending(h => h.ChangedAt)
                .Select(h =>
                {
                    var historyDto = _mapper.Map<TicketHistoryResponseDto>(h);
                    historyDto.ChangedBy = historyUsersMap.GetValueOrDefault(h.ChangedById);
                    return historyDto;
                })
                .ToList();
        }

        if (ticket.Replies != null && ticket.Replies.Any())
        {
            var replyAuthorIds = ticket.Replies.Select(r => r.AuthorId).Distinct();
            var replyAuthorsMap = await _userService.GetByIdsAsync(replyAuthorIds, includeDeleted: true);

            dto.Replies = ticket.Replies
                .OrderBy(r => r.CreatedAt)
                .Select(r => new TicketReplyResponseDto
                {
                    Id = r.Id,
                    AuthorId = r.AuthorId,
                    AuthorName = replyAuthorsMap.TryGetValue(r.AuthorId, out var u) ? u?.FullName ?? "Bilinmiyor" : "Bilinmiyor",
                    Message = r.Message,
                    CreatedAt = r.CreatedAt
                })
                .ToList();
        }

        return ServiceResult<TicketResponseDto>.Success(dto);
    }

    // ── Status Transition Rules ────────────────────────────────────

    /// <summary>
    /// Allowed transitions:
    ///   Submitted         → Reviewing, Closed
    ///   Reviewing         → WaitingDepartment, Answered, Closed
    ///   WaitingDepartment → Reviewing, Answered, Closed
    ///   Answered          → Closed
    ///   Closed            → (none)
    /// </summary>
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new()
    {
        [TicketStatus.Submitted] = [TicketStatus.Reviewing, TicketStatus.Closed],
        [TicketStatus.Reviewing] = [TicketStatus.WaitingDepartment, TicketStatus.Answered, TicketStatus.Closed],
        [TicketStatus.WaitingDepartment] = [TicketStatus.Reviewing, TicketStatus.Answered, TicketStatus.Closed],
        [TicketStatus.Answered] = [TicketStatus.Closed],
        [TicketStatus.Closed] = []
    };

    private static bool IsValidTransition(TicketStatus from, TicketStatus to)
    {
        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    private static string GetTransitionHint(TicketStatus currentStatus)
    {
        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowed) || allowed.Length == 0)
            return "Closed tickets cannot be transitioned.";

        return $"Allowed transitions from {currentStatus}: {string.Join(", ", allowed.Select(s => s.ToString()))}.";
    }

    // ══════════════════════════════════════════════════════════════
    // DEPARTMENT WORKFLOW METHODS
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<TicketResponseDto>> TakeOwnershipAsync(Guid ticketId, CurrentUser currentUser)
    {
        if (!currentUser.IsStaffOrAdmin && !currentUser.IsUnitUser)
            return ServiceResult<TicketResponseDto>.Forbidden("Only Staff or UnitUser can take ownership.");

        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult<TicketResponseDto>.NotFound("Ticket not found.");

        if ((currentUser.IsUnitUser || currentUser.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase)) 
             && ticket.AssignedDepartmentId != currentUser.DepartmentId)
            return ServiceResult<TicketResponseDto>.Forbidden("You can only take ownership of tickets in your department.");

        var oldAssigneeId = ticket.AssignedToId;
        ticket.AssignedToId = currentUser.Id;

        var oldStatus = ticket.Status;
        if (ticket.Status == TicketStatus.Submitted || ticket.Status == TicketStatus.WaitingDepartment)
        {
            ticket.Status = TicketStatus.Reviewing;
        }

        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChangesAsync();

        await AddHistoryAsync(ticketId, currentUser.Id, "TakenOwnership", 
            oldValue: oldAssigneeId?.ToString(), newValue: currentUser.Id.ToString());

        if (oldStatus != ticket.Status)
        {
            await AddHistoryAsync(ticketId, currentUser.Id, "Status Changed", 
                oldValue: oldStatus.ToString(), newValue: ticket.Status.ToString(), customNote: "Talep işleme alındı");
        }

        var updated = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
        return await BuildResponseAsync(updated!);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<TicketResponseDto>> UpdateInternalStatusAsync(Guid ticketId, int internalStatus, CurrentUser currentUser)
    {
        if (!currentUser.IsStaffOrAdmin && !currentUser.IsUnitUser)
            return ServiceResult<TicketResponseDto>.Forbidden("Permission denied.");

        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult<TicketResponseDto>.NotFound("Ticket not found.");

        if ((currentUser.IsUnitUser || currentUser.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase)) 
             && ticket.AssignedDepartmentId != currentUser.DepartmentId)
            return ServiceResult<TicketResponseDto>.Forbidden("Permission denied.");

        var oldStatus = ticket.InternalStatus;
        var newStatus = (TicketInternalStatus)internalStatus;

        if (oldStatus == newStatus)
            return ServiceResult<TicketResponseDto>.Failure("Ticket already has this internal status.");

        ticket.InternalStatus = newStatus;
        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChangesAsync();

        await AddHistoryAsync(ticketId, currentUser.Id, "InternalStatusChanged", 
            oldValue: oldStatus.ToString(), newValue: newStatus.ToString());

        var updated = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
        return await BuildResponseAsync(updated!);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<TicketResponseDto>> UpdatePriorityAsync(Guid ticketId, int priority, CurrentUser currentUser)
    {
        if (!currentUser.IsStaffOrAdmin && !currentUser.IsUnitUser)
            return ServiceResult<TicketResponseDto>.Forbidden("Permission denied.");

        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult<TicketResponseDto>.NotFound("Ticket not found.");

        if ((currentUser.IsUnitUser || currentUser.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase)) 
             && ticket.AssignedDepartmentId != currentUser.DepartmentId)
            return ServiceResult<TicketResponseDto>.Forbidden("Permission denied.");

        var oldPriority = ticket.Priority;
        var newPriority = (TicketPriority)priority;

        if (oldPriority == newPriority)
            return ServiceResult<TicketResponseDto>.Failure("Ticket already has this priority.");

        ticket.Priority = newPriority;
        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChangesAsync();

        await AddHistoryAsync(ticketId, currentUser.Id, "PriorityChanged", 
            oldValue: oldPriority.ToString(), newValue: newPriority.ToString());

        var updated = await _ticketRepository.GetByIdWithDetailsAsync(ticketId);
        return await BuildResponseAsync(updated!);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> CreateReminderAsync(Guid ticketId, CreateTicketReminderDto dto, CurrentUser currentUser)
    {
        if (!currentUser.IsStaffOrAdmin && !currentUser.IsUnitUser)
            return ServiceResult<bool>.Forbidden("Permission denied.");

        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult<bool>.NotFound("Ticket not found.");

        if ((currentUser.IsUnitUser || currentUser.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase)) 
             && ticket.AssignedDepartmentId != currentUser.DepartmentId)
            return ServiceResult<bool>.Forbidden("You can only create reminders for tickets in your department.");

        var reminder = new TicketReminder
        {
            TicketId = ticketId,
            UserId = currentUser.Id,
            ReminderAt = dto.ReminderAt,
            Note = dto.Note,
            IsDismissed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _reminderRepository.AddAsync(reminder);
        await _reminderRepository.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> DismissReminderAsync(int reminderId, CurrentUser currentUser)
    {
        var reminders = await _reminderRepository.FindAsync(r => r.Id == reminderId);
        var reminder = reminders.FirstOrDefault();
        if (reminder is null)
            return ServiceResult<bool>.NotFound("Reminder not found.");

        if (reminder.UserId != currentUser.Id)
            return ServiceResult<bool>.Forbidden("You can only dismiss your own reminders.");

        reminder.IsDismissed = true;
        reminder.UpdatedAt = DateTime.UtcNow;

        _reminderRepository.Update(reminder);
        await _reminderRepository.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> SnoozeReminderAsync(int reminderId, DateTime snoozeUntil, CurrentUser currentUser)
    {
        var reminders = await _reminderRepository.FindAsync(r => r.Id == reminderId);
        var reminder = reminders.FirstOrDefault();
        if (reminder is null)
            return ServiceResult<bool>.NotFound("Reminder not found.");

        if (reminder.UserId != currentUser.Id)
            return ServiceResult<bool>.Forbidden("You can only snooze your own reminders.");

        reminder.ReminderAt = snoozeUntil;
        reminder.IsDismissed = false;
        reminder.UpdatedAt = DateTime.UtcNow;

        _reminderRepository.Update(reminder);
        await _reminderRepository.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<List<TicketReminderDto>>> GetActiveRemindersAsync(CurrentUser currentUser)
    {
        var now = DateTime.UtcNow;
        var reminders = await _reminderRepository.FindAsync(r => r.UserId == currentUser.Id && !r.IsDismissed);

        // Preload tickets for title/reference
        var ticketIds = reminders.Select(r => r.TicketId).Distinct().ToList();
        var ticketsEnum = await _ticketRepository.FindAsync(t => ticketIds.Contains(t.Id));
        var tickets = ticketsEnum.ToDictionary(t => t.Id);

        var dtos = reminders.Select(r => new TicketReminderDto
        {
            Id = r.Id,
            TicketId = r.TicketId,
            TicketTitle = tickets.GetValueOrDefault(r.TicketId)?.Title ?? "Unknown Ticket",
            TicketReferenceNo = tickets.GetValueOrDefault(r.TicketId)?.ReferenceNo ?? "N/A",
            ReminderAt = r.ReminderAt,
            Note = r.Note,
            IsDismissed = r.IsDismissed,
            CreatedAt = r.CreatedAt
        }).OrderBy(r => r.ReminderAt).ToList();

        return ServiceResult<List<TicketReminderDto>>.Success(dtos);
    }

    // ══════════════════════════════════════════════════════════════
    // ANALYTICS — chartsrole only
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<ChartsResponseDto>> GetChartsAsync(DateTime? startDate, DateTime? endDate, CurrentUser currentUser)
    {
        // 1. Authorization
        if (currentUser.Role?.ToLower() != "chartsrole" && !currentUser.IsAdmin && !currentUser.IsRector)
            return ServiceResult<ChartsResponseDto>.Forbidden("Only chartsrole, Rector or Admin can access analytics.");

        // 2. Fetch tickets (with date filter)
        var ticketsEnum = await _ticketRepository.FindAsync(t =>
            (!startDate.HasValue || t.CreatedAt >= startDate.Value) &&
            (!endDate.HasValue || t.CreatedAt <= endDate.Value)
        );
        var tickets = ticketsEnum.ToList();

        // 3. Fetch all departments for name resolution
        var allDepartments = await _departmentRepository.GetAllAsync();
        var deptMap = allDepartments.ToDictionary(d => d.Id, d => d.Name);

        var utcNow = DateTime.UtcNow;

        // 4. Build result
        var result = new ChartsResponseDto
        {
            TotalTickets = tickets.Count,

            DateFiltered = new DateFilterDto { Start = startDate, End = endDate },

            // Category breakdown
            ByCategory = new List<CategoryCountDto>
            {
                new() { Name = "complaint",  Count = tickets.Count(t => t.Category == TicketCategory.Complaint) },
                new() { Name = "suggestion", Count = tickets.Count(t => t.Category == TicketCategory.Suggestion) },
                new() { Name = "request",    Count = tickets.Count(t => t.Category == TicketCategory.Request) },
                new() { Name = "thanks",     Count = tickets.Count(t => t.Category == TicketCategory.Thanks) },
                new() { Name = "info",       Count = tickets.Count(t => t.Category == TicketCategory.InfoRequest || t.Category == TicketCategory.Question) }
            },

            // Department ranking
            ByDepartment = tickets
                .Where(t => t.DepartmentId.HasValue)
                .GroupBy(t => t.DepartmentId!.Value)
                .Select(g => new DepartmentCountDto
                {
                    Name = deptMap.GetValueOrDefault(g.Key, "Unknown"),
                    Count = g.Count()
                })
                .OrderByDescending(d => d.Count)
                .ToList(),

            // Status summary (unanswered = Submitted AND older than 45 days)
            StatusSummary = new StatusSummaryDto
            {
                Resolved = tickets.Count(t => t.Status == TicketStatus.Closed),
                Pending = tickets.Count(t => t.Status == TicketStatus.Reviewing || t.Status == TicketStatus.WaitingDepartment),
                Unanswered = tickets.Count(t => t.Status == TicketStatus.Submitted && (utcNow - t.CreatedAt).TotalDays > 45)
            },

            // Aging buckets (only non-closed tickets)
            Aging = new AgingDto
            {
                Over45  = tickets.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays >= 45 && (utcNow - t.CreatedAt).TotalDays < 60),
                Over60  = tickets.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays >= 60 && (utcNow - t.CreatedAt).TotalDays < 90),
                Over90  = tickets.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays >= 90 && (utcNow - t.CreatedAt).TotalDays < 180),
                Over180 = tickets.Count(t => t.Status != TicketStatus.Closed && (utcNow - t.CreatedAt).TotalDays >= 180)
            }
        };

        return ServiceResult<ChartsResponseDto>.Success(result);
    }

    // ══════════════════════════════════════════════════════════════
    // DASHBOARD STATS
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<DepartmentStatsDto>> GetMyStatsAsync(CurrentUser currentUser)
    {
        // Fetch all non-deleted tickets first. 
        // In a very large app we might need specialized repository methods for stats.
        var allTickets = await _ticketRepository.FindAsync(t => !t.IsDeleted);

        IEnumerable<Ticket> query = allTickets;

        // Filter based on user role (similar to GetListAsync logic)
        if (currentUser.Role.Equals("Student", StringComparison.OrdinalIgnoreCase) || 
            currentUser.Role.Equals("Citizen", StringComparison.OrdinalIgnoreCase) || 
            currentUser.Role.Equals("Academician", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.CreatorId == currentUser.Id);
        }
        else if (currentUser.IsUnitUser || currentUser.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t => t.AssignedDepartmentId == currentUser.DepartmentId);
        }
        else if (currentUser.IsRector)
        {
            // Rector sees all, no filter needed
        }
        else if (currentUser.IsAdmin)
        {
            // Admin sees all, no filter needed
        }

        var tickets = query.ToList();
        var now = DateTime.UtcNow;

        var stats = new DepartmentStatsDto
        {
            Total = tickets.Count,
            NewTickets = tickets.Count(t => t.Status == TicketStatus.Submitted || t.Status == TicketStatus.WaitingDepartment),
            Reviewing = tickets.Count(t => t.AssignedToId != null && t.Status != TicketStatus.Closed),
            MyAssigned = tickets.Count(t => t.AssignedToId == currentUser.Id && t.Status != TicketStatus.Closed),
            Critical = tickets.Count(t => t.Priority == TicketPriority.Critical && t.Status != TicketStatus.Closed),
            Delayed45 = tickets.Count(t => t.Status != TicketStatus.Closed && (now - t.CreatedAt).TotalDays >= 45)
        };

        return ServiceResult<DepartmentStatsDto>.Success(stats);
    }

    // ══════════════════════════════════════════════════════════════
    // DELETE — Admin only
    // ══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> DeleteAsync(Guid ticketId, CurrentUser currentUser)
    {
        // 1. Authorization
        if (!currentUser.IsAdmin)
            return ServiceResult<bool>.Forbidden("Only Admin can delete tickets.");

        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket is null)
            return ServiceResult<bool>.NotFound("Ticket not found.");

        ticket.IsDeleted = true;
        ticket.DeletedAt = DateTime.UtcNow;
        ticket.IsActive = false;

        _ticketRepository.Update(ticket);
        await _ticketRepository.SaveChangesAsync();

        await _systemLogService.LogAsync(
            currentUser.Id.ToString(), 
            "Delete Ticket", 
            "Ticket", 
            ticketId.ToString(),
            $"Talep Silindi: {ticket.Title}"
        );

        _logger.LogInformation("Ticket {TicketId} deleted by Admin {UserId}.", ticketId, currentUser.Id);

        return ServiceResult<bool>.Success(true);
    }
}
