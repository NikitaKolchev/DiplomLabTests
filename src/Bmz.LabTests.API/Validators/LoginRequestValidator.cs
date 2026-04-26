using Bmz.LabTests.Application.Auth;
using FluentValidation;

namespace Bmz.LabTests.API.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(256);
    }
}
