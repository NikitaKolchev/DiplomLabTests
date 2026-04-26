using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;
using System.Linq.Expressions;

namespace Bmz.LabTests.Application.TestResults;

public sealed class TestResultSearchSpecification : Specification<TestResult>
{
    public TestResultSearchSpecification(
        int? laboratoryId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int? wireCodeId,
        string? batchNumber,
        TestResultStatus? status,
        int page,
        int pageSize,
        string? sortBy = null,
        bool? sortDesc = null)
    {
        if (laboratoryId.HasValue)
            AddCriteria(x => x.LaboratoryId == laboratoryId.Value);

        if (fromUtc.HasValue)
            AddCriteria(x => x.Date >= fromUtc.Value);

        if (toUtc.HasValue)
            AddCriteria(x => x.Date <= toUtc.Value);

        if (wireCodeId.HasValue)
            AddCriteria(x => x.WireCodeId == wireCodeId.Value);

        if (!string.IsNullOrWhiteSpace(batchNumber))
            AddCriteria(x => x.BatchNumber.Contains(batchNumber));

        if (status.HasValue)
            AddCriteria(x => x.Status == status.Value);

        ApplyPaging((page - 1) * pageSize, pageSize);

        var isDesc = sortDesc ?? true;
        var sortByLower = sortBy?.ToLowerInvariant();

        switch (sortByLower)
        {
            case "date":
                if (isDesc) ApplyOrderByDescending(x => x.Date);
                else ApplyOrderBy(x => x.Date);
                break;
            case "updatedat":
            case "updatedatutc":
                if (isDesc) ApplyOrderByDescending(x => x.UpdatedAtUtc);
                else ApplyOrderBy(x => x.UpdatedAtUtc);
                break;
            case "batchnumber":
                if (isDesc) ApplyOrderByDescending(x => x.BatchNumber);
                else ApplyOrderBy(x => x.BatchNumber);
                break;
            case "wirecode":
                if (isDesc) ApplyOrderByDescending(x => x.WireCode.Code);
                else ApplyOrderBy(x => x.WireCode.Code);
                break;
            case "assistant":
                if (isDesc) ApplyOrderByDescending(x => x.Assistant.FullName);
                else ApplyOrderBy(x => x.Assistant.FullName);
                break;
            case "status":
                if (isDesc) ApplyOrderByDescending(x => x.Status);
                else ApplyOrderBy(x => x.Status);
                break;
            default:
                ApplyOrderByDescending(x => x.Date);
                break;
        }
        
        AddInclude(x => x.WireCode);
        AddInclude(x => x.Assistant);
    }
}