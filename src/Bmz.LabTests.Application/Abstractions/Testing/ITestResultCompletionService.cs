using Bmz.LabTests.Application.Testing;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Testing;

/// <summary>
/// Доменный сервис для завершения процесса испытаний.
/// Инкапсулирует сложную логику проверки соответствия фактических значений нормам (Limits)
/// и автоматическое создание записей о годной продукции или браке.
/// </summary>
public interface ITestResultCompletionService
{
    /// <summary>
    /// Выполняет финальную обработку протокола.
    /// </summary>
    /// <param name="testResultId">Идентификатор протокола.</param>
    /// <param name="rowVersion">Версия строки для контроля конкурентного доступа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат завершения с информацией о том, признана ли партия годной или бракованной.</returns>
    Task<Result<CompletionResult>> CompleteAsync(int testResultId, byte[] rowVersion, CancellationToken cancellationToken);
}
