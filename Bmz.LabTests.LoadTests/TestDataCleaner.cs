using Microsoft.Data.SqlClient;

namespace Bmz.LabTests.LoadTests;

/// <summary>
/// Класс для очистки базы данных от тестовых данных, созданных в процессе нагрузочного тестирования.
/// Использует прямое подключение к БД через ADO.NET для эффективности.
/// </summary>
public static class TestDataCleaner
{
    /// <summary>
    /// Результаты операции очистки.
    /// </summary>
    public sealed class CleanupResult
    {
        /// <summary>Количество удаленных значений измерений.</summary>
        public int DeletedTestValues { get; init; }
        /// <summary>Количество удаленных записей о готовой продукции.</summary>
        public int DeletedFinalProducts { get; init; }
        /// <summary>Количество удаленных записей о браке.</summary>
        public int DeletedRejects { get; init; }
        /// <summary>Количество удаленных протоколов испытаний.</summary>
        public int DeletedTestResults { get; init; }
    }

    /// <summary>
    /// Выполняет асинхронную очистку всех таблиц, связанных с протоколами, имеющими тестовый префикс.
    /// Порядок удаления важен из-за внешних ключей (FK).
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Статистика удаленных записей.</returns>
    public static async Task<CleanupResult> CleanAsync(CancellationToken cancellationToken = default)
    {
        var prefixLike = $"{LoadTestConfig.TestDataPrefix}%";

        await using var connection = new SqlConnection(LoadTestConfig.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        // 1. Удаляем связанные значения измерений
        var deletedTestValues = await ExecuteAsync(connection, @"
DELETE FROM TestValues
WHERE TestResultId IN (
    SELECT Id FROM TestResults
    WHERE BatchNumber LIKE @prefix
);", prefixLike, cancellationToken);

        // 2. Удаляем связанные записи о готовой продукции
        var deletedFinalProducts = await ExecuteAsync(connection, @"
DELETE FROM FinalProducts
WHERE TestResultId IN (
    SELECT Id FROM TestResults
    WHERE BatchNumber LIKE @prefix
);", prefixLike, cancellationToken);

        // 3. Удаляем связанные записи о браке
        var deletedRejects = await ExecuteAsync(connection, @"
DELETE FROM Rejects
WHERE TestResultId IN (
    SELECT Id FROM TestResults
    WHERE BatchNumber LIKE @prefix
);", prefixLike, cancellationToken);

        // 4. В последнюю очередь удаляем сами протоколы
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

    /// <summary>
    /// Возвращает массив SQL-запросов для ручного выполнения очистки.
    /// </summary>
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

    /// <summary>
    /// Вспомогательный метод для выполнения SQL-команды с параметром префикса.
    /// </summary>
    private static async Task<int> ExecuteAsync(SqlConnection connection, string sql, string prefixLike, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@prefix", prefixLike);
        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
