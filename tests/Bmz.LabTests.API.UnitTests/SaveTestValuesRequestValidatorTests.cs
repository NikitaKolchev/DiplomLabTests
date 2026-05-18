using Bmz.LabTests.API.Contracts.TestResults;
using Bmz.LabTests.API.Validators.TestResults;
using FluentValidation.TestHelper;

namespace Bmz.LabTests.API.UnitTests;

public sealed class SaveTestValuesRequestValidatorTests
{
    private readonly SaveTestValuesRequestValidator _validator = new();

    [Fact]
    public void RowVersion_ShouldBeRequired()
    {
        var model = new SaveTestValuesRequest { RowVersion = "", Values = [new TestValueItemRequest { ParameterId = 1, Value = "x" }] };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }

    [Fact]
    public void Values_ShouldContainAtLeastOneItem()
    {
        var model = new SaveTestValuesRequest { RowVersion = "rv", Values = [] };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Values);
    }

    [Fact]
    public void Values_ParameterIds_ShouldBeUnique()
    {
        var model = new SaveTestValuesRequest
        {
            RowVersion = "rv",
            Values =
            [
                new TestValueItemRequest { ParameterId = 1, Value = "a" },
                new TestValueItemRequest { ParameterId = 1, Value = "b" }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Values);
    }

    [Fact]
    public void EachValue_ShouldRequirePositiveParameterId()
    {
        var model = new SaveTestValuesRequest
        {
            RowVersion = "rv",
            Values = [new TestValueItemRequest { ParameterId = 0, Value = "a" }]
        };

        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor("Values[0].ParameterId");
    }

    [Fact]
    public void ValidRequest_ShouldPass()
    {
        var model = new SaveTestValuesRequest
        {
            RowVersion = "rv",
            Values =
            [
                new TestValueItemRequest { ParameterId = 1, Value = "a" },
                new TestValueItemRequest { ParameterId = 2, Value = "b" }
            ]
        };

        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

