namespace Bmz.LabTests.LoadTests.Models;

/// <summary>
/// Запрос на авторизацию (логин).
/// </summary>
public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на создание нового протокола испытаний.
/// </summary>
public sealed class CreateTestResultRequest
{
    public int WireCodeId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
}

/// <summary>
/// Запрос на сохранение/обновление значений измерений в протоколе.
/// Использует RowVersion для оптимистичной блокировки.
/// </summary>
public sealed class SaveTestValuesRequest
{
    public string RowVersion { get; set; } = string.Empty;
    public List<TestValueItemRequest> Values { get; set; } = [];
}

/// <summary>
/// Одиночное значение измерения для сохранения.
/// </summary>
public sealed class TestValueItemRequest
{
    public int ParameterId { get; set; }
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на завершение испытаний и закрытие протокола.
/// </summary>
public sealed class CompleteTestResultRequest
{
    public string RowVersion { get; set; } = string.Empty;
}
