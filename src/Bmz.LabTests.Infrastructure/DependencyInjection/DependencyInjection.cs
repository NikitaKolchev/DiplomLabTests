using Bmz.LabTests.Application.Abstractions;
using Bmz.LabTests.Application.Abstractions.Audit;
using Bmz.LabTests.Application.Abstractions.Auth;
using Bmz.LabTests.Application.Abstractions.DataGeneration;
using Bmz.LabTests.Application.Abstractions.Persistence;
using Bmz.LabTests.Application.Abstractions.Products;
using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Application.Abstractions.Reporting;
using Bmz.LabTests.Application.Abstractions.Testing;
using Bmz.LabTests.Infrastructure.Audit;
using Bmz.LabTests.Infrastructure.Auth;
using Bmz.LabTests.Infrastructure.DataGeneration;
using Bmz.LabTests.Infrastructure.Persistence;
using Bmz.LabTests.Infrastructure.Persistence.Seeding;
using Bmz.LabTests.Infrastructure.Products;
using Bmz.LabTests.Infrastructure.Repositories;
using Bmz.LabTests.Infrastructure.Reporting;
using Bmz.LabTests.Infrastructure.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bmz.LabTests.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<LdapOptions>(configuration.GetSection(LdapOptions.SectionName));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ILdapService, LdapService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
        services.AddScoped<IProtocolRepository, ProtocolRepository>();
        services.AddScoped<ITestResultRepository, TestResultRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ITestResultCompletionService, TestResultCompletionService>();
        services.AddScoped<IDataGeneratorService, DataGeneratorService>();
        services.AddSingleton<IPasswordVerifier, PasswordVerifier>();
        
        services.AddScoped<IEntitySeeder, RoleSeeder>();
        services.AddScoped<IEntitySeeder, AdminSeeder>();
        services.AddScoped<IEntitySeeder, DemoDataSeeder>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        return services;
    }
}
