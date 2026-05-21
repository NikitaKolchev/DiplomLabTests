using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для настройки норм и параметров испытаний.
/// Управляет связями между марками проволоки и их техническими требованиями (лимитами).
/// </summary>
public sealed class ProtocolRepository(ApplicationDbContext dbContext) : IProtocolRepository
{
    /// <summary>
    /// Получает информацию о марке проволоки без отслеживания изменений.
    /// </summary>
    public Task<WireCode?> GetWireCodeByIdAsync(int wireCodeId, CancellationToken cancellationToken)
        => dbContext.WireCodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == wireCodeId, cancellationToken);

    /// <summary>
    /// Проверяет существование марки проволоки.
    /// </summary>
    public Task<bool> WireCodeExistsAsync(int wireCodeId, CancellationToken cancellationToken)
        => dbContext.WireCodes.AnyAsync(x => x.Id == wireCodeId, cancellationToken);

    /// <summary>
    /// Загружает все нормы (лимиты) для указанной марки проволоки вместе с описанием параметров.
    /// </summary>
    public Task<List<WireCodeLimit>> GetLimitsForWireCodeAsync(int wireCodeId, CancellationToken cancellationToken)
        => dbContext.Limits
            .AsNoTracking()
            .Include(x => x.Parameter)
            .Where(x => x.WireCodeId == wireCodeId)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Загружает список параметров по их идентификаторам.
    /// </summary>
    public Task<List<Parameter>> GetParametersByIdsAsync(IReadOnlyCollection<int> parameterIds, CancellationToken cancellationToken)
        => dbContext.Parameters
            .Where(x => parameterIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Синхронизирует список норм для марки проволоки.
    /// Старые нормы удаляются, новые добавляются в рамках одной транзакции.
    /// </summary>
    public async Task ReplaceLimitsAsync(int wireCodeId, IReadOnlyCollection<WireCodeLimit> limits, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Limits.Where(x => x.WireCodeId == wireCodeId).ToListAsync(cancellationToken);
        dbContext.Limits.RemoveRange(existing);
        dbContext.Limits.AddRange(limits);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
