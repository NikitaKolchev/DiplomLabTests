using Bmz.LabTests.API.Contracts.Customers;
using FluentValidation;

namespace Bmz.LabTests.API.Validators.Customers;

public sealed class UpsertCustomerRequestValidator : AbstractValidator<UpsertCustomerRequest>
{
    public UpsertCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.CountryId).GreaterThan(0).When(x => x.CountryId.HasValue);
    }
}
