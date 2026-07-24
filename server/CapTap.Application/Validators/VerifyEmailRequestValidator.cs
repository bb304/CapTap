using CapTap.Application.DTOs.Auth;
using FluentValidation;

namespace CapTap.Application.Validators;

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
    }
}
