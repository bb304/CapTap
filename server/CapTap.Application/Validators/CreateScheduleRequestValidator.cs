using CapTap.Application.DTOs.Schedule;
using FluentValidation;

namespace CapTap.Application.Validators;

public sealed class CreateScheduleRequestValidator : AbstractValidator<CreateScheduleRequest>
{
    public CreateScheduleRequestValidator()
    {
        RuleFor(x => x.Frequency)
            .IsInEnum();

        RuleFor(x => x.DoseQuantity)
            .GreaterThan(0);

        RuleFor(x => x.ScheduledTime)
            .Must(time => time is { Hour: >= 0 and <= 23 })
            .WithMessage("Scheduled time must be between 00:00 and 23:59.");

        RuleFor(x => x)
            .Must(x => !x.EffectiveTo.HasValue ||
                       (x.EffectiveFrom ?? DateTime.UtcNow.Date) <= x.EffectiveTo.Value)
            .WithMessage("EffectiveTo must be on or after EffectiveFrom.");
    }
}
