using Bmz.LabTests.API.Contracts.Countries;
using FluentValidation;

namespace Bmz.LabTests.API.Validators.Countries;

public sealed class UpsertCountryRequestValidator : AbstractValidator<UpsertCountryRequest>
{
    public UpsertCountryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
