using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Application.Abstractions.TestResults;
using Bmz.LabTests.Application.Abstractions.Testing;
using Bmz.LabTests.Application.Testing;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Application.TestResults;

public sealed class TestResultService(
    ITestResultRepository repository,
    ITestResultCompletionService completionService) : ITestResultService
{
    public async Task<Result<PaginatedListDto<TestResultListItemDto>>> GetListAsync(
        int currentUserId,
        string currentRole,
        DateTime? fromUtc,
        DateTime? toUtc,
        int? wireCodeId,
        string? batchNumber,
        TestResultStatus? status,
        int page,
        int pageSize,
        string? sortBy,
        bool? sortDesc,
        CancellationToken cancellationToken)
    {
        try
        {
            var laboratoryIdResult = await ResolveLaboratoryScopeAsync(currentUserId, currentRole, cancellationToken);
            if (!laboratoryIdResult.IsSuccess)
                return Result.Failure<PaginatedListDto<TestResultListItemDto>>(laboratoryIdResult.Error!);

            var laboratoryIdFilter = laboratoryIdResult.Value;

            var spec = new TestResultSearchSpecification(
                laboratoryIdFilter,
                fromUtc,
                toUtc,
                wireCodeId,
                batchNumber,
                status,
                page,
                pageSize,
                sortBy,
                sortDesc);

            var (items, totalCount) = await repository.GetListAsync(spec, cancellationToken);

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var dtos = items
                .Select(x => new TestResultListItemDto(
                    x.Id,
                    x.Date,
                    x.UpdatedAtUtc,
                    x.BatchNumber,
                    x.Status,
                    x.WireCodeId,
                    x.WireCode.Code,
                    x.Assistant.FullName,
                    Convert.ToBase64String(x.RowVersion)))
                .ToArray();

            return Result.Success(new PaginatedListDto<TestResultListItemDto>(dtos, totalCount, page, pageSize, totalPages));
        }
        catch (Exception ex)
        {
            return Result.Failure<PaginatedListDto<TestResultListItemDto>>($"Ошибка при получении списка испытаний: {ex.Message}");
        }
    }

    public async Task<Result<CreatedTestResultDto>> CreateAsync(int currentUserId, string currentRole, CreateTestResultDto request, CancellationToken cancellationToken)
    {
        if (!await repository.WireCodeExistsAsync(request.WireCodeId, cancellationToken))
        {
            return Result.Failure<CreatedTestResultDto>("Указанный код проволоки не существует.");
        }

        var currentUser = await repository.GetUserByIdAsync(currentUserId, cancellationToken);
        if (currentUser is null)
            return Result.Failure<CreatedTestResultDto>("Текущий пользователь не найден.");

        var laboratoryId = currentUser.LaboratoryId ?? 0;
        if (string.Equals(currentRole, Roles.Assistant, StringComparison.OrdinalIgnoreCase) && laboratoryId == 0)
        {
            return Result.Failure<CreatedTestResultDto>("Лаборант не назначен в лабораторию.");
        }

        var entity = new TestResult(
            request.AssistantId,
            request.WireCodeId,
            laboratoryId,
            request.BatchNumber,
            request.CustomerId);

        await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreatedTestResultDto(
            entity.Id,
            entity.Date,
            entity.UpdatedAtUtc,
            entity.WireCodeId,
            entity.BatchNumber,
            entity.Status,
            Convert.ToBase64String(entity.RowVersion)));
    }

    public async Task<Result<TestResultDetailsDto>> GetByIdAsync(int currentUserId, string currentRole, int id, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdWithValuesAsync(id, cancellationToken);
        if (item is null)
        {
            return Result.Failure<TestResultDetailsDto>("Результат испытания не найден.");
        }

        var isAdmin = string.Equals(currentRole, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && IsLaboratoryScopedRole(currentRole))
        {
            var labResult = await ResolveLaboratoryScopeAsync(currentUserId, currentRole, cancellationToken);
            if (!labResult.IsSuccess)
                return Result.Failure<TestResultDetailsDto>(labResult.Error!);
            if (item.LaboratoryId != labResult.Value)
            {
                return Result.Failure<TestResultDetailsDto>("Доступ запрещен.");
            }
        }

        return Result.Success(new TestResultDetailsDto(
            item.Id,
            item.Date,
            item.UpdatedAtUtc,
            item.AssistantId,
            item.WireCodeId,
            item.BatchNumber,
            item.Status,
            Convert.ToBase64String(item.RowVersion),
            item.Values.Select(v => new TestResultValueDto(v.ParameterId, v.Value)).ToArray()));
    }

    public async Task<Result<SavedTestResultDto>> SaveValuesAsync(int currentUserId, string currentRole, int id, SaveTestValuesDto request, CancellationToken cancellationToken)
    {
        if (!TryParseRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure<SavedTestResultDto>("RowVersion не является корректной Base64-строкой.");
        }

        var testResult = await repository.GetByIdWithValuesAsync(id, cancellationToken);
        if (testResult is null)
        {
            return Result.Failure<SavedTestResultDto>("Результат испытания не найден.");
        }

        if (IsLaboratoryScopedRole(currentRole))
        {
            var labResult = await ResolveLaboratoryScopeAsync(currentUserId, currentRole, cancellationToken);
            if (!labResult.IsSuccess)
                return Result.Failure<SavedTestResultDto>(labResult.Error!);
            if (testResult.LaboratoryId != labResult.Value)
            {
                return Result.Failure<SavedTestResultDto>("Доступ запрещен.");
            }
        }

        var isAdmin = string.Equals(currentRole, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        if (testResult.Status == TestResultStatus.Completed && !isAdmin)
        {
            return Result.Failure<SavedTestResultDto>("Завершенный результат испытания нельзя редактировать.");
        }

        repository.SetOriginalRowVersion(testResult, rowVersion);

        var allowedParameterIds = await repository.GetAllowedParameterIdsAsync(testResult.WireCodeId, cancellationToken);
        var notAllowed = request.Values.Select(x => x.ParameterId).Except(allowedParameterIds).ToArray();
        if (notAllowed.Length > 0)
        {
            return Result.Failure<SavedTestResultDto>($"Для этого кода проволоки не настроены параметры: {string.Join(", ", notAllowed)}.");
        }

        foreach (var value in request.Values)
        {
            testResult.AddOrUpdateValue(value.ParameterId, value.Value);
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(new SavedTestResultDto(testResult.Id, testResult.UpdatedAtUtc, Convert.ToBase64String(testResult.RowVersion)));
    }

    public async Task<Result<CompletionResult>> CompleteAsync(int currentUserId, string currentRole, int id, string rowVersionBase64, CancellationToken cancellationToken)
    {
        if (!TryParseRowVersion(rowVersionBase64, out var rowVersion))
        {
            return Result.Failure<CompletionResult>("RowVersion не является корректной Base64-строкой.");
        }

        var isAdmin = string.Equals(currentRole, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && IsLaboratoryScopedRole(currentRole))
        {
            var testResult = await repository.GetByIdAsync(id, cancellationToken);
            if (testResult is null)
            {
                return Result.Failure<CompletionResult>("Результат испытания не найден.");
            }

            var labResult = await ResolveLaboratoryScopeAsync(currentUserId, currentRole, cancellationToken);
            if (!labResult.IsSuccess)
                return Result.Failure<CompletionResult>(labResult.Error!);
            if (testResult.LaboratoryId != labResult.Value)
            {
                return Result.Failure<CompletionResult>("Доступ запрещен.");
            }
        }

        return await completionService.CompleteAsync(id, rowVersion, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int currentUserId, string currentRole, int id, CancellationToken cancellationToken)
    {
        if (!string.Equals(currentRole, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            return Result.Failure("Только администратор может удалять записи.");

        var deleted = await repository.DeleteByIdAsync(id, cancellationToken);
        return deleted ? Result.Success() : Result.Failure("Запись не найдена.");
    }

    private static bool TryParseRowVersion(string base64, out byte[] rowVersion)
    {
        try
        {
            rowVersion = Convert.FromBase64String(base64);
            return true;
        }
        catch
        {
            rowVersion = [];
            return false;
        }
    }

    private async Task<Result<int?>> ResolveLaboratoryScopeAsync(int currentUserId, string currentRole, CancellationToken cancellationToken)
    {
        if (currentUserId == 0 || string.IsNullOrEmpty(currentRole) || string.Equals(currentRole, "Guest", StringComparison.OrdinalIgnoreCase))
            return Result.Success<int?>(null);

        if (!IsLaboratoryScopedRole(currentRole))
            return Result.Success<int?>(null);

        var currentUser = await repository.GetUserByIdAsync(currentUserId, cancellationToken);
        if (currentUser is null)
            return Result.Failure<int?>($"Пользователь с ID {currentUserId} не найден.");

        if (string.Equals(currentRole, Roles.Assistant, StringComparison.OrdinalIgnoreCase))
        {
            if (!currentUser.LaboratoryId.HasValue)
                return Result.Failure<int?>("Лаборант не назначен в лабораторию.");
            return Result.Success<int?>(currentUser.LaboratoryId.Value);
        }

        var engineerLaboratoryId = currentUser.LaboratoryId
            ?? await repository.GetLaboratoryIdByEngineerIdAsync(currentUserId, cancellationToken);
        
        if (!engineerLaboratoryId.HasValue)
            return Result.Failure<int?>("Инженер не назначен в лабораторию.");
        
        return Result.Success<int?>(engineerLaboratoryId.Value);
    }

    private static bool IsLaboratoryScopedRole(string role)
        => string.Equals(role, Roles.Assistant, StringComparison.OrdinalIgnoreCase)
           || string.Equals(role, Roles.Engineer, StringComparison.OrdinalIgnoreCase);
}
