using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPresupuesto.Api.Extensions;
using MiPresupuesto.Application.Imports;

namespace MiPresupuesto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/expenses/import")]
public sealed class ExpenseImportsController(IExpenseImportService importService) : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<ExpenseImportResponse>> Import(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError("file", "Selecciona un archivo Excel.");
            return ValidationProblem(ModelState);
        }

        if (file.Length > MaxFileSize)
        {
            ModelState.AddModelError("file", "El archivo no puede superar los 5 MB.");
            return ValidationProblem(ModelState);
        }

        await using var stream = file.OpenReadStream();
        return Ok(await importService.ImportAsync(
            User.GetUserId(), stream, file.FileName, cancellationToken));
    }

    [HttpGet("template")]
    public IActionResult DownloadTemplate()
    {
        var file = importService.GetTemplate();
        return File(file.Content, file.ContentType, file.FileName);
    }
}
