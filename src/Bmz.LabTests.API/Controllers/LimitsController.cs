using Bmz.LabTests.API.Contracts.Limits;
using Bmz.LabTests.API.Extensions;
using Bmz.LabTests.Application.Abstractions.Protocol;
using Bmz.LabTests.Application.Protocol;
using Bmz.LabTests.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

/// <summary>
/// Контроллер для управления техническими нормами (лимитами) испытаний.
/// Привязывает параметры и их допустимые границы к конкретным маркам проволоки.
/// </summary>
[ApiController]
[Route("api/wire-codes/{wireCodeId:int}/limits")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Engineer}")]
public sealed class LimitsController(IProtocolService service) : ApiControllerBase
{
    /// <summary>
    /// Возвращает список всех настроенных норм для указанной марки проволоки.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetForWireCode(int wireCodeId, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetLimitsAsync(wireCodeId, cancellationToken));
    }

    /// <summary>
    /// Полностью обновляет (заменяет) список норм для марки проволоки.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> ReplaceForWireCode(
        int wireCodeId,
        [FromBody] UpsertWireCodeLimitsRequest request,
        CancellationToken cancellationToken)
    {
        var (actorUserId, userLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await service.ReplaceLimitsAsync(
            actorUserId,
            userLogin,
            wireCodeId,
            request.Items.Select(x => new LimitUpsertItemDto(x.ParameterId, x.IsRequired, x.MinValue, x.MaxValue)).ToArray(),
            cancellationToken));
    }

    /// <summary>
    /// Вспомогательный метод для предварительной валидации набора норм.
    /// </summary>
    [HttpPost("validate")]
    public IActionResult ValidateLimits([FromBody] UpsertWireCodeLimitsRequest request)
    {
        var items = request.Items.Where(x => x.IsRequired || x.MinValue.HasValue || x.MaxValue.HasValue).ToArray();
        if (items.Length == 0)
            return BadRequest(new { error = "Нужно настроить хотя бы один параметр" });
        return Ok(new { valid = true, configuredCount = items.Length });
    }
}
