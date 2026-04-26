using Bmz.LabTests.Application.Abstractions.Audit;
using Bmz.LabTests.Application.Abstractions.Protocol;
using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Entities;

namespace Bmz.LabTests.Application.Protocol;

public sealed class ProtocolService(
    IProtocolRepository repository,
    IAuditService auditService) : IProtocolService
{
    public async Task<Result<WireCodeInputSchemaDto>> GetInputSchemaAsync(int wireCodeId, CancellationToken cancellationToken)
    {
        try
        {
            var wireCode = await repository.GetWireCodeByIdAsync(wireCodeId, cancellationToken);
            if (wireCode is null)
            {
                return Result.Failure<WireCodeInputSchemaDto>("Марка проволоки не найдена.");
            }

            var limits = await repository.GetLimitsForWireCodeAsync(wireCodeId, cancellationToken);
            var fields = limits
                .OrderBy(x => x.Parameter.Name)
                .Select(x => new InputFieldDto(
                    x.ParameterId,
                    x.Parameter.Name,
                    x.Parameter.DataType,
                    x.Parameter.Unit,
                    x.IsRequired,
                    x.MinValue,
                    x.MaxValue))
                .ToArray();

            return Result.Success(new WireCodeInputSchemaDto(
                new WireCodeBriefDto(wireCode.Id, wireCode.Code, wireCode.Marking, wireCode.Diameter),
                fields));
        }
        catch (Exception ex)
        {
            return Result.Failure<WireCodeInputSchemaDto>($"Ошибка при получении схемы ввода: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyCollection<LimitDto>>> GetLimitsAsync(int wireCodeId, CancellationToken cancellationToken)
    {
        try
        {
            if (!await repository.WireCodeExistsAsync(wireCodeId, cancellationToken))
            {
                return Result.Failure<IReadOnlyCollection<LimitDto>>("Марка проволоки не найдена.");
            }

            var limits = await repository.GetLimitsForWireCodeAsync(wireCodeId, cancellationToken);
            var dtos = limits
                .OrderBy(x => x.Parameter.Name)
                .Select(x => new LimitDto(
                    x.Id,
                    x.WireCodeId,
                    x.ParameterId,
                    x.Parameter.Name,
                    x.Parameter.DataType,
                    x.Parameter.Unit,
                    x.IsRequired,
                    x.MinValue,
                    x.MaxValue))
                .ToArray();

            return Result.Success<IReadOnlyCollection<LimitDto>>(dtos);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyCollection<LimitDto>>($"Ошибка при получении лимитов: {ex.Message}");
        }
    }

    public async Task<Result> ReplaceLimitsAsync(int actorUserId, string? actorLogin, int wireCodeId, IReadOnlyCollection<LimitUpsertItemDto> items, CancellationToken cancellationToken)
    {
        try
        {
            if (!await repository.WireCodeExistsAsync(wireCodeId, cancellationToken))
            {
                return Result.Failure("Марка проволоки не найдена.");
            }

            var requestedParameterIds = items.Select(x => x.ParameterId).Distinct().ToArray();
            var parameters = await repository.GetParametersByIdsAsync(requestedParameterIds, cancellationToken);
            if (parameters.Count != requestedParameterIds.Length)
            {
                return Result.Failure("Один или несколько параметров не существуют.");
            }

            var limits = items.Select(x => new WireCodeLimit
            {
                WireCodeId = wireCodeId,
                ParameterId = x.ParameterId,
                IsRequired = x.IsRequired,
                MinValue = x.MinValue,
                MaxValue = x.MaxValue
            }).ToArray();

            await repository.ReplaceLimitsAsync(wireCodeId, limits, cancellationToken);
            await auditService.WriteAsync(actorUserId, actorLogin, "Replace", "Limits", wireCodeId.ToString(), $"Replaced {limits.Length} limits", cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка при замене лимитов: {ex.Message}");
        }
    }
}
