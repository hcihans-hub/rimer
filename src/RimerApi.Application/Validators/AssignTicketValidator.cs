using FluentValidation;
using RimerApi.Application.DTOs.Ticket;

namespace RimerApi.Application.Validators;

/// <summary>
/// Validates AssignTicketDto input.
/// </summary>
public class AssignTicketValidator : AbstractValidator<AssignTicketDto>
{
    public AssignTicketValidator()
    {
        RuleFor(x => x)
            .Must(x => x.AssignedToId.HasValue || x.DepartmentId.HasValue)
            .WithMessage("At least one of AssignedToId or DepartmentId must be provided.");

        RuleFor(x => x.AssignedToId)
            .NotEqual(Guid.Empty).WithMessage("AssignedToId cannot be an empty GUID.")
            .When(x => x.AssignedToId.HasValue);

        RuleFor(x => x.DepartmentId)
            .NotEqual(Guid.Empty).WithMessage("DepartmentId cannot be an empty GUID.")
            .When(x => x.DepartmentId.HasValue);
    }
}
