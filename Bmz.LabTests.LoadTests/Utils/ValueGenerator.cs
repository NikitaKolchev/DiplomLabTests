using Bmz.LabTests.LoadTests.Models;

namespace Bmz.LabTests.LoadTests.Utils;

/// <summary>
/// Утилита для генерации случайных, но валидных данных для протоколов испытаний.
/// </summary>
public static class ValueGenerator
{
    /// <summary>
    /// Формирует список значений измерений на основе предоставленной схемы полей.
    /// </summary>
    /// <param name="fields">Список полей (параметров) с их ограничениями.</param>
    /// <returns>Список готовых к отправке в API значений.</returns>
    public static List<TestValueItemRequest> BuildValuesForSchema(IReadOnlyCollection<InputFieldDto> fields)
    {
        var result = new List<TestValueItemRequest>(fields.Count);

        foreach (var field in fields)
        {
            var value = CreateValue(field);
            result.Add(new TestValueItemRequest { ParameterId = field.ParameterId, Value = value });
        }

        return result;
    }

    /// <summary>
    /// Генерирует случайное значение для конкретного поля с учетом его типа и диапазона (Min/Max).
    /// </summary>
    private static string CreateValue(InputFieldDto field)
    {
        // DataType == 1 означает числовое значение (Decimal)
        if (field.DataType == 1)
        {
            var min = field.MinValue ?? 1m;
            var max = field.MaxValue ?? (min + 100m);
            
            // Защита от некорректно настроенных границ в БД
            if (max < min)
            {
                (min, max) = (max, min);
            }

            var roll = Random.Shared.NextDouble();
            var value = min + (decimal)roll * (max - min);
            // Округляем до 3 знаков для реалистичности и соответствия формату БД
            return decimal.Round(value, 3).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        // Для строковых или булевых типов (в данной версии — заглушка "ok")
        return "ok";
    }
}
