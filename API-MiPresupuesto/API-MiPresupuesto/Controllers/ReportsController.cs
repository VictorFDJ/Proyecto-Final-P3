using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPresupuesto.Api.Extensions;
using MiPresupuesto.Application.Reports;

namespace MiPresupuesto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("monthly")]
    public async Task<ActionResult<MonthlyReportResponse>> GetMonthly(
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
        => Ok(await reportService.GetMonthlyAsync(User.GetUserId(), year, month, cancellationToken));

    [HttpGet("monthly/export/{format}")]
    public async Task<IActionResult> ExportMonthly(
        string format,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken)
    {
        var file = await reportService.ExportMonthlyAsync(
            User.GetUserId(), year, month, format, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
