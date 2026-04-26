using Bmz.LabTests.API.Contracts.Limits;
using FluentValidation;

namespace Bmz.LabTests.API.Validators.Limits;

public sealed class UpsertWireCodeLimitsRequestValidator : AbstractValidator<UpsertWireCodeLimitsRequest>
{
    public UpsertWireCodeLimitsRequestValidator()
    {
        RuleFor(x => x.Items).NotNull();
        RuleFor(x => x.Items).Must(items => items.Count > 0).WithMessage("Требуется хотя бы один параметр.");
        RuleForEach(x => x.Items).SetValidator(new WireCodeLimitItemRequestValidator());
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.ParameterId).Distinct().Count() == items.Count)
            .WithMessage("Значения идентификаторов параметров должны быть уникальными.");
    }
}

public sealed class WireCodeLimitItemRequestValidator : AbstractValidator<WireCodeLimitItemRequest>
{
    public WireCodeLimitItemRequestValidator()
    {
        RuleFor(x => x.ParameterId).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => !x.MinValue.HasValue || !x.MaxValue.HasValue || x.MinValue <= x.MaxValue)
            .WithMessage("Минимальное значение должно быть меньше или равно максимальному значению.");
    }
}
