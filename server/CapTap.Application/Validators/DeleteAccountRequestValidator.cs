using CapTap.Application.DTOs.Users;
using FluentValidation;

namespace CapTap.Application.Validators;

public sealed class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required to delete your account.");
    }
}
