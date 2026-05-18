using Bmz.LabTests.LoadTests.Http;
using Bmz.LabTests.LoadTests.Models;
using NBomber.Contracts;
using NBomber.CSharp;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bmz.LabTests.LoadTests.Scenarios;

public static class WriteScenario
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static ScenarioProps Build(HttpClient httpClient, string token)
    {
        return Scenario.Create("СозданиеПротокола", async context =>
        {
            // Шаг 1: получить список доступных кодов проволоки.
            var wireCodesStep = await Step.Run<List<WireCodeDto>>("Список кодов проволоки", context, async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, LoadTestEndpoints.WireCodes());
                NbomberHttpHelper.SetBearer(request, token);
                return await NbomberHttpHelper.SendJsonAsync<List<WireCodeDto>>(httpClient, request, CancellationToken.None);
            });

            if (wireCodesStep.IsError)
                return (IResponse)wireCodesStep;

            var wireCodes = wireCodesStep.Payload.Value;
            if (wireCodes == null || wireCodes.Count == 0)
                return Response.Ok(statusCode: "нет-кодов");

            // Шаг 2: получить список заказчиков.
            var customersStep = await Step.Run<List<CustomerDto>>("Список заказчиков", context, async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, LoadTestEndpoints.Customers());
                NbomberHttpHelper.SetBearer(request, token);
                return await NbomberHttpHelper.SendJsonAsync<List<CustomerDto>>(httpClient, request, CancellationToken.None);
            });

            if (customersStep.IsError)
                return (IResponse)customersStep;

            var customers = customersStep.Payload.Value;
            var wireCode = wireCodes[Random.Shared.Next(0, wireCodes.Count)];
            var customerId = (customers == null || customers.Count == 0) ? (int?)null : customers[Random.Shared.Next(0, customers.Count)].Id;

            // Шаг 3: создать новый протокол.
            var createStep = await Step.Run<CreatedTestResultDto>("Создание протокола", context, async () =>
            {
                var dto = new CreateTestResultRequest
                {
                    WireCodeId = wireCode.Id,
                    CustomerId = customerId,
                    BatchNumber = $"{LoadTestConfig.TestDataPrefix}{DateTime.UtcNow.Ticks}-{context.InvocationNumber}"
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{LoadTestConfig.BaseUrl}/api/testresults")
                {
                    Content = JsonContent.Create(dto, options: JsonOptions)
                };
                NbomberHttpHelper.SetBearer(request, token);

                return await NbomberHttpHelper.SendJsonAsync<CreatedTestResultDto>(httpClient, request, CancellationToken.None);
            });

            return (IResponse)createStep;
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(15))
        .WithLoadSimulations(Simulation.Inject(rate: 3, interval: TimeSpan.FromSeconds(1), during: LoadTestConfig.TestDuration));
    }
}
