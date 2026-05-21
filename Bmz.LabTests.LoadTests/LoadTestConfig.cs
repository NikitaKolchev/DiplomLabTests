namespace Bmz.LabTests.LoadTests;

/// <summary>
/// Глобальная конфигурация для нагрузочных тестов.
/// Содержит параметры подключения к API и базе данных, учетные данные и временные интервалы.
/// </summary>
public static class LoadTestConfig
{
    /// <summary>Базовый URL развернутого API.</summary>
    public const string BaseUrl = "http://localhost:5287";

    /// <summary>Логин пользователя с правами Assistant для выполнения операций.</summary>
    public const string AdminLogin = "asst-mech-1";

    /// <summary>Пароль пользователя.</summary>
    public const string AdminPassword = "VeryHardPassword";

    /// <summary>Префикс для BatchNumber, позволяющий идентифицировать данные, созданные тестом.</summary>
    public const string TestDataPrefix = "LOAD-";

    /// <summary>Строка подключения к SQL Server для прямой очистки данных после завершения тестов.</summary>
    public const string ConnectionString =
        "Server=localhost\\SQLEXPRESS;Database=BmzLabTestsDb;Trusted_Connection=True;TrustServerCertificate=True";

    /// <summary>Длительность фазы "прогрева" (warmup) перед основным замером.</summary>
    public static readonly TimeSpan WarmupDuration = TimeSpan.FromSeconds(30);

    /// <summary>Стандартная длительность выполнения сценария.</summary>
    public static readonly TimeSpan TestDuration = TimeSpan.FromMinutes(3);

    /// <summary>Увеличенная длительность для тяжелых сценариев (например, ПросмотрЖурнала).</summary>
    public static readonly TimeSpan HeavyTestDuration = TimeSpan.FromMinutes(5);
}
