using Microsoft.Data.SqlClient;

namespace Bmz.LabTests.LoadTests;

public static class TestDataCleaner
{
    public sealed class CleanupResult
    {
        public int DeletedTestValues { get; init; }
        public int DeletedFinalProducts { get; init; }
        public int DeletedRejects { get; init; }
        public int DeletedTestResults { get; init; }
    }

    public static async Task<CleanupResult> CleanAsync(CancellationToken cancellationToken = default)
    {
        var prefixLike = $"{LoadTestConfig.TestDataPrefix}%";

        await using var connection = new SqlConnection(LoadTestConfig.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var deletedTestValues = await ExecuteAsync(connection, @"
DELETE FROM TestValues
WHERE TestResultId IN (
    SELECT Id FROM TestResults
    WHERE BatchNumber LIKE @prefix
);", prefixLike, cancellationToken);

        var deletedFinalProducts = await ExecuteAsync(connection, @"
DELETE FROM FinalProducts
WHERE TestResultId IN (
    SELECT Id FROM TestResults
    WHERE BatchNumber LIKE @prefix
);", prefixLike, cancellationToken);

        var deletedRejects = await ExecuteAsync(connection, @"
DELETE FROM Rejects
WHERE TestResultId IN (
    SELECT Id FROM TestResults
    WHERE BatchNumber LIKE @prefix
);", prefixLike, cancellationToken);

        var deletedTestResults = await ExecuteAsync(connection, @"
DELETE FROM TestResults
WHERE BatchNumber LIKE @prefix;", prefixLike, cancellationToken);

        return new CleanupResult
        {
            DeletedTestValues = deletedTestValues,
            DeletedFinalProducts = deletedFinalProducts,
            DeletedRejects = deletedRejects,
            DeletedTestResults = deletedTestResults
        };
    }

    public static string[] GetManualSql()
    {
        var prefix = $"{LoadTestConfig.TestDataPrefix}%";
        return
        [
            $"DELETE FROM TestValues WHERE TestResultId IN (SELECT Id FROM TestResults WHERE BatchNumber LIKE '{prefix}');",
            $"DELETE FROM FinalProducts WHERE TestResultId IN (SELECT Id FROM TestResults WHERE BatchNumber LIKE '{prefix}');",
            $"DELETE FROM Rejects WHERE TestResultId IN (SELECT Id FROM TestResults WHERE BatchNumber LIKE '{prefix}');",
            $"DELETE FROM TestResults WHERE BatchNumber LIKE '{prefix}';"
        ];
    }

    private static async Task<int> ExecuteAsync(SqlConnection connection, string sql, string prefixLike, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@prefix", prefixLike);
        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
