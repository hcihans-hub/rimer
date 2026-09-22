using FluentValidation;
using RimerApi.Application.DTOs.Ticket;

namespace RimerApi.Application.Validators;

/// <summary>
/// Validates UpdateTicketStatusDto input.
/// </summary>
public class UpdateTicketStatusValidator : AbstractValidator<UpdateTicketStatusDto>
{
    public UpdateTicketStatusValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid ticket status.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Note must not exceed 1000 characters.")
            .When(x => x.Note is not null);
    }
}
