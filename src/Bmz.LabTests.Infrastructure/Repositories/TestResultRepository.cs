using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для управления протоколами испытаний и их значениями.
/// Реализует сложные запросы с фильтрацией через паттерн Спецификация.
/// </summary>
public sealed class TestResultRepository(ApplicationDbContext dbContext) : ITestResultRepository
{
    /// <summary>
    /// Выполняет поиск протоколов на основе предоставленной спецификации.
    /// Возвращает список элементов и общее количество для пагинации.
    /// </summary>
    public async Task<(List<TestResult> Items, int TotalCount)> GetListAsync(Specification<TestResult> specification, CancellationToken cancellationToken)
    {
        var baseQuery = ApplySpecificationCriteriaOnly(specification);
        var query = ApplySpecification(specification);

        // Получаем элементы и общее количество в одном запросе к БД.
        // EF Core транслирует Count() в подзапрос, что позволяет сократить количество раундтрипов.
        var results = await query
            .Select(x => new
            {
                Item = x,
                TotalCount = baseQuery.Count()
            })
            .ToListAsync(cancellationToken);

        if (results.Count == 0)
        {
            // Если на текущей странице нет элементов, все равно нужно общее количество для пагинации.
            var totalCount = await baseQuery.CountAsync(cancellationToken);
            return (new List<TestResult>(), totalCount);
        }

        return (results.Select(r => r.Item).ToList(), results[0].TotalCount);
    }

    /// <summary>
    /// Применяет все правила спецификации (фильтры, загрузки, сортировку, пагинацию) к запросу.
    /// </summary>
    private IQueryable<TestResult> ApplySpecification(Specification<TestResult> specification)
    {
        var query = dbContext.TestResults.AsNoTracking().AsQueryable();

        foreach (var criteria in specification.Criteria)
        {
            query = query.Where(criteria);
        }

        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy).ThenBy(x => x.Id);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending).ThenBy(x => x.Id);
        }
        else
        {
            query = query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id);
        }

        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }

    /// <summary>
    /// Применяет только критерии фильтрации спецификации. Используется для подсчета общего количества.
    /// </summary>
    private IQueryable<TestResult> ApplySpecificationCriteriaOnly(Specification<TestResult> specification)
    {
        var query = dbContext.TestResults.AsNoTracking().AsQueryable();

        foreach (var criteria in specification.Criteria)
        {
            query = query.Where(criteria);
        }

        return query;
    }

    /// <summary>
    /// Получает протокол по ID.
    /// </summary>
    public Task<TestResult?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.TestResults.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <summary>
    /// Получает протокол вместе с загруженными значениями измерений.
    /// </summary>
    public Task<TestResult?> GetByIdWithValuesAsync(int id, CancellationToken cancellationToken)
        => dbContext.TestResults
            .Include(x => x.Values)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <summary>
    /// Проверяет существование марки проволоки в БД.
    /// </summary>
    public Task<bool> WireCodeExistsAsync(int wireCodeId, CancellationToken cancellationToken)
        => dbContext.WireCodes.AnyAsync(x => x.Id == wireCodeId, cancellationToken);

    /// <summary>
    /// Возвращает список ID параметров, которые допустимы для заполнения в данной марке проволоки.
    /// </summary>
    public Task<List<int>> GetAllowedParameterIdsAsync(int wireCodeId, CancellationToken cancellationToken)
        => dbContext.Limits.Where(x => x.WireCodeId == wireCodeId).Select(x => x.ParameterId).ToListAsync(cancellationToken);

    /// <summary>
    /// Добавляет новый протокол в контекст.
    /// </summary>
    public Task AddAsync(TestResult testResult, CancellationToken cancellationToken)
        => dbContext.TestResults.AddAsync(testResult, cancellationToken).AsTask();

    /// <summary>
    /// Регистрирует факт выпуска годной продукции.
    /// </summary>
    public Task AddFinalProductAsync(FinalProduct finalProduct, CancellationToken cancellationToken)
        => dbContext.FinalProducts.AddAsync(finalProduct, cancellationToken).AsTask();

    /// <summary>
    /// Регистрирует факт брака.
    /// </summary>
    public Task AddRejectAsync(Reject reject, CancellationToken cancellationToken)
        => dbContext.Rejects.AddAsync(reject, cancellationToken).AsTask();

    /// <summary>
    /// Загружает нормы (лимиты) для конкретной марки проволоки.
    /// </summary>
    public Task<List<WireCodeLimit>> GetLimitsByWireCodeIdAsync(int wireCodeId, CancellationToken cancellationToken)
        => dbContext.Limits
            .Include(x => x.Parameter)
            .Where(x => x.WireCodeId == wireCodeId)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Устанавливает оригинальное значение RowVersion для реализации оптимистичной блокировки при сохранении.
    /// </summary>
    public void SetOriginalRowVersion(TestResult testResult, byte[] rowVersion)
        => dbContext.Entry(testResult).Property(x => x.RowVersion).OriginalValue = rowVersion;

    /// <summary>
    /// Сохраняет все изменения в базе данных.
    /// </summary>
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Удаляет протокол по его идентификатору.
    /// </summary>
    public async Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TestResults.FindAsync([id], cancellationToken);
        if (entity is null)
            return false;
        dbContext.TestResults.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
