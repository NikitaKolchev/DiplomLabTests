using Bmz.LabTests.Application.Testing;
using Bmz.LabTests.Application.TestResults;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Application.Abstractions.TestResults;

/// <summary>
/// Основной интерфейс для работы с протоколами испытаний.
/// Содержит методы для управления жизненным циклом протокола: создание, поиск, заполнение данных и завершение.
/// </summary>
public interface ITestResultService
{
    /// <summary>
    /// Получает пагинированный список протоколов с учетом прав доступа пользователя.
    /// </summary>
    Task<Result<PaginatedListDto<TestResultListItemDto>>> GetListAsync(
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
        CancellationToken cancellationToken);

    /// <summary>Создает новый протокол испытаний.</summary>
    Task<Result<CreatedTestResultDto>> CreateAsync(int currentUserId, string currentRole, CreateTestResultDto request, CancellationToken cancellationToken);
    
    /// <summary>Получает детальную информацию о протоколе по его идентификатору.</summary>
    Task<Result<TestResultDetailsDto>> GetByIdAsync(int currentUserId, string currentRole, int id, CancellationToken cancellationToken);
    
    /// <summary>Сохраняет (добавляет или обновляет) значения измерений в протоколе.</summary>
    Task<Result<SavedTestResultDto>> SaveValuesAsync(int currentUserId, string currentRole, int id, SaveTestValuesDto request, CancellationToken cancellationToken);
    
    /// <summary>Завершает испытания по протоколу и выполняет проверку на соответствие нормам.</summary>
    Task<Result<CompletionResult>> CompleteAsync(int currentUserId, string currentRole, int id, string rowVersionBase64, CancellationToken cancellationToken);
    
    /// <summary>Удаляет протокол из системы.</summary>
    Task<Result> DeleteAsync(int currentUserId, string currentRole, int id, CancellationToken cancellationToken);
}
