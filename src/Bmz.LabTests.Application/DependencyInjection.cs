using Bmz.LabTests.Application.Abstractions.Auth;
using Bmz.LabTests.Application.Abstractions.Organization;
using Bmz.LabTests.Application.Abstractions.Protocol;
using Bmz.LabTests.Application.Abstractions.ReferenceData;
using Bmz.LabTests.Application.Abstractions.TestResults;
using Bmz.LabTests.Application.Auth;
using Bmz.LabTests.Application.Organization;
using Bmz.LabTests.Application.Protocol;
using Bmz.LabTests.Application.ReferenceData;
using Bmz.LabTests.Application.TestResults;
using Microsoft.Extensions.DependencyInjection;

namespace Bmz.LabTests.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IIdentityProvider, IdentityProvider>();
        services.AddScoped<IReferenceDataService, ReferenceDataService>();
        services.AddScoped<IProtocolService, ProtocolService>();
        services.AddScoped<ITestResultService, TestResultService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<ILaboratoryService, LaboratoryService>();
        return services;
    }
}
