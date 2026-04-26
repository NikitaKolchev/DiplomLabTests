namespace Bmz.LabTests.Application.Abstractions;

public interface ICurrentUserService
{
    int UserId { get; }
    string Role { get; }
    string? LaboratoryName { get; }
    int? LaboratoryId { get; }
    bool IsAuthenticated { get; }
}