using CapTap.Application.DTOs.Medication;
using FluentValidation;

namespace CapTap.Application.Validators;

public sealed class CreateMedicationRequestValidator : AbstractValidator<CreateMedicationRequest>
{
    public CreateMedicationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.GenericName)
            .MaximumLength(255)
            .When(x => x.GenericName is not null);

        RuleFor(x => x.BrandName)
            .MaximumLength(255)
            .When(x => x.BrandName is not null);

        RuleFor(x => x.FdaIdentifier)
            .MaximumLength(100)
            .When(x => x.FdaIdentifier is not null);

        RuleFor(x => x.DosageAmount)
            .GreaterThan(0);

        RuleFor(x => x.DosageUnit)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Form)
            .MaximumLength(50)
            .When(x => x.Form is not null);

        RuleFor(x => x.Instructions)
            .MaximumLength(2000)
            .When(x => x.Instructions is not null);
    }
}
