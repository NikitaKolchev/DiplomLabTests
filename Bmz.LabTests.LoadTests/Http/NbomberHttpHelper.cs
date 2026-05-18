using NBomber.Contracts;
using NBomber.CSharp;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bmz.LabTests.LoadTests.Http;

public static class NbomberHttpHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static void SetBearer(HttpRequestMessage request, string token)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<Response<T>> SendJsonAsync<T>(HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var statusCode = (int)response.StatusCode;

            if (statusCode is >= 200 and < 300)
            {
                var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                if (payload is null)
                {
                    return Response.Fail<T>(statusCode: statusCode.ToString(), message: "Пустой JSON в успешном ответе.");
                }

                var sizeBytes = await TryGetResponseSizeAsync(response, cancellationToken);
                return Response.Ok(payload: payload, sizeBytes: (int)sizeBytes, statusCode: statusCode.ToString());
            }

            var body = await SafeReadBodyAsync(response, cancellationToken);
            return Response.Fail<T>(statusCode: statusCode.ToString(), message: body);
        }
        catch (Exception ex)
        {
            return Response.Fail<T>(message: ex.Message);
        }
    }

    public static async Task<Response<object>> SendAsync(HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var statusCode = (int)response.StatusCode;

            if (statusCode is >= 200 and < 300)
            {
                var sizeBytes = await TryGetResponseSizeAsync(response, cancellationToken);
                return Response.Ok<object>(sizeBytes: (int)sizeBytes, statusCode: statusCode.ToString());
            }

            var body = await SafeReadBodyAsync(response, cancellationToken);
            return Response.Fail<object>(statusCode: statusCode.ToString(), message: body);
        }
        catch (Exception ex)
        {
            return Response.Fail<object>(message: ex.Message);
        }
    }

    public static async Task<Response<object>> SendAsyncTreat409AsOk(HttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var statusCode = (int)response.StatusCode;

            if (statusCode == 409)
            {
                return Response.Ok<object>(statusCode: "conflict");
            }

            if (statusCode is >= 200 and < 300)
            {
                var sizeBytes = await TryGetResponseSizeAsync(response, cancellationToken);
                return Response.Ok<object>(sizeBytes: (int)sizeBytes, statusCode: statusCode.ToString());
            }

            var body = await SafeReadBodyAsync(response, cancellationToken);
            return Response.Fail<object>(statusCode: statusCode.ToString(), message: body);
        }
        catch (Exception ex)
        {
            return Response.Fail<object>(message: ex.Message);
        }
    }

    private static async Task<long> TryGetResponseSizeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            if (response.Content.Headers.ContentLength.HasValue)
                return response.Content.Headers.ContentLength.Value;

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return bytes.LongLength;
        }
        catch
        {
            return 0;
        }
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(body) ? $"HTTP {(int)response.StatusCode}" : body;
        }
        catch
        {
            return $"HTTP {(int)response.StatusCode}";
        }
    }
}
