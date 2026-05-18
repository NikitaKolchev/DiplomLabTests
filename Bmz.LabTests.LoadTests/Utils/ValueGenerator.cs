using Bmz.LabTests.LoadTests.Models;

namespace Bmz.LabTests.LoadTests.Utils;

public static class ValueGenerator
{
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

    private static string CreateValue(InputFieldDto field)
    {
        if (field.DataType == 1)
        {
            var min = field.MinValue ?? 1m;
            var max = field.MaxValue ?? (min + 100m);
            if (max < min)
            {
                (min, max) = (max, min);
            }

            var roll = Random.Shared.NextDouble();
            var value = min + (decimal)roll * (max - min);
            return decimal.Round(value, 3).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return "ok";
    }
}
