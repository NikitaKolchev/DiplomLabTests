using Bmz.LabTests.LoadTests.Http;
using Bmz.LabTests.LoadTests.Models;
using Bmz.LabTests.LoadTests.Utils;
using NBomber.Contracts;
using NBomber.CSharp;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bmz.LabTests.LoadTests.Scenarios;

/// <summary>
/// Сценарий "Полный цикл".
/// Моделирует сквозной процесс: создание протокола -> получение параметров -> внесение данных -> завершение испытания.
/// Это самый тяжелый сценарий, максимально нагружающий бизнес-логику и базу данных.
/// </summary>
public static class FullCycleScenario
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Создает конфигурацию сценария.
    /// Модель нагрузки: Inject (2 новых пользователя в секунду).
    /// </summary>
    public static ScenarioProps Build(HttpClient httpClient, string token)
    {
        return Scenario.Create("ПолныйЦикл", async context =>
        {
            // Шаг 1: Получение справочника шифров для начала работы.
            var wireCodesStep = await Step.Run<List<WireCodeDto>>("Справочник кодов", context, async () =>
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

            var wireCode = wireCodes[Random.Shared.Next(0, wireCodes.Count)];

            // Имитация времени на раздумья пользователя
            await Task.Delay(500);

            // Шаг 2: Создание нового протокола испытаний.
            var createStep = await Step.Run<CreatedTestResultDto>("Создание протокола", context, async () =>
            {
                var dto = new CreateTestResultRequest
                {
                    WireCodeId = wireCode.Id,
                    CustomerId = null,
                    BatchNumber = $"{LoadTestConfig.TestDataPrefix}{DateTime.UtcNow.Ticks}-{context.InvocationNumber}"
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{LoadTestConfig.BaseUrl}/api/testresults")
                {
                    Content = JsonContent.Create(dto, options: JsonOptions)
                };
                NbomberHttpHelper.SetBearer(request, token);

                return await NbomberHttpHelper.SendJsonAsync<CreatedTestResultDto>(httpClient, request, CancellationToken.None);
            });

            if (createStep.IsError)
                return (IResponse)createStep;

            var created = createStep.Payload.Value;

            await Task.Delay(500);

            // Шаг 3: Получение деталей только что созданного протокола для получения RowVersion.
            var detailsStep = await Step.Run<TestResultDetailsDto>("Детали протокола", context, async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, LoadTestEndpoints.TestResultById(created.Id));
                NbomberHttpHelper.SetBearer(request, token);
                return await NbomberHttpHelper.SendJsonAsync<TestResultDetailsDto>(httpClient, request, CancellationToken.None);
            });

            if (detailsStep.IsError)
                return (IResponse)detailsStep;

            var details = detailsStep.Payload.Value;

            await Task.Delay(500);

            // Шаг 4: Получение справочника всех параметров.
            var parametersStep = await Step.Run<List<ParameterDto>>("Параметры", context, async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, LoadTestEndpoints.Parameters());
                NbomberHttpHelper.SetBearer(request, token);
                return await NbomberHttpHelper.SendJsonAsync<List<ParameterDto>>(httpClient, request, CancellationToken.None);
            });

            if (parametersStep.IsError)
                return (IResponse)parametersStep;

            await Task.Delay(500);

            // Дополнительный шаг: Получение схемы полей ввода для генерации корректных данных.
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

            await Task.Delay(500);

            // Шаг 5: Внесение значений измерений (PUT /api/testresults/{id}/values).
            var saveStep = await Step.Run<SavedTestResultDto>("Внесение значений", context, async () =>
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

                return await NbomberHttpHelper.SendJsonAsync<SavedTestResultDto>(httpClient, request, CancellationToken.None);
            });

            if (saveStep.IsError)
                return (IResponse)saveStep;

            var saved = saveStep.Payload.Value;

            await Task.Delay(500);

            // Шаг 6: Завершение протокола (перевод в статус Completed).
            var completeStep = await Step.Run<object>("Завершение протокола", context, async () =>
            {
                var completePayload = new CompleteTestResultRequest { RowVersion = saved.RowVersion };
                var request = new HttpRequestMessage(HttpMethod.Post, LoadTestEndpoints.TestResultComplete(details.Id))
                {
                    Content = JsonContent.Create(completePayload, options: JsonOptions)
                };
                NbomberHttpHelper.SetBearer(request, token);

                return await NbomberHttpHelper.SendAsync(httpClient, request, CancellationToken.None);
            });

            if (completeStep.IsError)
                return (IResponse)completeStep;

            return (IResponse)completeStep;
        })
        .WithLoadSimulations(Simulation.Inject(rate: 2, interval: TimeSpan.FromSeconds(1), during: LoadTestConfig.TestDuration));
    }
}
