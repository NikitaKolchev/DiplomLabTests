using Bmz.LabTests.API.Contracts.WireCodes;
using FluentValidation;

namespace Bmz.LabTests.API.Validators.WireCodes;

public sealed class UpsertWireCodeRequestValidator : AbstractValidator<UpsertWireCodeRequest>
{
    public UpsertWireCodeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Marking).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Diameter).GreaterThan(0);
    }
}
