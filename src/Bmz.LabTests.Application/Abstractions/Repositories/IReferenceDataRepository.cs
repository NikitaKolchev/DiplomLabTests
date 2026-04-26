using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Application.Abstractions.Repositories;

public interface IReferenceDataRepository
{
    Task<List<Country>> GetCountriesAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<Country?> GetCountryByIdAsync(int id, CancellationToken cancellationToken);
    Task AddCountryAsync(Country country, CancellationToken cancellationToken);
    Task DeleteCountryAsync(Country country, CancellationToken cancellationToken);

    Task<List<Customer>> GetCustomersAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken);
    Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken);
    Task DeleteCustomerAsync(Customer customer, CancellationToken cancellationToken);
    Task<bool> CountryExistsAsync(int countryId, CancellationToken cancellationToken);

    Task<List<WireCode>> GetWireCodesAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<WireCode?> GetWireCodeByIdAsync(int id, CancellationToken cancellationToken);
    Task AddWireCodeAsync(WireCode wireCode, CancellationToken cancellationToken);
    Task DeleteWireCodeAsync(WireCode wireCode, CancellationToken cancellationToken);
    Task<bool> WireCodeExistsByCodeAsync(string code, int? excludingId, CancellationToken cancellationToken);

    Task<List<Parameter>> GetParametersAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<Parameter?> GetParameterByIdAsync(int id, CancellationToken cancellationToken);
    Task AddParameterAsync(Parameter parameter, CancellationToken cancellationToken);
    Task DeleteParameterAsync(Parameter parameter, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
