using Bmz.LabTests.LoadTests.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bmz.LabTests.LoadTests.Http;

/// <summary>
/// Провайдер для получения JWT токена доступа.
/// </summary>
public static class TokenProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Выполняет вход в систему и возвращает JWT токен.
    /// Использует учетные данные из <see cref="LoadTestConfig"/>.
    /// </summary>
    /// <param name="httpClient">HttpClient для выполнения запроса.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Строка с JWT токеном.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если авторизация не удалась.</exception>
    public static async Task<string> GetTokenAsync(HttpClient httpClient, CancellationToken cancellationToken = default)
    {
        var request = new LoginRequest
        {
            Username = LoadTestConfig.AdminLogin,
            Password = LoadTestConfig.AdminPassword
        };

        using var response = await httpClient.PostAsJsonAsync(LoadTestEndpoints.AuthLogin(), request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadBodyAsync(response, cancellationToken);
            throw new InvalidOperationException($"Авторизация не прошла. HTTP {(int)response.StatusCode}. Ответ: {body}");
        }

        var payload = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions, cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.Token))
        {
            throw new InvalidOperationException("Авторизация прошла, но токен не найден в ответе.");
        }

        return payload.Token;
    }

    /// <summary>
    /// Безопасное чтение тела ответа для вывода в исключение.
    /// </summary>
    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return "<не удалось прочитать тело ответа>";
        }
    }
}
