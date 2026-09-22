using AutoMapper;
using RimerApi.Application.DTOs.Ticket;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Enums;

namespace RimerApi.Application.Mappings;

/// <summary>
/// AutoMapper profile for mapping between Domain entities and Application DTOs.
/// Note: User-related fields (Creator, AssignedTo, ChangedBy) are NOT mapped here —
/// they are resolved manually via IUserService because they live outside the Domain model.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ── Ticket → TicketResponseDto ─────────────────────────────
        CreateMap<Ticket, TicketResponseDto>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.InternalStatus, opt => opt.MapFrom(src => src.InternalStatus.ToString()))
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src =>
                src.Department != null ? src.Department.Name : null))
            .ForMember(dest => dest.AssignedDepartmentName, opt => opt.MapFrom(src =>
                src.AssignedDepartment != null ? src.AssignedDepartment.Name : null))
            // User fields are resolved manually in TicketService — not via AutoMapper
            .ForMember(dest => dest.Creator, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedTo, opt => opt.Ignore())
            .ForMember(dest => dest.Histories, opt => opt.Ignore())
            .ForMember(dest => dest.Transfers, opt => opt.Ignore())
            .ForMember(dest => dest.Replies, opt => opt.Ignore());

        // ── CreateTicketDto → Ticket ───────────────────────────────
        CreateMap<CreateTicketDto, Ticket>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => TicketStatus.Submitted))
            .ForMember(dest => dest.CreatorId, opt => opt.Ignore())   // Set in service
            .ForMember(dest => dest.AssignedToId, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedDepartmentId, opt => opt.Ignore())  // Set in service
            .ForMember(dest => dest.LastTransferredAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())   // Set by DbContext
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Department, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedDepartment, opt => opt.Ignore())
            .ForMember(dest => dest.Histories, opt => opt.Ignore())
            .ForMember(dest => dest.Transfers, opt => opt.Ignore())
            .ForMember(dest => dest.Replies, opt => opt.Ignore());

        // ── TicketHistory → TicketHistoryResponseDto ───────────────
        CreateMap<TicketHistory, TicketHistoryResponseDto>()
            // ChangedBy is resolved manually via IUserService
            .ForMember(dest => dest.ChangedBy, opt => opt.Ignore());

        // ── TicketTransfer → TicketTransferResponseDto ─────────────
        CreateMap<TicketTransfer, TicketTransferResponseDto>()
            .ForMember(dest => dest.FromDepartmentName, opt => opt.MapFrom(src =>
                src.FromDepartment != null ? src.FromDepartment.Name : "Unknown"))
            .ForMember(dest => dest.ToDepartmentName, opt => opt.MapFrom(src =>
                src.ToDepartment != null ? src.ToDepartment.Name : "Unknown"));

        // ── TicketReply → TicketReplyResponseDto ───────────────────
        CreateMap<TicketReply, TicketReplyResponseDto>()
            .ForMember(dest => dest.AuthorName, opt => opt.Ignore()); // Resolved manually
    }
}

