using Bmz.LabTests.API.Contracts.Parameters;
using Bmz.LabTests.Domain.Enums;
using FluentValidation;

namespace Bmz.LabTests.API.Validators.Parameters;

public sealed class UpsertParameterRequestValidator : AbstractValidator<UpsertParameterRequest>
{
    public UpsertParameterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.DataType).IsInEnum();
        RuleFor(x => x.Unit).MaximumLength(16);
        RuleFor(x => x.Unit)
            .NotEmpty()
            .When(x => x.DataType == ParameterDataType.Number)
            .WithMessage("Для числовых параметров требуется единица измерения.");
    }
}
