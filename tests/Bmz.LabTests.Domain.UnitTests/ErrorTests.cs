using Bmz.LabTests.Domain.Common;
using FluentAssertions;

namespace Bmz.LabTests.Domain.UnitTests;

public sealed class ErrorTests
{
    [Fact]
    public void None_ShouldHaveNoneTypeAndEmptyMessage()
    {
        Error.None.Type.Should().Be(ErrorType.None);
        Error.None.Message.Should().BeEmpty();
    }

    [Theory]
    [InlineData("x")]
    [InlineData("тест")]
    public void Factories_ShouldSetMessage(string message)
    {
        Error.Failure(message).Message.Should().Be(message);
        Error.Validation(message).Message.Should().Be(message);
        Error.NotFound(message).Message.Should().Be(message);
        Error.Conflict(message).Message.Should().Be(message);
        Error.Unauthorized(message).Message.Should().Be(message);
        Error.Forbidden(message).Message.Should().Be(message);
    }

    [Fact]
    public void ImplicitStringConversion_ShouldReturnMessage()
    {
        Error error = Error.NotFound("missing");
        string message = error;

        message.Should().Be("missing");
    }
}

