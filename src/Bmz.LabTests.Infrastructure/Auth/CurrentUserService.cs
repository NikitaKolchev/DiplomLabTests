using Bmz.LabTests.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Bmz.LabTests.Infrastructure.Auth;

/// <summary>
/// Сервис для получения информации о текущем авторизованном пользователе из HTTP-контекста.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <summary>
    /// Возвращает идентификатор текущего пользователя.
    /// </summary>
    public int UserId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub");
            return int.TryParse(claim?.Value, out var id) ? id : 0;
        }
    }

    /// <summary>
    /// Возвращает название роли текущего пользователя.
    /// </summary>
    public string Role
        => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    /// <summary>
    /// Возвращает идентификатор лаборатории пользователя, если он извлечен из токена.
    /// </summary>
    public int? LaboratoryId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("laboratoryId");
            return int.TryParse(claim?.Value, out var id) ? id : null;
        }
    }

    /// <summary>
    /// Возвращает название лаборатории пользователя.
    /// </summary>
    public string? LaboratoryName
        => _httpContextAccessor.HttpContext?.User.FindFirst("laboratoryName")?.Value;

    /// <summary>
    /// Проверяет, аутентифицирован ли пользователь в данный момент.
    /// </summary>
    public bool IsAuthenticated
        => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}