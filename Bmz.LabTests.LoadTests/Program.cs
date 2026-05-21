using Bmz.LabTests.LoadTests.Http;
using Bmz.LabTests.LoadTests.Scenarios;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Bmz.LabTests.LoadTests;

/// <summary>
/// Главный класс программы для запуска нагрузочного тестирования.
/// Использует NBomber для моделирования нагрузки на API Bmz.LabTests.
/// </summary>
public static class Program
{
    /// <summary>
    /// Точка входа в программу.
    /// Выполняет авторизацию, настраивает сценарии, запускает тесты и очищает данные.
    /// </summary>
    /// <param name="args">Аргументы командной строки.</param>
    public static async Task Main(string[] args)
    {
        // Создаем HttpClient, который будет переиспользоваться во всех сценариях
        using var httpClient = new HttpClient();

        Console.WriteLine("Получение JWT токена...");
        // Получаем JWT токен один раз при старте для всех сценариев
        var token = await TokenProvider.GetTokenAsync(httpClient);
        Console.WriteLine("Токен получен. Запуск нагрузочных тестов...");

        // Инициализация сценариев тестирования
        var readScenario = ReadScenario.Build(httpClient, token);
        var writeScenario = WriteScenario.Build(httpClient, token);
        var updateScenario = UpdateScenario.Build(httpClient, token);
        var fullCycleScenario = FullCycleScenario.Build(httpClient, token);

        // Регистрация и запуск сценариев через NBomberRunner
        var result = NBomberRunner
            .RegisterScenarios(readScenario, writeScenario, updateScenario, fullCycleScenario)
            .WithReportFolder("./load-test-reports")
            .Run(args);

        // Вывод краткой статистики в консоль
        PrintSummary(result, ["ПросмотрЖурнала", "СозданиеПротокола", "ВнесениеДанных", "ПолныйЦикл"]);

        Console.WriteLine("\nЗапуск очистки тестовых данных...");
        try
        {
            // Очистка базы данных от созданных во время теста записей (с префиксом LOAD-)
            var cleanup = await TestDataCleaner.CleanAsync();
            Console.WriteLine($"Очистка завершена: удалено {cleanup.DeletedTestResults} протоколов, {cleanup.DeletedTestValues} значений измерений, {cleanup.DeletedFinalProducts} продуктов, {cleanup.DeletedRejects} брака");
        }
        catch (Exception ex)
        {
            // В случае ошибки выводим инструкции по ручной очистке
            Console.WriteLine($"\nПРЕДУПРЕЖДЕНИЕ: Очистка не удалась. Сообщение: {ex.Message}");
            Console.WriteLine("Выполните SQL вручную для очистки:");
            Console.WriteLine("DELETE FROM TestValues WHERE TestResultId IN (SELECT Id FROM TestResults WHERE BatchNumber LIKE 'LOAD-%');");
            Console.WriteLine("DELETE FROM FinalProducts WHERE TestResultId IN (SELECT Id FROM TestResults WHERE BatchNumber LIKE 'LOAD-%');");
            Console.WriteLine("DELETE FROM Rejects WHERE TestResultId IN (SELECT Id FROM TestResults WHERE BatchNumber LIKE 'LOAD-%');");
            Console.WriteLine("DELETE FROM TestResults WHERE BatchNumber LIKE 'LOAD-%';");
        }
    }

    /// <summary>
    /// Выводит сводную таблицу результатов тестирования в консоль.
    /// </summary>
    /// <param name="stats">Статистика от NBomber.</param>
    /// <param name="scenarioNames">Список имен сценариев для вывода.</param>
    private static void PrintSummary(NBomber.Contracts.Stats.NodeStats stats, string[] scenarioNames)
    {
        Console.WriteLine("\nИтоговая таблица:");
        Console.WriteLine("сценарий | RPS | p50 | p95 | p99 | Процент ошибок");
        Console.WriteLine("--------------------------------------------------");

        foreach (var name in scenarioNames)
        {
            var scenario = stats.ScenarioStats.FirstOrDefault(x => x.ScenarioName == name);
            if (scenario != null)
            {
                var rps = scenario.Ok.Request.RPS;
                var p50 = scenario.Ok.Latency.Percent50;
                var p95 = scenario.Ok.Latency.Percent95;
                var p99 = scenario.Ok.Latency.Percent99;
                var errorRate = scenario.Fail.Request.Count > 0
                    ? (double)scenario.Fail.Request.Count / (scenario.Ok.Request.Count + scenario.Fail.Request.Count) * 100
                    : 0;

                Console.WriteLine($"{name} | {rps:F1} | {p50} | {p95} | {p99} | {errorRate:F2} %");
            }
        }
    }
}
