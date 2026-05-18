using Bmz.LabTests.LoadTests.Http;
using Bmz.LabTests.LoadTests.Scenarios;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Bmz.LabTests.LoadTests;

public static class Program
{
    public static async Task Main(string[] args)
    {
        using var httpClient = new HttpClient();

        Console.WriteLine("Получение JWT токена...");
        var token = await TokenProvider.GetTokenAsync(httpClient);
        Console.WriteLine("Токен получен. Запуск нагрузочных тестов...");

        var readScenario = ReadScenario.Build(httpClient, token);
        var writeScenario = WriteScenario.Build(httpClient, token);
        var updateScenario = UpdateScenario.Build(httpClient, token);
        var fullCycleScenario = FullCycleScenario.Build(httpClient, token);

        var result = NBomberRunner
            .RegisterScenarios(readScenario, writeScenario, updateScenario, fullCycleScenario)
            .WithReportFolder("./load-test-reports")
            .Run(args);

        PrintSummary(result, ["ПросмотрЖурнала", "СозданиеПротокола", "ВнесениеДанных", "ПолныйЦикл"]);

        Console.WriteLine("\nЗапуск очистки тестовых данных...");
        try
        {            var cleanup = await TestDataCleaner.CleanAsync();
            Console.WriteLine($"Очистка завершена: удалено {cleanup.DeletedTestResults} протоколов, {cleanup.DeletedTestValues} значений измерений, {cleanup.DeletedFinalProducts} продуктов, {cleanup.DeletedRejects} брака");
        }
        catch (Exception ex)
        {            Console.WriteLine($"\nПРЕДУПРЕЖДЕНИЕ: Очистка не удалась. Сообщение: {ex.Message}");
            Console.WriteLine("Выполните SQL вручную для очистки:");
            Console.WriteLine("DELETE FROM TestValues WHERE TestResultId IN (SELECT Id FROM TestResults WHERE BatchNumber LIKE 'LOAD-%');");
            Console.WriteLine("DELETE FROM FinalProducts WHERE TestResultId IN (SELECT Id FROM TestResults WHERE BatchNumber LIKE 'LOAD-%');");
            Console.WriteLine("DELETE FROM Rejects WHERE TestResultId IN (SELECT Id FROM TestResults WHERE BatchNumber LIKE 'LOAD-%');");
            Console.WriteLine("DELETE FROM TestResults WHERE BatchNumber LIKE 'LOAD-%';");
        }
    }

    private static void PrintSummary(NBomber.Contracts.Stats.NodeStats stats, string[] scenarioNames)
    {        Console.WriteLine("\nИтоговая таблица:");
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
