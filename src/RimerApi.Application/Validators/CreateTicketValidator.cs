using FluentValidation;
using RimerApi.Application.DTOs.Ticket;
using RimerApi.Domain.Enums;

namespace RimerApi.Application.Validators;

/// <summary>
/// Validates CreateTicketDto input.
/// </summary>
public class CreateTicketValidator : AbstractValidator<CreateTicketDto>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(4000).WithMessage("Description must not exceed 4000 characters.");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid ticket category.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 3).WithMessage("Priority must be between 1 (Low) and 3 (High).");
    }
}
