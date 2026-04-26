using Bmz.LabTests.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Bmz.LabTests.Infrastructure.Auth;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public int UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub");
            return int.TryParse(claim?.Value, out var id) ? id : 0;
        }
    }

    public string Role
        => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    public int? LaboratoryId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("laboratoryId");
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }

    public string? LaboratoryName
        => _httpContextAccessor.HttpContext?.User.FindFirst("laboratoryName")?.Value;

    public bool IsAuthenticated
        => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}