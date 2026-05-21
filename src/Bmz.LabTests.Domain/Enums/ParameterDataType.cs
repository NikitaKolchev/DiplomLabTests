namespace Bmz.LabTests.Domain.Enums;

/// <summary>
/// Тип данных значения параметра испытания.
/// </summary>
public enum ParameterDataType
{
    /// <summary>Числовой параметр (Decimal). Позволяет проводить валидацию по границам Min/Max.</summary>
    Number = 1,

    /// <summary>Текстовый параметр (String). Свободный ввод.</summary>
    Text = 2
}
