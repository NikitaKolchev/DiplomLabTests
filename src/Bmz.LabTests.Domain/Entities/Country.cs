using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Domain.Entities;

public sealed class Country : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}
