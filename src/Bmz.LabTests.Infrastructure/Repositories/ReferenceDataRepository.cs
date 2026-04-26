using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Repositories;

public sealed class ReferenceDataRepository(ApplicationDbContext dbContext) : IReferenceDataRepository
{
    public Task<List<Country>> GetCountriesAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = dbContext.Countries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Name.Contains(searchTerm));
        }

        return query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Country?> GetCountryByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.Countries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddCountryAsync(Country country, CancellationToken cancellationToken)
        => dbContext.Countries.AddAsync(country, cancellationToken).AsTask();

    public Task DeleteCountryAsync(Country country, CancellationToken cancellationToken)
    {
        dbContext.Countries.Remove(country);
        return Task.CompletedTask;
    }

    public Task<List<Customer>> GetCustomersAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = dbContext.Customers.AsNoTracking().Include(x => x.Country);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Name.Contains(searchTerm));
        }

        return query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.Customers.Include(x => x.Country).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken)
        => dbContext.Customers.AddAsync(customer, cancellationToken).AsTask();

    public Task DeleteCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        dbContext.Customers.Remove(customer);
        return Task.CompletedTask;
    }

    public Task<bool> CountryExistsAsync(int countryId, CancellationToken cancellationToken)
        => dbContext.Countries.AnyAsync(x => x.Id == countryId, cancellationToken);

    public Task<List<WireCode>> GetWireCodesAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = dbContext.WireCodes.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Code.Contains(searchTerm) || x.Marking.Contains(searchTerm));
        }

        return query.OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public Task<WireCode?> GetWireCodeByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.WireCodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddWireCodeAsync(WireCode wireCode, CancellationToken cancellationToken)
        => dbContext.WireCodes.AddAsync(wireCode, cancellationToken).AsTask();

    public Task DeleteWireCodeAsync(WireCode wireCode, CancellationToken cancellationToken)
    {
        dbContext.WireCodes.Remove(wireCode);
        return Task.CompletedTask;
    }

    public Task<bool> WireCodeExistsByCodeAsync(string code, int? excludingId, CancellationToken cancellationToken)
        => dbContext.WireCodes.AnyAsync(x => x.Code == code && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);

    public Task<List<Parameter>> GetParametersAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var query = dbContext.Parameters.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.Name.Contains(searchTerm));
        }

        return query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<Parameter?> GetParameterByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.Parameters.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddParameterAsync(Parameter parameter, CancellationToken cancellationToken)
        => dbContext.Parameters.AddAsync(parameter, cancellationToken).AsTask();

    public Task DeleteParameterAsync(Parameter parameter, CancellationToken cancellationToken)
    {
        dbContext.Parameters.Remove(parameter);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
