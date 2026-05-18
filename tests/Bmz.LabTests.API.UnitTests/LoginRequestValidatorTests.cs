using Bmz.LabTests.API.Validators;
using Bmz.LabTests.Application.Auth;
using FluentValidation.TestHelper;

namespace Bmz.LabTests.API.UnitTests;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Username_ShouldBeRequired()
    {
        var model = new LoginRequest { Username = "", Password = "p" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Username_ShouldHaveMaxLength128()
    {
        var model = new LoginRequest { Username = new string('a', 129), Password = "p" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Password_ShouldBeRequired()
    {
        var model = new LoginRequest { Username = "u", Password = "" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Password_ShouldHaveMaxLength256()
    {
        var model = new LoginRequest { Username = "u", Password = new string('a', 257) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ValidRequest_ShouldPass()
    {
        var model = new LoginRequest { Username = "user", Password = "pass" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

