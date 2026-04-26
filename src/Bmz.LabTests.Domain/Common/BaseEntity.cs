namespace Bmz.LabTests.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
