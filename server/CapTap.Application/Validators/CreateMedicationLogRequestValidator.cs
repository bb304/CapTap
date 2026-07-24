using CapTap.Application.DTOs.Logging;
using CapTap.Domain.Enums;
using FluentValidation;

namespace CapTap.Application.Validators;

public sealed class CreateMedicationLogRequestValidator : AbstractValidator<CreateMedicationLogRequest>
{
    public CreateMedicationLogRequestValidator()
    {
        RuleFor(x => x.MedicationId)
            .NotEmpty()
            .WithMessage("Medication is required.");

        RuleFor(x => x.ScheduledDoseTime)
            .NotEmpty()
            .WithMessage("Scheduled dose time is required.");

        RuleFor(x => x.LoggingMethod)
            .IsInEnum()
            .WithMessage("Logging method must be Manual or Nfc.");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes is not null);
    }
}
