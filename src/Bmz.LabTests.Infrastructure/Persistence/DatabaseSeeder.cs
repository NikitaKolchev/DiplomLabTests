using Bmz.LabTests.Application.Abstractions.Persistence;
using Bmz.LabTests.Infrastructure.Persistence.Seeding;
using Bmz.LabTests.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Persistence;

/// <summary>
/// Главный сервис инициализации базы данных.
/// Выполняет автоматическое применение миграций и последовательный запуск всех наполнителей (seeders) начальными данными.
/// </summary>
public sealed class DatabaseSeeder(
    ApplicationDbContext context,
    IEnumerable<IEntitySeeder> seeders) : IDatabaseSeeder
{
    /// <summary>
    /// Запускает процесс миграции и наполнения БД.
    /// Гарантирует, что БД находится в актуальном состоянии перед началом работы приложения.
    /// </summary>
    public async Task<Result> SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.Database.MigrateAsync(cancellationToken);

            foreach (var seeder in seeders.OrderBy(x => x.Order))
            {
                var result = await seeder.SeedAsync(context, cancellationToken);
                if (result.IsFailure)
                    return result;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка при миграции или наполнении БД: {ex.Message}");
        }
    }
}