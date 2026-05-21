namespace Bmz.LabTests.Domain.Common;

/// <summary>
/// Перечисление типов ошибок для более точной обработки в слое API (маппинг на HTTP статусы).
/// </summary>
public enum ErrorType
{
    None = 0,
    Failure = 1,
    Validation = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5,
    Forbidden = 6
}

/// <summary>
/// Объект ошибки, содержащий тип и текстовое описание.
/// Используется в связке с классом Result для функциональной обработки ошибок без исключений.
/// </summary>
public record Error(ErrorType Type, string Message)
{
    public static Error None => new(ErrorType.None, string.Empty);
    public static Error Failure(string message) => new(ErrorType.Failure, message);
    public static Error Validation(string message) => new(ErrorType.Validation, message);
    public static Error NotFound(string message) => new(ErrorType.NotFound, message);
    public static Error Conflict(string message) => new(ErrorType.Conflict, message);
    public static Error Unauthorized(string message) => new(ErrorType.Unauthorized, message);
    public static Error Forbidden(string message) => new(ErrorType.Forbidden, message);

    public static implicit operator string(Error error) => error.Message;
}
