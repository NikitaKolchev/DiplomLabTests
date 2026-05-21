using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Domain.Entities;

/// <summary>
/// Сущность заказчика (организации), для которой проводятся испытания.
/// </summary>
public sealed class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public int? CountryId { get; set; }

    public Country? Country { get; set; }
}
