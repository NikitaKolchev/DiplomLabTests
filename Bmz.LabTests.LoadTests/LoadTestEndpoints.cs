namespace Bmz.LabTests.LoadTests;

public static class LoadTestEndpoints
{
    public static string AuthLogin()
        => $"{LoadTestConfig.BaseUrl}/api/auth/login";

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

    public static string TestResultById(int id)
        => $"{LoadTestConfig.BaseUrl}/api/testresults/{id}";

    public static string TestResultValues(int id)
        => $"{LoadTestConfig.BaseUrl}/api/testresults/{id}/values";

    public static string TestResultComplete(int id)
        => $"{LoadTestConfig.BaseUrl}/api/testresults/{id}/complete";

    public static string WireCodes()
        => $"{LoadTestConfig.BaseUrl}/api/wirecodes";

    public static string Customers()
        => $"{LoadTestConfig.BaseUrl}/api/customers";

    public static string Parameters()
        => $"{LoadTestConfig.BaseUrl}/api/parameters";

    public static string InputFields(int wireCodeId)
        => $"{LoadTestConfig.BaseUrl}/api/wire-codes/{wireCodeId}/input-fields";
}
