using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Repositories;

public sealed class TestResultRepository(ApplicationDbContext dbContext) : ITestResultRepository
{
    public async Task<(List<TestResult> Items, int TotalCount)> GetListAsync(Specification<TestResult> specification, CancellationToken cancellationToken)
    {
        var query = ApplySpecification(specification);
        var totalCount = await ApplySpecificationCriteriaOnly(specification).CountAsync(cancellationToken);
        var items = await query.ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    private IQueryable<TestResult> ApplySpecification(Specification<TestResult> specification)
    {
        var query = dbContext.TestResults.AsNoTracking().AsQueryable();

        foreach (var criteria in specification.Criteria)
        {
            query = query.Where(criteria);
        }

        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy).ThenBy(x => x.Id);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending).ThenBy(x => x.Id);
        }
        else
        {
            query = query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id);
        }

        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }

    private IQueryable<TestResult> ApplySpecificationCriteriaOnly(Specification<TestResult> specification)
    {
        var query = dbContext.TestResults.AsNoTracking().AsQueryable();

        foreach (var criteria in specification.Criteria)
        {
            query = query.Where(criteria);
        }

        return query;
    }

    public Task<TestResult?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.TestResults.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<TestResult?> GetByIdWithValuesAsync(int id, CancellationToken cancellationToken)
        => dbContext.TestResults
            .Include(x => x.Values)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
        => dbContext.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public Task<int?> GetLaboratoryIdByEngineerIdAsync(int engineerUserId, CancellationToken cancellationToken)
        => dbContext.Users
            .Where(x => x.Id == engineerUserId && x.Role.Name == Roles.Engineer)
            .Select(x => (int?)x.LaboratoryId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> WireCodeExistsAsync(int wireCodeId, CancellationToken cancellationToken)
        => dbContext.WireCodes.AnyAsync(x => x.Id == wireCodeId, cancellationToken);

    public Task<List<int>> GetAllowedParameterIdsAsync(int wireCodeId, CancellationToken cancellationToken)
        => dbContext.Limits.Where(x => x.WireCodeId == wireCodeId).Select(x => x.ParameterId).ToListAsync(cancellationToken);

    public Task AddAsync(TestResult testResult, CancellationToken cancellationToken)
        => dbContext.TestResults.AddAsync(testResult, cancellationToken).AsTask();

    public void SetOriginalRowVersion(TestResult testResult, byte[] rowVersion)
        => dbContext.Entry(testResult).Property(x => x.RowVersion).OriginalValue = rowVersion;

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);

    public async Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.TestResults.FindAsync([id], cancellationToken);
        if (entity is null)
            return false;
        dbContext.TestResults.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
