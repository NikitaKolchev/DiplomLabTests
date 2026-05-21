using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Application.Abstractions.Testing;
using Bmz.LabTests.Application.Testing;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Bmz.LabTests.Infrastructure.Testing;

/// <summary>
/// Реализация сервиса завершения испытаний.
/// Выполняет финальную валидацию фактических данных на соответствие техническим условиям (нормам).
/// </summary>
public sealed class TestResultCompletionService(ITestResultRepository repository) : ITestResultCompletionService
{
    /// <summary>
    /// Асинхронно завершает испытание.
    /// Сравнивает введенные значения с нормами и автоматически распределяет результат в "Готовую продукцию" или "Брак".
    /// </summary>
    public async Task<Result<CompletionResult>> CompleteAsync(int testResultId, byte[] rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Загружаем протокол со всеми значениями
            var testResult = await repository.GetByIdWithValuesAsync(testResultId, cancellationToken);

            if (testResult is null)
            {
                return Result.Failure<CompletionResult>("Результат испытания не найден.");
            }

            if (testResult.Status == TestResultStatus.Completed)
            {
                return Result.Failure<CompletionResult>("Результат испытания уже завершен.");
            }

            // 2. Устанавливаем RowVersion для проверки конкурентного доступа
            repository.SetOriginalRowVersion(testResult, rowVersion);

            // 3. Загружаем нормы (лимиты) для данного типа продукции (WireCode)
            var limits = await repository.GetLimitsByWireCodeIdAsync(testResult.WireCodeId, cancellationToken);

            // 4. Выполняем валидацию всех параметров
            var valueLookup = testResult.Values.ToDictionary(x => x.ParameterId, x => x.Value);
            var issues = Validate(limits, valueLookup);

            // 5. Переводим протокол в статус Completed (замораживаем изменения)
            testResult.Complete();

            // 6. Формируем результат: если ошибок нет — партия годна, иначе — брак
            if (issues.Count == 0)
            {
                await repository.AddFinalProductAsync(new FinalProduct(testResultId), cancellationToken);
            }
            else
            {
                var reason = string.Join("; ", issues);
                await repository.AddRejectAsync(new Reject(testResultId, reason), cancellationToken);
            }

            // 7. Сохраняем все изменения в одной транзакции
            await repository.SaveChangesAsync(cancellationToken);

            return Result.Success(new CompletionResult
            {
                IsAccepted = issues.Count == 0,
                RejectReason = issues.Count == 0 ? null : string.Join("; ", issues)
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            // Обработка случая, когда RowVersion в БД не совпадает с переданным (данные устарели)
            return Result.Failure<CompletionResult>("Данные были изменены другим пользователем. Пожалуйста, обновите страницу.");
        }
        catch (Exception ex)
        {
            return Result.Failure<CompletionResult>($"Ошибка при завершении испытания: {ex.Message}");
        }
    }

    /// <summary>
    /// Внутренняя логика валидации значений против установленных норм.
    /// Проверяет обязательность заполнения, корректность числовых форматов и вхождение в диапазоны [Min, Max].
    /// </summary>
    private static List<string> Validate(IReadOnlyCollection<WireCodeLimit> limits, IReadOnlyDictionary<int, string> values)
    {
        var issues = new List<string>();
        var culture = CultureInfo.InvariantCulture;

        foreach (var limit in limits)
        {
            values.TryGetValue(limit.ParameterId, out var rawValue);
            var hasValue = !string.IsNullOrWhiteSpace(rawValue);

            // Проверка обязательности
            if (limit.IsRequired && !hasValue)
            {
                issues.Add($"{limit.Parameter.Name}: значение обязательно.");
                continue;
            }

            if (!hasValue)
            {
                continue;
            }

            // Валидация числовых значений
            if (limit.Parameter.DataType != ParameterDataType.Number)
            {
                continue;
            }

            // Приведение к единому формату разделителя (точка)
            var normalized = rawValue!.Trim().Replace(',', '.');
            if (!decimal.TryParse(normalized, NumberStyles.Number, culture, out var numericValue))
            {
                issues.Add($"{limit.Parameter.Name}: '{rawValue}' не является корректным числом.");
                continue;
            }

            // Проверка на выход за нижнюю границу
            if (limit.MinValue.HasValue && numericValue < limit.MinValue.Value)
            {
                issues.Add($"{limit.Parameter.Name}: {numericValue} меньше {limit.MinValue.Value}.");
            }

            // Проверка на выход за верхнюю границу
            if (limit.MaxValue.HasValue && numericValue > limit.MaxValue.Value)
            {
                issues.Add($"{limit.Parameter.Name}: {numericValue} больше {limit.MaxValue.Value}.");
            }
        }

        return issues;
    }
}
