using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для управления нормативно-справочной информацией (НСИ).
/// Работает со справочниками стран, потребителей, марок проволоки и параметров.
/// </summary>
public sealed class ReferenceDataRepository(ApplicationDbContext dbContext) : IReferenceDataRepository
{
    /// <summary>
    /// Получает список стран с фильтрацией по названию.
    /// </summary>
    public Task<List<Country>> GetCountriesAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = dbContext.Countries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Name.Contains(searchTerm));
        }

        return query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получает страну по ID.
    /// </summary>
    public Task<Country?> GetCountryByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.Countries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <summary>
    /// Добавляет новую страну.
    /// </summary>
    public Task AddCountryAsync(Country country, CancellationToken cancellationToken)
        => dbContext.Countries.AddAsync(country, cancellationToken).AsTask();

    /// <summary>
    /// Удаляет страну.
    /// </summary>
    public Task DeleteCountryAsync(Country country, CancellationToken cancellationToken)
    {
        dbContext.Countries.Remove(country);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Получает список потребителей с фильтрацией по названию и загрузкой страны.
    /// </summary>
    public Task<List<Customer>> GetCustomersAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = dbContext.Customers.AsNoTracking().Include(x => x.Country);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Name.Contains(searchTerm));
        }

        return query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получает потребителя по ID.
    /// </summary>
    public Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.Customers.Include(x => x.Country).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <summary>
    /// Добавляет нового потребителя.
    /// </summary>
    public Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken)
        => dbContext.Customers.AddAsync(customer, cancellationToken).AsTask();

    /// <summary>
    /// Удаляет потребителя.
    /// </summary>
    public Task DeleteCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        dbContext.Customers.Remove(customer);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Проверяет существование страны по ID.
    /// </summary>
    public Task<bool> CountryExistsAsync(int countryId, CancellationToken cancellationToken)
        => dbContext.Countries.AnyAsync(x => x.Id == countryId, cancellationToken);

    /// <summary>
    /// Получает список марок проволоки с фильтрацией по коду или маркировке.
    /// </summary>
    public Task<List<WireCode>> GetWireCodesAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = dbContext.WireCodes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Code.Contains(searchTerm) || x.Marking.Contains(searchTerm));
        }

        return query.OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получает марку проволоки по ID.
    /// </summary>
    public Task<WireCode?> GetWireCodeByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.WireCodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <summary>
    /// Добавляет новую марку проволоки.
    /// </summary>
    public Task AddWireCodeAsync(WireCode wireCode, CancellationToken cancellationToken)
        => dbContext.WireCodes.AddAsync(wireCode, cancellationToken).AsTask();

    /// <summary>
    /// Удаляет марку проволоки.
    /// </summary>
    public Task DeleteWireCodeAsync(WireCode wireCode, CancellationToken cancellationToken)
    {
        dbContext.WireCodes.Remove(wireCode);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Проверяет уникальность кода марки проволоки.
    /// </summary>
    public Task<bool> WireCodeExistsByCodeAsync(string code, int? excludingId, CancellationToken cancellationToken)
        => dbContext.WireCodes.AnyAsync(x => x.Code == code && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);

    /// <summary>
    /// Получает список параметров испытаний с фильтрацией по названию.
    /// </summary>
    public Task<List<Parameter>> GetParametersAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = dbContext.Parameters.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Name.Contains(searchTerm));
        }

        return query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Получает параметр по ID.
    /// </summary>
    public Task<Parameter?> GetParameterByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.Parameters.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    /// <summary>
    /// Добавляет новый параметр испытания.
    /// </summary>
    public Task AddParameterAsync(Parameter parameter, CancellationToken cancellationToken)
        => dbContext.Parameters.AddAsync(parameter, cancellationToken).AsTask();

    /// <summary>
    /// Удаляет параметр испытания.
    /// </summary>
    public Task DeleteParameterAsync(Parameter parameter, CancellationToken cancellationToken)
    {
        dbContext.Parameters.Remove(parameter);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Сохраняет изменения во всех справочниках.
    /// </summary>
    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
