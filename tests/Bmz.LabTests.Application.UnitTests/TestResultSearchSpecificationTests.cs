using Bmz.LabTests.Application.TestResults;
using Bmz.LabTests.Domain.Enums;
using FluentAssertions;

namespace Bmz.LabTests.Application.UnitTests;

public sealed class TestResultSearchSpecificationTests
{
    [Fact]
    public void Constructor_ShouldApplyPaging()
    {
        var spec = new TestResultSearchSpecification(
            laboratoryId: null,
            fromUtc: null,
            toUtc: null,
            wireCodeId: null,
            batchNumber: null,
            status: null,
            page: 3,
            pageSize: 25,
            sortBy: null,
            sortDesc: null);

        spec.IsPagingEnabled.Should().BeTrue();
        spec.Skip.Should().Be(50);
        spec.Take.Should().Be(25);
    }

    [Fact]
    public void Constructor_ShouldBuildCriteria_ForProvidedFilters()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        var spec = new TestResultSearchSpecification(
            laboratoryId: 10,
            fromUtc: from,
            toUtc: to,
            wireCodeId: 5,
            batchNumber: "B-",
            status: TestResultStatus.Completed,
            page: 1,
            pageSize: 20,
            sortBy: TestResultSortBy.Date,
            sortDesc: true);

        spec.Criteria.Should().HaveCount(6);
    }

    [Fact]
    public void Constructor_DefaultSort_ShouldOrderByDateDescending()
    {
        var spec = new TestResultSearchSpecification(
            laboratoryId: null,
            fromUtc: null,
            toUtc: null,
            wireCodeId: null,
            batchNumber: null,
            status: null,
            page: 1,
            pageSize: 20,
            sortBy: null,
            sortDesc: null);

        spec.OrderByDescending.Should().NotBeNull();
        spec.OrderBy.Should().BeNull();
        spec.OrderByDescending!.ToString().Should().Contain("x.Date");
    }

    [Fact]
    public void Constructor_SortByWireCode_ShouldUseNavigationPropertyInExpression()
    {
        var spec = new TestResultSearchSpecification(
            laboratoryId: null,
            fromUtc: null,
            toUtc: null,
            wireCodeId: null,
            batchNumber: null,
            status: null,
            page: 1,
            pageSize: 20,
            sortBy: TestResultSortBy.WireCode,
            sortDesc: true);

        spec.OrderByDescending.Should().NotBeNull();
        spec.OrderByDescending!.ToString().Should().Contain("x.WireCode.Code");
    }
}

