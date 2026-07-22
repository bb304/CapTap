using CapTap.Application.DTOs.Schedule;
using FluentValidation;

namespace CapTap.Application.Validators;

public sealed class UpdateScheduleRequestValidator : AbstractValidator<UpdateScheduleRequest>
{
    public UpdateScheduleRequestValidator()
    {
        RuleFor(x => x.Frequency)
            .IsInEnum()
            .When(x => x.Frequency.HasValue);

        RuleFor(x => x.DoseQuantity)
            .GreaterThan(0)
            .When(x => x.DoseQuantity.HasValue);

        RuleFor(x => x.ScheduledTime)
            .Must(time => time is null || time.Value is { Hour: >= 0 and <= 23 })
            .WithMessage("Scheduled time must be between 00:00 and 23:59.");

        RuleFor(x => x)
            .Must(x =>
            {
                if (!x.EffectiveFrom.HasValue || !x.EffectiveTo.HasValue)
                {
                    return true;
                }

                return x.EffectiveFrom.Value <= x.EffectiveTo.Value;
            })
            .WithMessage("EffectiveTo must be on or after EffectiveFrom.");
    }
}
