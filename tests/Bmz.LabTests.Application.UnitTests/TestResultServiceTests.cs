using AutoFixture;
using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Application.Abstractions.Testing;
using Bmz.LabTests.Application.TestResults;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Bmz.LabTests.Application.UnitTests;

public sealed class TestResultServiceTests
{
    private readonly ITestResultRepository _repository = Substitute.For<ITestResultRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITestResultCompletionService _completionService = Substitute.For<ITestResultCompletionService>();
    private readonly Fixture _fixture = new();
    private readonly TestResultService _sut;

    public TestResultServiceTests()
    {
        _sut = new TestResultService(_repository, _userRepository, _completionService);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenWireCodeDoesNotExist()
    {
        // Arrange
        var request = _fixture.Create<CreateTestResultDto>();
        _repository.WireCodeExistsAsync(request.WireCodeId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.CreateAsync(1, Roles.Assistant, request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("код проволоки не существует");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenAssistantHasNoLaboratory()
    {
        // Arrange
        var request = _fixture.Create<CreateTestResultDto>();
        var user = new User { Id = 1, Login = "login", FullName = "Assistant Name", LaboratoryId = null };
        
        _repository.WireCodeExistsAsync(request.WireCodeId, Arg.Any<CancellationToken>())
            .Returns(true);
        _userRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.CreateAsync(1, Roles.Assistant, request, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenAllValid()
    {
        // Arrange
        var request = _fixture.Create<CreateTestResultDto>();
        var user = new User { Id = 1, Login = "login", FullName = "Assistant Name", LaboratoryId = 10 };
        
        _repository.WireCodeExistsAsync(request.WireCodeId, Arg.Any<CancellationToken>())
            .Returns(true);
        _userRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(user);
        
        _repository.AddAsync(Arg.Any<TestResult>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _sut.CreateAsync(1, Roles.Assistant, request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        await _repository.Received(1).AddAsync(Arg.Any<TestResult>(), Arg.Any<CancellationToken>());
    }
}
