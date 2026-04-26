using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Application.ReferenceData;

public sealed record CountryDto(int Id, string Name);

public sealed record CustomerDto(int Id, string Name, int? CountryId, string? CountryName);

public sealed record WireCodeDto(int Id, string Code, string Marking, decimal Diameter);

public sealed record ParameterDto(int Id, string Name, ParameterDataType DataType, string? Unit);
