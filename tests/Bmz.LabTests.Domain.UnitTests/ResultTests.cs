using Bmz.LabTests.Domain.Common;
using FluentAssertions;

namespace Bmz.LabTests.Domain.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_WithError_ShouldCreateFailedResult()
    {
        var error = Error.Validation("bad");
        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Failure_WithNoneError_ShouldThrow()
    {
        var act = () => Result.Failure(Error.None);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Success_Generic_ShouldExposeValue()
    {
        var result = Result.Success(123);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(123);
    }

    [Fact]
    public void Failure_Generic_ValueAccess_ShouldThrow()
    {
        var result = Result.Failure<int>("fail");

        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitConversion_ShouldCreateSuccessfulGenericResult()
    {
        Result<int> result = 42;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }
}

