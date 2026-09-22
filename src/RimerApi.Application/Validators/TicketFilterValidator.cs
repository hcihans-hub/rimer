using FluentValidation;
using RimerApi.Application.DTOs.Ticket;

namespace RimerApi.Application.Validators;

/// <summary>
/// Validates TicketFilterDto input for list queries.
/// </summary>
public class TicketFilterValidator : AbstractValidator<TicketFilterDto>
{
    public TicketFilterValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("PageNumber must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid ticket status.")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid ticket category.")
            .When(x => x.Category.HasValue);
    }
}
