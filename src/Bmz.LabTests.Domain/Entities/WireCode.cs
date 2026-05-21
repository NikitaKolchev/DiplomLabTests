using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Domain.Entities;

/// <summary>
/// Сущность шифра проволоки (марка стали и типоразмер).
/// </summary>
public sealed class WireCode : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Marking { get; set; } = string.Empty;

    public decimal Diameter { get; set; }

    public ICollection<WireCodeLimit> Limits { get; set; } = new List<WireCodeLimit>();
}
