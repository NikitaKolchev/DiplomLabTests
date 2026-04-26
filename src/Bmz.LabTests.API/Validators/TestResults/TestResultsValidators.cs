using Bmz.LabTests.API.Contracts.TestResults;
using FluentValidation;

namespace Bmz.LabTests.API.Validators.TestResults;

public sealed class CreateTestResultRequestValidator : AbstractValidator<CreateTestResultRequest>
{
    public CreateTestResultRequestValidator()
    {
        RuleFor(x => x.WireCodeId).GreaterThan(0);
        RuleFor(x => x.BatchNumber).NotEmpty().MaximumLength(128);
    }
}

public sealed class SaveTestValuesRequestValidator : AbstractValidator<SaveTestValuesRequest>
{
    public SaveTestValuesRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.Values).NotNull();
        RuleFor(x => x.Values).Must(v => v.Count > 0).WithMessage("Требуется хотя бы одно значение.");
        RuleForEach(x => x.Values).SetValidator(new TestValueItemRequestValidator());
        RuleFor(x => x.Values)
            .Must(v => v.Select(i => i.ParameterId).Distinct().Count() == v.Count)
            .WithMessage("Значения идентификаторов параметров должны быть уникальными.");
    }
}

public sealed class TestValueItemRequestValidator : AbstractValidator<TestValueItemRequest>
{
    public TestValueItemRequestValidator()
    {
        RuleFor(x => x.ParameterId).GreaterThan(0);
        RuleFor(x => x.Value).NotNull().MaximumLength(512);
    }
}

public sealed class CompleteTestResultRequestValidator : AbstractValidator<CompleteTestResultRequest>
{
    public CompleteTestResultRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty();
    }
}
