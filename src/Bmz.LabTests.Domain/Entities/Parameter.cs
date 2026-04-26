using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Domain.Entities;

public sealed class Parameter : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ParameterDataType DataType { get; set; }

    public string? Unit { get; set; }

    public ICollection<WireCodeLimit> Limits { get; set; } = new List<WireCodeLimit>();
}
