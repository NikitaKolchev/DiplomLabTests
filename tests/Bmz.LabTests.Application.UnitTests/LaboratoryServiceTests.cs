using Bmz.LabTests.Application.Abstractions.Audit;
using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Application.Organization;
using Bmz.LabTests.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Bmz.LabTests.Application.UnitTests;

public sealed class LaboratoryServiceTests
{
    private readonly IOrganizationRepository _repository = Substitute.For<IOrganizationRepository>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly LaboratoryService _sut;

    public LaboratoryServiceTests()
    {
        _sut = new LaboratoryService(_repository, _auditService);
    }

    [Fact]
    public async Task CreateLaboratoryAsync_ShouldReturnFailure_WhenEngineerAlreadyAssigned()
    {
        // Arrange
        var engineer = new User { Id = 2, Login = "eng", FullName = "Eng FullName" };
        _repository.GetEngineerByIdAsync(2, Arg.Any<CancellationToken>())
            .Returns(engineer);
        _repository.IsEngineerAssignedToAnotherLaboratoryAsync(2, null, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.CreateLaboratoryAsync(1, "admin", "Lab Name", 2, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("уже назначен");
    }

    [Fact]
    public async Task CreateLaboratoryAsync_ShouldSucceed_AndWriteAudit()
    {
        // Arrange
        var name = "New Lab";
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        // Act
        var result = await _sut.CreateLaboratoryAsync(1, "admin", name, null, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        await _repository.Received(1).AddLaboratoryAsync(Arg.Any<Laboratory>(), Arg.Any<CancellationToken>());
        await _auditService.Received(1).WriteAsync(1, "admin", "Create", "Laboratory", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
