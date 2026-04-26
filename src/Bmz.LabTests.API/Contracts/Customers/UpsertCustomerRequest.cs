namespace Bmz.LabTests.API.Contracts.Customers;

public sealed class UpsertCustomerRequest
{
    public string Name { get; set; } = string.Empty;

    public int? CountryId { get; set; }
}
