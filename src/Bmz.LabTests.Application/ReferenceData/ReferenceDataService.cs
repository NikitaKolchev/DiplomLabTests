using Bmz.LabTests.Application.Abstractions.ReferenceData;
using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Application.ReferenceData;

public sealed class ReferenceDataService(IReferenceDataRepository repository) : IReferenceDataService
{
    public async Task<Result<IReadOnlyCollection<CountryDto>>> GetCountriesAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var countries = await repository.GetCountriesAsync(searchTerm, cancellationToken);
        var dtos = countries.Select(x => new CountryDto(x.Id, x.Name)).ToArray();
        return Result.Success<IReadOnlyCollection<CountryDto>>(dtos);
    }

    public async Task<Result<CountryDto>> GetCountryByIdAsync(int id, CancellationToken cancellationToken)
    {
        var country = await repository.GetCountryByIdAsync(id, cancellationToken);
        if (country is null)
        {
            return Result.Failure<CountryDto>("Страна не найдена.");
        }

        return Result.Success(new CountryDto(country.Id, country.Name));
    }

    public async Task<Result<CountryDto>> CreateCountryAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            var country = new Country { Name = name.Trim() };
            await repository.AddCountryAsync(country, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Success(new CountryDto(country.Id, country.Name));
        }
        catch (Exception ex)
        {
            return Result.Failure<CountryDto>($"Ошибка при создании страны: {ex.Message}");
        }
    }

    public async Task<Result<CountryDto>> UpdateCountryAsync(int id, string name, CancellationToken cancellationToken)
    {
        try
        {
            var country = await repository.GetCountryByIdAsync(id, cancellationToken);
            if (country is null)
            {
                return Result.Failure<CountryDto>("Страна не найдена.");
            }

            country.Name = name.Trim();
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Success(new CountryDto(country.Id, country.Name));
        }
        catch (Exception ex)
        {
            return Result.Failure<CountryDto>($"Ошибка при обновлении страны: {ex.Message}");
        }
    }

    public async Task<Result> DeleteCountryAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var country = await repository.GetCountryByIdAsync(id, cancellationToken);
            if (country is null)
            {
                return Result.Failure("Страна не найдена.");
            }

            await repository.DeleteCountryAsync(country, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка при удалении страны: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyCollection<CustomerDto>>> GetCustomersAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var customers = await repository.GetCustomersAsync(searchTerm, cancellationToken);
        var dtos = customers.Select(x => new CustomerDto(x.Id, x.Name, x.CountryId, x.Country?.Name)).ToArray();
        return Result.Success<IReadOnlyCollection<CustomerDto>>(dtos);
    }

    public async Task<Result<CustomerDto>> GetCustomerByIdAsync(int id, CancellationToken cancellationToken)
    {
        var customer = await repository.GetCustomerByIdAsync(id, cancellationToken);
        if (customer is null)
        {
            return Result.Failure<CustomerDto>("Потребитель не найден.");
        }

        return Result.Success(new CustomerDto(customer.Id, customer.Name, customer.CountryId, customer.Country?.Name));
    }

    public async Task<Result<CustomerDto>> CreateCustomerAsync(string name, int? countryId, CancellationToken cancellationToken)
    {
        try
        {
            if (countryId.HasValue && !await repository.CountryExistsAsync(countryId.Value, cancellationToken))
            {
                return Result.Failure<CustomerDto>("Страна не найдена.");
            }

            var customer = new Customer
            {
                Name = name.Trim(),
                CountryId = countryId
            };

            await repository.AddCustomerAsync(customer, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            var loaded = await repository.GetCustomerByIdAsync(customer.Id, cancellationToken);
            if (loaded is null)
            {
                return Result.Failure<CustomerDto>("Не удалось загрузить потребителя после создания.");
            }

            return Result.Success(new CustomerDto(loaded.Id, loaded.Name, loaded.CountryId, loaded.Country?.Name));
        }
        catch (Exception ex)
        {
            return Result.Failure<CustomerDto>($"Ошибка при создании потребителя: {ex.Message}");
        }
    }

    public async Task<Result<CustomerDto>> UpdateCustomerAsync(int id, string name, int? countryId, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await repository.GetCustomerByIdAsync(id, cancellationToken);
            if (customer is null)
            {
                return Result.Failure<CustomerDto>("Потребитель не найден.");
            }

            if (countryId.HasValue && !await repository.CountryExistsAsync(countryId.Value, cancellationToken))
            {
                return Result.Failure<CustomerDto>("Страна не найдена.");
            }

            customer.Name = name.Trim();
            customer.CountryId = countryId;
            await repository.SaveChangesAsync(cancellationToken);

            var loaded = await repository.GetCustomerByIdAsync(customer.Id, cancellationToken);
            if (loaded is null)
            {
                return Result.Failure<CustomerDto>("Не удалось загрузить потребителя после обновления.");
            }

            return Result.Success(new CustomerDto(loaded.Id, loaded.Name, loaded.CountryId, loaded.Country?.Name));
        }
        catch (Exception ex)
        {
            return Result.Failure<CustomerDto>($"Ошибка при обновлении потребителя: {ex.Message}");
        }
    }

    public async Task<Result> DeleteCustomerAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await repository.GetCustomerByIdAsync(id, cancellationToken);
            if (customer is null)
            {
                return Result.Failure("Потребитель не найден.");
            }

            await repository.DeleteCustomerAsync(customer, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка при удалении потребителя: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyCollection<WireCodeDto>>> GetWireCodesAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var wireCodes = await repository.GetWireCodesAsync(searchTerm, cancellationToken);
        var dtos = wireCodes.Select(x => new WireCodeDto(x.Id, x.Code, x.Marking, x.Diameter)).ToArray();
        return Result.Success<IReadOnlyCollection<WireCodeDto>>(dtos);
    }

    public async Task<Result<WireCodeDto>> GetWireCodeByIdAsync(int id, CancellationToken cancellationToken)
    {
        var wireCode = await repository.GetWireCodeByIdAsync(id, cancellationToken);
        if (wireCode is null)
        {
            return Result.Failure<WireCodeDto>("Код проволоки не найден.");
        }

        return Result.Success(new WireCodeDto(wireCode.Id, wireCode.Code, wireCode.Marking, wireCode.Diameter));
    }

    public async Task<Result<WireCodeDto>> CreateWireCodeAsync(string code, string marking, decimal diameter, CancellationToken cancellationToken)
    {
        try
        {
            var normalized = code.Trim();
            if (await repository.WireCodeExistsByCodeAsync(normalized, null, cancellationToken))
            {
                return Result.Failure<WireCodeDto>("Код проволоки уже существует.");
            }

            var wireCode = new WireCode
            {
                Code = normalized,
                Marking = marking.Trim(),
                Diameter = diameter
            };

            await repository.AddWireCodeAsync(wireCode, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Success(new WireCodeDto(wireCode.Id, wireCode.Code, wireCode.Marking, wireCode.Diameter));
        }
        catch (Exception ex)
        {
            return Result.Failure<WireCodeDto>($"Ошибка при создании кода проволоки: {ex.Message}");
        }
    }

    public async Task<Result<WireCodeDto>> UpdateWireCodeAsync(int id, string code, string marking, decimal diameter, CancellationToken cancellationToken)
    {
        try
        {
            var wireCode = await repository.GetWireCodeByIdAsync(id, cancellationToken);
            if (wireCode is null)
            {
                return Result.Failure<WireCodeDto>("Код проволоки не найден.");
            }

            var normalized = code.Trim();
            if (await repository.WireCodeExistsByCodeAsync(normalized, id, cancellationToken))
            {
                return Result.Failure<WireCodeDto>("Код проволоки уже существует.");
            }

            wireCode.Code = normalized;
            wireCode.Marking = marking.Trim();
            wireCode.Diameter = diameter;
            await repository.SaveChangesAsync(cancellationToken);

            return Result.Success(new WireCodeDto(wireCode.Id, wireCode.Code, wireCode.Marking, wireCode.Diameter));
        }
        catch (Exception ex)
        {
            return Result.Failure<WireCodeDto>($"Ошибка при обновлении кода проволоки: {ex.Message}");
        }
    }

    public async Task<Result> DeleteWireCodeAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var wireCode = await repository.GetWireCodeByIdAsync(id, cancellationToken);
            if (wireCode is null)
            {
                return Result.Failure("Код проволоки не найден.");
            }

            await repository.DeleteWireCodeAsync(wireCode, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка при удалении кода проволоки: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyCollection<ParameterDto>>> GetParametersAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        var parameters = await repository.GetParametersAsync(searchTerm, cancellationToken);
        var dtos = parameters.Select(x => new ParameterDto(x.Id, x.Name, x.DataType, x.Unit)).ToArray();
        return Result.Success<IReadOnlyCollection<ParameterDto>>(dtos);
    }

    public async Task<Result<ParameterDto>> GetParameterByIdAsync(int id, CancellationToken cancellationToken)
    {
        var parameter = await repository.GetParameterByIdAsync(id, cancellationToken);
        if (parameter is null)
        {
            return Result.Failure<ParameterDto>("Параметр не найден.");
        }

        return Result.Success(new ParameterDto(parameter.Id, parameter.Name, parameter.DataType, parameter.Unit));
    }

    public async Task<Result<ParameterDto>> CreateParameterAsync(string name, ParameterDataType dataType, string? unit, CancellationToken cancellationToken)
    {
        try
        {
            var parameter = new Parameter
            {
                Name = name.Trim(),
                DataType = dataType,
                Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim()
            };

            await repository.AddParameterAsync(parameter, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Success(new ParameterDto(parameter.Id, parameter.Name, parameter.DataType, parameter.Unit));
        }
        catch (Exception ex)
        {
            return Result.Failure<ParameterDto>($"Ошибка при создании параметра: {ex.Message}");
        }
    }

    public async Task<Result<ParameterDto>> UpdateParameterAsync(int id, string name, ParameterDataType dataType, string? unit, CancellationToken cancellationToken)
    {
        try
        {
            var parameter = await repository.GetParameterByIdAsync(id, cancellationToken);
            if (parameter is null)
            {
                return Result.Failure<ParameterDto>("Параметр не найден.");
            }

            parameter.Name = name.Trim();
            parameter.DataType = dataType;
            parameter.Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
            await repository.SaveChangesAsync(cancellationToken);

            return Result.Success(new ParameterDto(parameter.Id, parameter.Name, parameter.DataType, parameter.Unit));
        }
        catch (Exception ex)
        {
            return Result.Failure<ParameterDto>($"Ошибка при обновлении параметра: {ex.Message}");
        }
    }

    public async Task<Result> DeleteParameterAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var parameter = await repository.GetParameterByIdAsync(id, cancellationToken);
            if (parameter is null)
            {
                return Result.Failure("Параметр не найден.");
            }

            await repository.DeleteParameterAsync(parameter, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка при удалении параметра: {ex.Message}");
        }
    }
}
