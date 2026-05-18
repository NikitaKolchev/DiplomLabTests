using Bmz.LabTests.LoadTests.Http;
using Bmz.LabTests.LoadTests.Models;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Bmz.LabTests.LoadTests.Scenarios;

public static class ReadScenario
{
    public static ScenarioProps Build(HttpClient httpClient, string token)
    {
        return Scenario.Create("ПросмотрЖурнала", async context =>
        {
            // Шаг 1: GET /api/testresults с параметрами пагинации и случайной сортировкой.
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
                return Response.Ok(statusCode: "пусто");
            }

            // Шаг 2: из полученного списка взять случайный id протокола.
            var randomItem = pageDto.Items[Random.Shared.Next(0, pageDto.Items.Count)];

            // Шаг 3: GET /api/testresults/{id} — получить детали протокола.
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
