using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bmz.LabTests.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();

        const string connectionString = "Server=localhost\\SQLEXPRESS;Database=BmzLabTestsDb;Trusted_Connection=True;TrustServerCertificate=True";
        builder.UseSqlServer(connectionString);

        return new ApplicationDbContext(builder.Options);
    }
}
