using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Application.Abstractions.TestResults;
using Bmz.LabTests.Application.Abstractions.Testing;
using Bmz.LabTests.Application.Testing;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Application.TestResults;

/// <summary>
/// Реализация сервиса управления протоколами испытаний.
/// Обрабатывает бизнес-логику, разграничение прав доступа и взаимодействие с репозиториями.
/// </summary>
public sealed class TestResultService(
    ITestResultRepository repository,
    IUserRepository userRepository,
    ITestResultCompletionService completionService) : ITestResultService
{
    private Result<int?>? _cachedLaboratoryScope;

    /// <summary>
    /// Получает список протоколов с фильтрацией.
    /// Реализует логику "видимости": обычные пользователи видят только протоколы своей лаборатории,
    /// администраторы видят всё.
    /// </summary>
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
        TestResultSortBy? sortBy,
        bool? sortDesc,
        CancellationToken cancellationToken)
    {
        try
        {
            // Определяем область видимости (лабораторию) для текущего пользователя
            var laboratoryIdResult = await ResolveLaboratoryScopeAsync(currentUserId, currentRole, cancellationToken);
            if (!laboratoryIdResult.IsSuccess)
                return Result.Failure<PaginatedListDto<TestResultListItemDto>>(laboratoryIdResult.Error);

            var laboratoryIdFilter = laboratoryIdResult.Value;

            // Формируем спецификацию поиска
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

            // Выполняем запрос к БД
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

    /// <summary>
    /// Создает новый протокол. Только пользователи с ролью Assistant могут создавать протоколы.
    /// </summary>
    public async Task<Result<CreatedTestResultDto>> CreateAsync(int currentUserId, string currentRole, CreateTestResultDto request, CancellationToken cancellationToken)
    {
        // Проверка существования шифра
        if (!await repository.WireCodeExistsAsync(request.WireCodeId, cancellationToken))
        {
            return Result.Failure<CreatedTestResultDto>("Указанный код проволоки не существует.");
        }

        var currentUser = await userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (currentUser is null)
            return Result.Failure<CreatedTestResultDto>(Error.NotFound("Текущий пользователь не найден."));

        // Привязываем протокол к лаборатории создателя
        var laboratoryId = currentUser.LaboratoryId ?? 0;
        if (string.Equals(currentRole, Roles.Assistant, StringComparison.OrdinalIgnoreCase) && laboratoryId == 0)
        {
            return Result.Failure<CreatedTestResultDto>(Error.Forbidden("Лаборант не назначен в лабораторию."));
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

    /// <summary>
    /// Получает детали протокола с проверкой прав доступа.
    /// </summary>
    public async Task<Result<TestResultDetailsDto>> GetByIdAsync(int currentUserId, string currentRole, int id, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdWithValuesAsync(id, cancellationToken);
        if (item is null)
            return Result.Failure<TestResultDetailsDto>(Error.NotFound("Результат испытания не найден."));

        // Проверка прав: нельзя смотреть протокол чужой лаборатории (кроме админа)
        var laboratoryIdResult = await ResolveLaboratoryScopeAsync(currentUserId, currentRole, cancellationToken);
        if (!laboratoryIdResult.IsSuccess)
            return Result.Failure<TestResultDetailsDto>(laboratoryIdResult.Error);

        var isAdmin = string.Equals(currentRole, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && IsLaboratoryScopedRole(currentRole))
        {
            if (item.LaboratoryId != laboratoryIdResult.Value)
            {
                return Result.Failure<TestResultDetailsDto>(Error.Forbidden("Доступ запрещен."));
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

    /// <summary>
    /// Сохраняет значения измерений. 
    /// Реализует оптимистичную блокировку через RowVersion.
    /// </summary>
    public async Task<Result<SavedTestResultDto>> SaveValuesAsync(int currentUserId, string currentRole, int id, SaveTestValuesDto request, CancellationToken cancellationToken)
    {
        // Декодируем RowVersion из Base64 для передачи в EF
        if (!TryParseRowVersion(request.RowVersion, out var rowVersion))
        {
            return Result.Failure<SavedTestResultDto>("RowVersion не является корректной Base64-строкой.");
        }

        var testResult = await repository.GetByIdWithValuesAsync(id, cancellationToken);
        if (testResult is null)
        {
            return Result.Failure<SavedTestResultDto>(Error.NotFound("Результат испытания не найден."));
        }

        // Проверка прав на редактирование
        if (IsLaboratoryScopedRole(currentRole))
        {
            var labResult = await ResolveLaboratoryScopeAsync(currentUserId, currentRole, cancellationToken);
            if (!labResult.IsSuccess)
                return Result.Failure<SavedTestResultDto>(labResult.Error);
            if (testResult.LaboratoryId != labResult.Value)
            {
                return Result.Failure<SavedTestResultDto>(Error.Forbidden("Нет доступа к испытанию другой лаборатории."));
            }
        }

        var isAdmin = string.Equals(currentRole, Roles.Admin, StringComparison.OrdinalIgnoreCase);
        if (testResult.Status == TestResultStatus.Completed && !isAdmin)
        {
            return Result.Failure<SavedTestResultDto>("Завершенный результат испытания нельзя редактировать.");
        }

        // Устанавливаем оригинальный RowVersion для проверки конкурентного доступа в EF
        repository.SetOriginalRowVersion(testResult, rowVersion);

        // Проверка, что передаваемые параметры допустимы для данного шифра
        var allowedParameterIds = await repository.GetAllowedParameterIdsAsync(testResult.WireCodeId, cancellationToken);
        var notAllowed = request.Values.Select(x => x.ParameterId).Except(allowedParameterIds).ToArray();
        if (notAllowed.Length > 0)
        {
            return Result.Failure<SavedTestResultDto>($"Для этого кода проволоки не настроены параметры: {string.Join(", ", notAllowed)}.");
        }

        // Проверка, что все обязательные параметры заполнены
        var limits = await repository.GetLimitsByWireCodeIdAsync(testResult.WireCodeId, cancellationToken);
        var requiredParamIds = limits.Where(l => l.IsRequired).Select(l => l.ParameterId).ToHashSet();
        var submittedValueIds = request.Values
            .Where(v => requiredParamIds.Contains(v.ParameterId))
            .ToDictionary(v => v.ParameterId, v => v.Value);
        var missingRequired = limits
            .Where(l => l.IsRequired && string.IsNullOrWhiteSpace(submittedValueIds.GetValueOrDefault(l.ParameterId)))
            .Select(l => l.Parameter.Name)
            .ToArray();
        if (missingRequired.Length > 0)
        {
            return Result.Failure<SavedTestResultDto>($"Заполните обязательные поля: {string.Join(", ", missingRequired)}.");
        }

        foreach (var value in request.Values)
        {
            testResult.AddOrUpdateValue(value.ParameterId, value.Value);
        }

        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success(new SavedTestResultDto(testResult.Id, testResult.UpdatedAtUtc, Convert.ToBase64String(testResult.RowVersion)));
    }

    /// <summary>
    /// Завершает протокол и делегирует проверку норм специализированному сервису.
    /// </summary>
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
                return Result.Failure<CompletionResult>(Error.NotFound("Результат испытания не найден."));
            }

            var labResult = await ResolveLaboratoryScopeAsync(currentUserId, currentRole, cancellationToken);
            if (!labResult.IsSuccess)
                return Result.Failure<CompletionResult>(labResult.Error);
            if (testResult.LaboratoryId != labResult.Value)
            {
                return Result.Failure<CompletionResult>(Error.Forbidden("Нет доступа к испытанию другой лаборатории."));
            }
        }

        return await completionService.CompleteAsync(id, rowVersion, cancellationToken);
    }

    /// <summary>
    /// Удаляет протокол. Доступно только администратору.
    /// </summary>
    public async Task<Result> DeleteAsync(int currentUserId, string currentRole, int id, CancellationToken cancellationToken)
    {
        if (!string.Equals(currentRole, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(Error.Forbidden("Только администратор может удалять записи."));

        var item = await repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return Result.Failure(Error.NotFound("Результат испытания не найден."));

        if (!await repository.DeleteByIdAsync(id, cancellationToken))
            return Result.Failure(Error.Failure("Не удалось удалить испытание."));

        return Result.Success();
    }

    /// <summary>
    /// Вспомогательный метод для парсинга RowVersion из строки Base64.
    /// </summary>
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
        if (_cachedLaboratoryScope != null)
            return _cachedLaboratoryScope;

        if (currentUserId == 0 || string.IsNullOrEmpty(currentRole) || string.Equals(currentRole, "Guest", StringComparison.OrdinalIgnoreCase))
        {
            _cachedLaboratoryScope = Result.Success<int?>(null);
            return _cachedLaboratoryScope;
        }

        if (!IsLaboratoryScopedRole(currentRole))
        {
            _cachedLaboratoryScope = Result.Success<int?>(null);
            return _cachedLaboratoryScope;
        }

        var user = await userRepository.GetByIdAsync(currentUserId, cancellationToken);
        if (user == null)
        {
            _cachedLaboratoryScope = Result.Failure<int?>(Error.NotFound("Пользователь не найден."));
            return _cachedLaboratoryScope;
        }

        if (string.Equals(currentRole, Roles.Assistant, StringComparison.OrdinalIgnoreCase))
        {
            if (!user.LaboratoryId.HasValue)
            {
                _cachedLaboratoryScope = Result.Failure<int?>(Error.Forbidden("Лаборант не назначен в лабораторию."));
            }
            else
            {
                _cachedLaboratoryScope = Result.Success(user.LaboratoryId);
            }
            return _cachedLaboratoryScope;
        }

        if (string.Equals(currentRole, Roles.Engineer, StringComparison.OrdinalIgnoreCase))
        {
            var engineerLaboratoryId = user.LaboratoryId ?? await userRepository.GetLaboratoryIdByEngineerIdAsync(currentUserId, cancellationToken);
            if (!engineerLaboratoryId.HasValue)
            {
                _cachedLaboratoryScope = Result.Failure<int?>(Error.Forbidden("Инженер не назначен в лабораторию."));
            }
            else
            {
                _cachedLaboratoryScope = Result.Success(engineerLaboratoryId);
            }
            return _cachedLaboratoryScope;
        }

        return _cachedLaboratoryScope ?? Result.Success<int?>(null);
    }

    private static bool IsLaboratoryScopedRole(string role)
        => string.Equals(role, Roles.Assistant, StringComparison.OrdinalIgnoreCase)
           || string.Equals(role, Roles.Engineer, StringComparison.OrdinalIgnoreCase);
}
