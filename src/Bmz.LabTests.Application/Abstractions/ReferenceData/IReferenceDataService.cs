using Bmz.LabTests.Domain.Enums;
using Bmz.LabTests.Application.ReferenceData;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.ReferenceData;

public interface IReferenceDataService
{
    Task<Result<IReadOnlyCollection<CountryDto>>> GetCountriesAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<Result<CountryDto>> GetCountryByIdAsync(int id, CancellationToken cancellationToken);
    Task<Result<CountryDto>> CreateCountryAsync(string name, CancellationToken cancellationToken);
    Task<Result<CountryDto>> UpdateCountryAsync(int id, string name, CancellationToken cancellationToken);
    Task<Result> DeleteCountryAsync(int id, CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<CustomerDto>>> GetCustomersAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<Result<CustomerDto>> GetCustomerByIdAsync(int id, CancellationToken cancellationToken);
    Task<Result<CustomerDto>> CreateCustomerAsync(string name, int? countryId, CancellationToken cancellationToken);
    Task<Result<CustomerDto>> UpdateCustomerAsync(int id, string name, int? countryId, CancellationToken cancellationToken);
    Task<Result> DeleteCustomerAsync(int id, CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<WireCodeDto>>> GetWireCodesAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<Result<WireCodeDto>> GetWireCodeByIdAsync(int id, CancellationToken cancellationToken);
    Task<Result<WireCodeDto>> CreateWireCodeAsync(string code, string marking, decimal diameter, CancellationToken cancellationToken);
    Task<Result<WireCodeDto>> UpdateWireCodeAsync(int id, string code, string marking, decimal diameter, CancellationToken cancellationToken);
    Task<Result> DeleteWireCodeAsync(int id, CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<ParameterDto>>> GetParametersAsync(string? searchTerm, CancellationToken cancellationToken);
    Task<Result<ParameterDto>> GetParameterByIdAsync(int id, CancellationToken cancellationToken);
    Task<Result<ParameterDto>> CreateParameterAsync(string name, ParameterDataType dataType, string? unit, CancellationToken cancellationToken);
    Task<Result<ParameterDto>> UpdateParameterAsync(int id, string name, ParameterDataType dataType, string? unit, CancellationToken cancellationToken);
    Task<Result> DeleteParameterAsync(int id, CancellationToken cancellationToken);
}
