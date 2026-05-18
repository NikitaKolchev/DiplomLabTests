namespace Bmz.LabTests.LoadTests;

public static class LoadTestConfig
{
    public const string BaseUrl = "http://localhost:5287";
    public const string AdminLogin = "asst-mech-1";
    public const string AdminPassword = "VeryHardPassword";

    // Префикс для идентификации тестовых данных в БД
    public const string TestDataPrefix = "LOAD-";

    // Строка подключения к БД для очистки после теста
    public const string ConnectionString =
        "Server=localhost\\SQLEXPRESS;Database=BmzLabTestsDb;Trusted_Connection=True;TrustServerCertificate=True";

    // Длительности — менять здесь для разных прогонов
    public static readonly TimeSpan WarmupDuration = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan TestDuration = TimeSpan.FromMinutes(3);
    public static readonly TimeSpan HeavyTestDuration = TimeSpan.FromMinutes(5);
}
