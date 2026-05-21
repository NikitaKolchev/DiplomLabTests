using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Domain.Entities;

/// <summary>
/// Сущность, определяющая нормы (допустимые границы) для конкретного параметра в рамках шифра проволоки.
/// </summary>
public sealed class WireCodeLimit : BaseEntity
{
    public int WireCodeId { get; set; }

    public WireCode WireCode { get; set; } = null!;

    public int ParameterId { get; set; }

    public Parameter Parameter { get; set; } = null!;

    public decimal? MinValue { get; set; }

    public decimal? MaxValue { get; set; }

    public bool IsRequired { get; set; } = true;
}
