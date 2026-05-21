using Bmz.LabTests.LoadTests.Http;
using Bmz.LabTests.LoadTests.Models;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Bmz.LabTests.LoadTests.Scenarios;

/// <summary>
/// Сценарий "Просмотр журнала".
/// Моделирует поведение пользователя (мастера/инженера), который просматривает список протоколов и заходит в детали конкретного протокола.
/// </summary>
public static class ReadScenario
{
    /// <summary>
    /// Создает конфигурацию сценария для NBomber.
    /// Модель нагрузки: KeepConstant (20 параллельных пользователей).
    /// </summary>
    public static ScenarioProps Build(HttpClient httpClient, string token)
    {
        return Scenario.Create("ПросмотрЖурнала", async context =>
        {
            // Шаг 1: Получение списка протоколов с имитацией пагинации и сортировки пользователем.
            var listStep = await Step.Run<PaginatedListDto<TestResultListItemDto>>("Список протоколов", context, async () =>
            {
                var page = Random.Shared.Next(1, 51);
                var sortBy = Random.Shared.Next(0, 6);
                var sortDesc = Random.Shared.Next(0, 2) == 0;

                var url = LoadTestEndpoints.TestResultsList(page, pageSize: 20, sortBy: sortBy, sortDesc: sortDesc);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                NbomberHttpHelper.SetBearer(request, token);

                return await NbomberHttpHelper.SendJsonAsync<PaginatedListDto<TestResultListItemDto>>(httpClient, request, CancellationToken.None);
            });

            if (listStep.IsError)
                return (IResponse)listStep;

            var pageDto = listStep.Payload.Value;
            if (pageDto == null || pageDto.Items.Count == 0)
            {
                // Если база пуста — это не ошибка API, а состояние данных
                return Response.Ok(statusCode: "пусто");
            }

            // Шаг 2: Выбор случайного протокола из полученного списка для детального просмотра.
            var randomItem = pageDto.Items[Random.Shared.Next(0, pageDto.Items.Count)];

            // Шаг 3: Получение детальной информации по выбранному протоколу (GET /api/testresults/{id}).
            var detailsStep = await Step.Run<TestResultDetailsDto>("Детали протокола", context, async () =>
            {
                var url = LoadTestEndpoints.TestResultById(randomItem.Id);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                NbomberHttpHelper.SetBearer(request, token);

                return await NbomberHttpHelper.SendJsonAsync<TestResultDetailsDto>(httpClient, request, CancellationToken.None);
            });

            return (IResponse)detailsStep;
        })
        .WithWarmUpDuration(LoadTestConfig.WarmupDuration)
        .WithLoadSimulations(Simulation.KeepConstant(copies: 20, during: LoadTestConfig.HeavyTestDuration));
    }
}
