namespace Bmz.LabTests.LoadTests.Models;

/// <summary>
/// Универсальный DTO для пагинированных списков.
/// </summary>
public sealed class PaginatedListDto<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Элемент списка протоколов в журнале испытаний.
/// </summary>
public sealed class TestResultListItemDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public object? Status { get; set; }
    public int WireCodeId { get; set; }
    public string WireCode { get; set; } = string.Empty;
    public string Assistant { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
}

/// <summary>
/// Значение измерения для конкретного параметра.
/// </summary>
public sealed class TestResultValueDto
{
    public int ParameterId { get; set; }
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Детальная информация о протоколе испытаний.
/// </summary>
public sealed class TestResultDetailsDto
{
    public int Id { get; set; }
    public int WireCodeId { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public object? Status { get; set; }
    public List<TestResultValueDto> Values { get; set; } = [];
}

/// <summary>
/// Ответ API при успешном создании протокола.
/// </summary>
public sealed class CreatedTestResultDto
{
    public int Id { get; set; }
    public int WireCodeId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public object? Status { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

/// <summary>
/// Ответ API при сохранении значений измерений.
/// </summary>
public sealed class SavedTestResultDto
{
    public int Id { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

/// <summary>
/// Справочник шифров проволоки.
/// </summary>
public sealed class WireCodeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Справочник заказчиков.
/// </summary>
public sealed class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Описание параметра испытания.
/// </summary>
public sealed class ParameterDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DataType { get; set; }
    public string? Unit { get; set; }
}

/// <summary>
/// Описание поля ввода для конкретного шифра.
/// </summary>
public sealed class InputFieldDto
{
    public int ParameterId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public int DataType { get; set; }
    public string? Unit { get; set; }
    public bool IsRequired { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
}

/// <summary>
/// Схема необходимых полей ввода для шифра проволоки.
/// </summary>
public sealed class WireCodeInputSchemaDto
{
    public List<InputFieldDto> Fields { get; set; } = [];
}

/// <summary>
/// Ответ API на запрос авторизации.
/// </summary>
public sealed class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
}
