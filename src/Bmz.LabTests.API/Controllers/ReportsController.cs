using Bmz.LabTests.Application.Abstractions;
using Bmz.LabTests.Application.Abstractions.Organization;
using Bmz.LabTests.Application.Abstractions.ReferenceData;
using Bmz.LabTests.Application.Abstractions.Reporting;
using Bmz.LabTests.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

/// <summary>
/// Контроллер для генерации аналитических отчетов и журналов.
/// Позволяет выгружать данные в форматах Excel (для журналов) и PDF (для статистики и сертификатов).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Engineer}")]
public sealed class ReportsController(
    IReportService reportService,
    ILaboratoryService laboratoryService,
    IReferenceDataService referenceDataService,
    ICurrentUserService currentUser,
    IStatisticsService statisticsService) : ApiControllerBase
{
    /// <summary>
    /// Получает доступные фильтры для отчетов (лаборатории и коды проволоки).
    /// Фильтрует список лабораторий в зависимости от прав текущего пользователя.
    /// </summary>
    [HttpGet("filters")]
    public async Task<IActionResult> GetReportFilters(CancellationToken cancellationToken)
    {
        var isAdmin = string.Equals(currentUser.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase);

        var labsResult = await laboratoryService.GetLaboratoriesAsync(cancellationToken);
        var wireCodesResult = await referenceDataService.GetWireCodesAsync(null, cancellationToken);

        if (labsResult.IsFailure) return ToActionResult(labsResult);
        if (wireCodesResult.IsFailure) return ToActionResult(wireCodesResult);

        var labs = labsResult.Value;
        var wireCodes = wireCodesResult.Value;

        if (!isAdmin)
        {
            var userLabId = currentUser.LaboratoryId;
            if (userLabId.HasValue)
            {
                labs = labs.Where(x => x.Id == userLabId.Value).ToList();
            }
        }

        return Ok(new
        {
            laboratories = labs.Select(x => new { x.Id, x.Name }),
            wireCodes = wireCodes.Select(x => new { x.Id, x.Code })
        });
    }

    /// <summary>
    /// Генерирует и возвращает ежемесячный журнал испытаний в формате Excel.
    /// </summary>
    [HttpGet("monthly-journal")]
    public async Task<IActionResult> MonthlyJournal([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
            return BadRequest("Неверный год/месяц.");

        var result = await reportService.GenerateMonthlyJournalExcelAsync(year, month, cancellationToken);
        if (result.IsFailure) return ToActionResult(result);

        var report = result.Value;
        return File(report.Content, report.ContentType, report.FileName);
    }

    /// <summary>
    /// Генерирует и возвращает детальный журнал испытаний в формате Excel по заданным фильтрам.
    /// </summary>
    [HttpGet("detailed-journal")]
    public async Task<IActionResult> DetailedJournal(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int? laboratoryId,
        [FromQuery] int? wireCodeId,
        CancellationToken cancellationToken)
    {
        if (fromUtc >= toUtc)
            return BadRequest("Параметр fromUtc должен быть раньше параметра toUtc.");
        if ((toUtc - fromUtc).TotalDays > 365)
            return BadRequest("Диапазон дат не может превышать 365 дней.");

        var result = await reportService.GenerateDetailedJournalExcelAsync(fromUtc, toUtc, laboratoryId, wireCodeId, cancellationToken);
        if (result.IsFailure) return ToActionResult(result);

        var report = result.Value;
        return File(report.Content, report.ContentType, report.FileName);
    }

    /// <summary>
    /// Генерирует аналитический отчет со статистикой и графиками в формате PDF.
    /// </summary>
    [HttpGet("statistics-pdf")]
    public async Task<IActionResult> StatisticsPdf(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int? laboratoryId,
        [FromQuery] StatisticsTrendGroupBy groupBy,
        CancellationToken cancellationToken)
    {
        if (fromUtc >= toUtc)
            return BadRequest("Параметр fromUtc должен быть раньше параметра toUtc.");
        if ((toUtc - fromUtc).TotalDays > 365)
            return BadRequest("Диапазон дат не может превышать 365 дней.");

        var statsResult = await statisticsService.GetAsync(fromUtc, toUtc, groupBy, laboratoryId, cancellationToken);
        if (statsResult.IsFailure) return ToActionResult(statsResult);

        var reportResult = await reportService.GenerateStatisticsPdfWithChartsAsync(fromUtc, toUtc, laboratoryId, groupBy, statsResult.Value, cancellationToken);
        if (reportResult.IsFailure) return ToActionResult(reportResult);

        var report = reportResult.Value;
        return File(report.Content, report.ContentType, report.FileName);
    }

    /// <summary>
    /// Генерирует сертификат (протокол) качества для конкретного результата испытаний в формате PDF.
    /// </summary>
    [HttpGet("test-results/{testResultId:int}/certificate")]
    public async Task<IActionResult> Certificate(int testResultId, CancellationToken cancellationToken)
    {
        var result = await reportService.GenerateBatchCertificatePdfAsync(testResultId, cancellationToken);
        if (result.IsFailure) return ToActionResult(result);

        var report = result.Value;
        return File(report.Content, report.ContentType, report.FileName);
    }
}
