namespace Bmz.LabTests.Domain.Common;

/// <summary>
/// Шаблон "Result" для обработки результатов операций без использования исключений для ожидаемых ошибок.
/// Позволяет явно вернуть успех или ошибку из доменного слоя или слоя приложения.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Успешный результат не может содержать ошибку.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Неуспешный результат должен содержать ошибку.");

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Признак успешного завершения операции.</summary>
    public bool IsSuccess { get; }

    /// <summary>Признак того, что операция завершилась ошибкой.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Объект ошибки (доступен только при IsFailure = true).</summary>
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(string message) => new(false, Error.Failure(message));

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
    public static Result<T> Failure<T>(string message) => Result<T>.Failure(Error.Failure(message));
}

/// <summary>
/// Обобщенная версия Result, содержащая возвращаемое значение типа T.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Возвращаемое значение. Выбрасывает исключение, если попытаться получить значение при ошибке.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Нельзя получить значение неуспешного результата.");

    public static Result<T> Success(T value) => new(value, true, Error.None);
    public static new Result<T> Failure(Error error) => new(default, false, error);
    public static new Result<T> Failure(string message) => new(default, false, Error.Failure(message));

    public static implicit operator Result<T>(T value) => Success(value);
}