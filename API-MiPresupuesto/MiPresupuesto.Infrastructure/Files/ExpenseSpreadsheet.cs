using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.Imports;
using MiPresupuesto.Application.Reports;

namespace MiPresupuesto.Infrastructure.Files;

public sealed class ExpenseSpreadsheet : IExpenseSpreadsheet
{
    private static readonly string[] RequiredHeaders = ["fecha", "monto", "categoria", "metodopago"];

    public IReadOnlyList<ExpenseImportRow> Read(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.FirstOrDefault()
                ?? throw FileError("El archivo no contiene hojas.");
            var headerRow = worksheet.FirstRowUsed()
                ?? throw FileError("El archivo está vacío.");
            var columns = headerRow.CellsUsed()
                .ToDictionary(cell => Normalize(cell.GetString()), cell => cell.Address.ColumnNumber);

            var missing = RequiredHeaders.Where(header => !columns.ContainsKey(header)).ToArray();
            if (missing.Length > 0)
            {
                throw FileError($"Faltan columnas obligatorias: {string.Join(", ", missing)}.");
            }

            var rows = new List<ExpenseImportRow>();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();
            var lastColumn = columns.Values.Max();
            for (var rowNumber = headerRow.RowNumber() + 1; rowNumber <= lastRow; rowNumber++)
            {
                var row = worksheet.Row(rowNumber);
                if (row.Cells(1, lastColumn).All(cell => cell.IsEmpty())) continue;

                var dateCell = row.Cell(columns["fecha"]);
                var amountCell = row.Cell(columns["monto"]);
                var errors = new List<string>();
                var date = ReadDate(dateCell, errors);
                var amount = ReadAmount(amountCell, errors);
                rows.Add(new ExpenseImportRow(
                    rowNumber,
                    date,
                    amount,
                    row.Cell(columns["categoria"]).GetString().Trim(),
                    row.Cell(columns["metodopago"]).GetString().Trim(),
                    columns.TryGetValue("descripcion", out var descriptionColumn)
                        ? row.Cell(descriptionColumn).GetString().Trim()
                        : null,
                    errors.Count == 0 ? null : string.Join(" ", errors)));
            }

            return rows;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw FileError("No se pudo leer el archivo. Verifica que sea un Excel .xlsx válido.");
        }
    }

    public ReportFile CreateTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Gastos");
        string[] headers = ["Fecha", "Monto", "Categoría", "Método de pago", "Descripción"];
        for (var index = 0; index < headers.Length; index++) sheet.Cell(1, index + 1).Value = headers[index];
        sheet.Range(1, 1, 1, headers.Length).Style.Font.SetBold().Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#4F46E5"));
        sheet.Cell("A2").Value = DateTime.Today;
        sheet.Cell("A2").Style.DateFormat.Format = "dd/MM/yyyy";
        sheet.Cell("B2").Value = 1250.50m;
        sheet.Cell("C2").Value = "Alimentación";
        sheet.Cell("D2").Value = "Efectivo";
        sheet.Cell("E2").Value = "Compra de ejemplo (puedes borrar esta fila)";
        sheet.Column(2).Style.NumberFormat.Format = "#,##0.00";
        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        var instructions = workbook.Worksheets.Add("Instrucciones");
        instructions.Cell("A1").Value = "INSTRUCCIONES DE IMPORTACIÓN";
        instructions.Cell("A1").Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#4F46E5"));
        instructions.Cell("A3").Value = "1. No cambies los nombres de las columnas de la hoja Gastos.";
        instructions.Cell("A4").Value = "2. Categoría y Método de pago deben existir y estar activos en la aplicación.";
        instructions.Cell("A5").Value = "3. El monto debe ser positivo y tener máximo dos decimales.";
        instructions.Cell("A6").Value = "4. Se importan las filas válidas y se reportan individualmente las filas con errores.";
        instructions.Cell("A7").Value = "5. El máximo permitido es de 5,000 filas por archivo.";
        instructions.Column(1).AdjustToContents();

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return new ReportFile(
            output.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "plantilla-importacion-gastos.xlsx");
    }

    private static DateOnly? ReadDate(IXLCell cell, ICollection<string> errors)
    {
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<DateTime>(out var dateTime)) return DateOnly.FromDateTime(dateTime);
        var text = cell.GetString().Trim();
        string[] formats = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy"];
        if (DateOnly.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return date;
        errors.Add("Fecha inválida.");
        return null;
    }

    private static decimal? ReadAmount(IXLCell cell, ICollection<string> errors)
    {
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<decimal>(out var amount)) return amount;
        var text = cell.GetString().Trim();
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) ||
            decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("es-DO"), out amount)) return amount;
        errors.Add("Monto inválido.");
        return null;
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var normalized = new string(decomposed
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit)
            .ToArray());
        return normalized switch
        {
            "metododepago" or "formadepago" => "metodopago",
            "category" => "categoria",
            "paymentmethod" => "metodopago",
            "description" => "descripcion",
            "date" => "fecha",
            "amount" => "monto",
            _ => normalized
        };
    }

    private static ValidationException FileError(string message)
        => new("Archivo no válido.", new Dictionary<string, string[]> { ["file"] = [message] });
}
