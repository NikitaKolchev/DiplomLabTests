using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Persistence;

/// <summary>
/// Основной контекст базы данных приложения.
/// Отвечает за маппинг доменных сущностей на таблицы SQL Server и настройку связей между ними.
/// </summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Laboratory> Laboratories => Set<Laboratory>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<WireCode> WireCodes => Set<WireCode>();
    public DbSet<Parameter> Parameters => Set<Parameter>();
    public DbSet<WireCodeLimit> Limits => Set<WireCodeLimit>();
    public DbSet<TestResult> TestResults => Set<TestResult>();
    public DbSet<TestValue> TestValues => Set<TestValue>();
    public DbSet<FinalProduct> FinalProducts => Set<FinalProduct>();
    public DbSet<Reject> Rejects => Set<Reject>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <summary>
    /// Настройка моделей и связей Fluent API.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Глобальная настройка RowVersion для всех сущностей, наследующих BaseEntity
        ConfigureRowVersion(modelBuilder);

        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.Property(x => x.Name).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.Sid).HasMaxLength(256).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(256).IsRequired();
            e.Property(x => x.Login).HasMaxLength(128).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(512);
            e.HasIndex(x => x.Login).IsUnique();
            // Запрет удаления роли, если есть привязанные пользователи
            e.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            // При удалении лаборатории пользователь остается, но поле LaboratoryId зануляется
            e.HasOne(x => x.Laboratory).WithMany(x => x.Users).HasForeignKey(x => x.LaboratoryId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Laboratory>(e =>
        {
            e.ToTable("Laboratories");
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Country>(e =>
        {
            e.ToTable("Countries");
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
            e.HasOne(x => x.Country).WithMany().HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WireCode>(e =>
        {
            e.ToTable("WireCodes");
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Marking).HasMaxLength(128).IsRequired();
            e.Property(x => x.Diameter).HasPrecision(18, 3);
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Parameter>(e =>
        {
            e.ToTable("Parameters");
            e.Property(x => x.Name).HasMaxLength(128).IsRequired();
            e.Property(x => x.Unit).HasMaxLength(16);
        });

        modelBuilder.Entity<WireCodeLimit>(e =>
        {
            e.ToTable("Limits");
            e.Property(x => x.MinValue).HasPrecision(18, 4);
            e.Property(x => x.MaxValue).HasPrecision(18, 4);
            e.Property(x => x.IsRequired).HasDefaultValue(true);
            // Уникальный индекс: один параметр для одного шифра настраивается только один раз
            e.HasIndex(x => new { x.WireCodeId, x.ParameterId }).IsUnique();
            e.HasOne(x => x.WireCode).WithMany(x => x.Limits).HasForeignKey(x => x.WireCodeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Parameter).WithMany(x => x.Limits).HasForeignKey(x => x.ParameterId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TestResult>(e =>
        {
            e.ToTable("TestResults");
            e.Property(x => x.BatchNumber).HasMaxLength(128).IsRequired();
            e.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.Assistant).WithMany(x => x.TestResults).HasForeignKey(x => x.AssistantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.WireCode).WithMany().HasForeignKey(x => x.WireCodeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Laboratory).WithMany().HasForeignKey(x => x.LaboratoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.Date);
        });

        modelBuilder.Entity<TestValue>(e =>
        {
            e.ToTable("TestValues");
            e.Property(x => x.Value).HasMaxLength(512).IsRequired();
            e.HasOne(x => x.TestResult).WithMany(x => x.Values).HasForeignKey(x => x.TestResultId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Parameter).WithMany().HasForeignKey(x => x.ParameterId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinalProduct>(e =>
        {
            e.ToTable("FinalProducts");
            e.HasIndex(x => x.TestResultId).IsUnique();
            e.HasOne(x => x.TestResult).WithOne(x => x.FinalProduct).HasForeignKey<FinalProduct>(x => x.TestResultId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Reject>(e =>
        {
            e.ToTable("Rejects");
            e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            e.HasIndex(x => x.TestResultId).IsUnique();
            e.HasOne(x => x.TestResult).WithOne(x => x.Reject).HasForeignKey<Reject>(x => x.TestResultId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.Property(x => x.ActionType).HasMaxLength(64).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(128);
            e.Property(x => x.ActorLogin).HasMaxLength(128);
            e.Property(x => x.Details).HasMaxLength(4000);
            e.HasIndex(x => x.TimestampUtc);
        });
    }

    private static void ConfigureRowVersion(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType));

        foreach (var entityType in entityTypes)
        {
            modelBuilder.Entity(entityType.ClrType)
                .Property(nameof(BaseEntity.RowVersion))
                .IsRowVersion()
                .IsConcurrencyToken();
        }
    }
}
