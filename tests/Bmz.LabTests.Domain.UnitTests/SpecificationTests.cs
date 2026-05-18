using Bmz.LabTests.Domain.Common;
using FluentAssertions;
using System.Linq.Expressions;

namespace Bmz.LabTests.Domain.UnitTests;

public sealed class SpecificationTests
{
    private sealed class IntSpec : Specification<int>
    {
        public void Add(Expression<Func<int, bool>> criteria) => AddCriteria(criteria);
        public void Include(Expression<Func<int, object>> include) => AddInclude(include);
        public void Order(Expression<Func<int, object>> orderBy) => ApplyOrderBy(orderBy);
        public void OrderDesc(Expression<Func<int, object>> orderBy) => ApplyOrderByDescending(orderBy);
        public void Page(int skip, int take) => ApplyPaging(skip, take);
    }

    [Fact]
    public void AddCriteria_ShouldAddToCriteriaList()
    {
        var spec = new IntSpec();
        spec.Add(x => x > 10);

        spec.Criteria.Should().HaveCount(1);
    }

    [Fact]
    public void ApplyOrderBy_ShouldSetOrderByAndClearOrderByDescending()
    {
        var spec = new IntSpec();
        spec.OrderDesc(x => x);
        spec.Order(x => x);

        spec.OrderBy.Should().NotBeNull();
        spec.OrderByDescending.Should().BeNull();
    }

    [Fact]
    public void ApplyOrderByDescending_ShouldSetOrderByDescendingAndClearOrderBy()
    {
        var spec = new IntSpec();
        spec.Order(x => x);
        spec.OrderDesc(x => x);

        spec.OrderByDescending.Should().NotBeNull();
        spec.OrderBy.Should().BeNull();
    }

    [Fact]
    public void ApplyPaging_ShouldEnablePagingAndSetSkipTake()
    {
        var spec = new IntSpec();
        spec.Page(20, 10);

        spec.IsPagingEnabled.Should().BeTrue();
        spec.Skip.Should().Be(20);
        spec.Take.Should().Be(10);
    }
}

