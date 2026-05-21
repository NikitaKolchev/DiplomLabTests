namespace Bmz.LabTests.LoadTests;

/// <summary>
/// Статический класс для формирования URL-адресов эндпоинтов API.
/// </summary>
public static class LoadTestEndpoints
{
    /// <summary>Эндпоинт для авторизации.</summary>
    public static string AuthLogin()
        => $"{LoadTestConfig.BaseUrl}/api/auth/login";

    /// <summary>Получение списка протоколов испытаний с пагинацией и фильтрацией.</summary>
    public static string TestResultsList(int page, int pageSize, int? sortBy = null, bool? sortDesc = null, string? status = null)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (sortBy.HasValue) query.Add($"sortBy={sortBy.Value}");
        if (sortDesc.HasValue) query.Add($"sortDesc={sortDesc.Value.ToString()!.ToLowerInvariant()}");
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");

        return $"{LoadTestConfig.BaseUrl}/api/testresults?{string.Join("&", query)}";
    }

    /// <summary>Получение деталей протокола по ID.</summary>
    public static string TestResultById(int id)
        => $"{LoadTestConfig.BaseUrl}/api/testresults/{id}";

    /// <summary>Внесение/изменение значений измерений для протокола.</summary>
    public static string TestResultValues(int id)
        => $"{LoadTestConfig.BaseUrl}/api/testresults/{id}/values";

    /// <summary>Завершение испытания для протокола.</summary>
    public static string TestResultComplete(int id)
        => $"{LoadTestConfig.BaseUrl}/api/testresults/{id}/complete";

    /// <summary>Получение списка шифров проволоки (марки стали).</summary>
    public static string WireCodes()
        => $"{LoadTestConfig.BaseUrl}/api/wirecodes";

    /// <summary>Получение списка заказчиков.</summary>
    public static string Customers()
        => $"{LoadTestConfig.BaseUrl}/api/customers";

    /// <summary>Получение списка всех параметров испытаний.</summary>
    public static string Parameters()
        => $"{LoadTestConfig.BaseUrl}/api/parameters";

    /// <summary>Получение необходимых полей ввода для конкретного шифра проволоки.</summary>
    public static string InputFields(int wireCodeId)
        => $"{LoadTestConfig.BaseUrl}/api/wirecodes/{wireCodeId}/input-fields";
}
