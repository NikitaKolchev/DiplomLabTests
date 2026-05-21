using Bmz.LabTests.LoadTests.Http;
using Bmz.LabTests.LoadTests.Models;
using Bmz.LabTests.LoadTests.Utils;
using NBomber.Contracts;
using NBomber.CSharp;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bmz.LabTests.LoadTests.Scenarios;

/// <summary>
/// Сценарий "Внесение данных".
/// Моделирует работу лаборанта, который выбирает протоколы со статусом InProgress и вносит в них результаты измерений.
/// Учитывает оптимистичную блокировку через RowVersion.
/// </summary>
public static class UpdateScenario
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Создает конфигурацию сценария.
    /// Модель нагрузки: Inject (5 новых запросов в секунду).
    /// </summary>
    public static ScenarioProps Build(HttpClient httpClient, string token)
    {
        return Scenario.Create("ВнесениеДанных", async context =>
        {
            // Шаг 1: Получение списка протоколов, которые сейчас находятся в статусе "В работе".
            var inProgressStep = await Step.Run<PaginatedListDto<TestResultListItemDto>>("Протоколы в работе", context, async () =>
            {
                var url = LoadTestEndpoints.TestResultsList(page: 1, pageSize: 50, status: "InProgress");
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                NbomberHttpHelper.SetBearer(request, token);

                return await NbomberHttpHelper.SendJsonAsync<PaginatedListDto<TestResultListItemDto>>(httpClient, request, CancellationToken.None);
            });

            if (inProgressStep.IsError)
                return (IResponse)inProgressStep;

            var listDto = inProgressStep.Payload.Value;
            if (listDto == null || listDto.Items.Count == 0)
                return Response.Ok(statusCode: "нет-в-работе");

            // Шаг 2: Выбор случайного протокола для обновления.
            var picked = listDto.Items[Random.Shared.Next(0, listDto.Items.Count)];

            // Шаг 3: Получение текущего состояния протокола и актуального RowVersion (обязательно для PUT).
            var detailsStep = await Step.Run<TestResultDetailsDto>("Получение деталей", context, async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, LoadTestEndpoints.TestResultById(picked.Id));
                NbomberHttpHelper.SetBearer(request, token);
                return await NbomberHttpHelper.SendJsonAsync<TestResultDetailsDto>(httpClient, request, CancellationToken.None);
            });

            if (detailsStep.IsError)
                return (IResponse)detailsStep;

            var details = detailsStep.Payload.Value;

            // Шаг 4: Получение справочника параметров (для информации).
            var parametersStep = await Step.Run<List<ParameterDto>>("Список параметров", context, async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, LoadTestEndpoints.Parameters());
                NbomberHttpHelper.SetBearer(request, token);
                return await NbomberHttpHelper.SendJsonAsync<List<ParameterDto>>(httpClient, request, CancellationToken.None);
            });

            if (parametersStep.IsError)
                return (IResponse)parametersStep;

            // Дополнительный шаг: Получение схемы полей ввода для конкретного шифра проволоки.
            var schemaStep = await Step.Run<WireCodeInputSchemaDto>("Схема ввода", context, async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, LoadTestEndpoints.InputFields(details.WireCodeId));
                NbomberHttpHelper.SetBearer(request, token);
                return await NbomberHttpHelper.SendJsonAsync<WireCodeInputSchemaDto>(httpClient, request, CancellationToken.None);
            });

            if (schemaStep.IsError)
                return (IResponse)schemaStep;

            var schema = schemaStep.Payload.Value;
            if (schema == null || schema.Fields.Count == 0)
                return Response.Ok(statusCode: "нет-схемы");

            // Шаг 5: Сохранение сгенерированных случайных значений (PUT /api/testresults/{id}/values).
            var saveStep = await Step.Run<object>("Сохранение значений", context, async () =>
            {
                var payload = new SaveTestValuesRequest
                {
                    RowVersion = details.RowVersion,
                    Values = ValueGenerator.BuildValuesForSchema(schema.Fields)
                };

                var request = new HttpRequestMessage(HttpMethod.Put, LoadTestEndpoints.TestResultValues(details.Id))
                {
                    Content = JsonContent.Create(payload, options: JsonOptions)
                };
                NbomberHttpHelper.SetBearer(request, token);

                // Используем специальный хелпер, который не считает HTTP 409 (конфликт версий) ошибкой.
                return await NbomberHttpHelper.SendAsyncTreat409AsOk(httpClient, request, CancellationToken.None);
            });

            if (saveStep.IsError)
                return (IResponse)saveStep;

            return (IResponse)saveStep;
        })
        .WithLoadSimulations(Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: LoadTestConfig.TestDuration));
    }
}
