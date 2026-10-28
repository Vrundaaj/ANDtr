using System.Globalization;
using System.Text.RegularExpressions;
using ANDtr.Web.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace ANDtr.Web.Data;

public sealed partial class NoticeImporter(
    ApplicationDbContext db,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<NoticeImporter> logger)
{
    private const string DefaultRelativePath = "../../data/private/FDAdataset.xlsx";

    private const int ProductCode = 2;
    private const int ProductCodeName = 3;
    private const int RegulationNumber = 4;
    private const int WebAddress = 5;
    private const int RecallNumber = 6;
    private const int ProductDescription = 7;
    private const int TradeName = 8;
    private const int RecallClass = 9;
    private const int CenterClassificationDate = 10;
    private const int PostedInternetDate = 11;
    private const int TerminationDate = 12;
    private const int FeiNumber = 13;
    private const int FirmName = 14;
    private const int ManufacturerRecallReason = 15;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRuns();

    public async Task ImportAsync()
    {
        if (await db.Notices.AnyAsync())
        {
            return;
        }

        var configured = configuration["Seed:DatasetPath"];
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, DefaultRelativePath)
            : configured;

        if (!File.Exists(path))
        {
            logger.LogWarning(
                "Skipping dataset import: no file at {Path}. Put the dataset in data/private/ or set Seed:DatasetPath (see SETUP.md).",
                Path.GetFullPath(path));
            return;
        }

        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet(1);

        var notices = new List<Notice>();
        var skippedWithoutRecallNumber = 0;
        var skippedUnparsable = 0;

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var recallNumber = Text(row, RecallNumber);
            if (recallNumber is null)
            {
                skippedWithoutRecallNumber++;
                continue;
            }

            var centerClassificationDate = Date(row, CenterClassificationDate);
            if (centerClassificationDate is null || !int.TryParse(Text(row, RecallClass), out var recallClass))
            {
                skippedUnparsable++;
                logger.LogWarning("Skipping dataset row {Row}: recall class or classification date could not be read.", row.RowNumber());
                continue;
            }

            notices.Add(new Notice
            {
                ProductCode = Text(row, ProductCode) ?? string.Empty,
                ProductCodeName = Text(row, ProductCodeName) ?? string.Empty,
                RegulationNumber = Text(row, RegulationNumber) ?? string.Empty,
                WebAddress = Text(row, WebAddress) ?? string.Empty,
                RecallNumber = recallNumber,
                ProductDescription = Text(row, ProductDescription) ?? string.Empty,
                TradeName = Text(row, TradeName),
                RecallClass = recallClass,
                CenterClassificationDate = centerClassificationDate.Value,
                PostedInternetDate = Date(row, PostedInternetDate),
                TerminationDate = Date(row, TerminationDate),
                FeiNumber = Text(row, FeiNumber),
                FirmName = Text(row, FirmName) ?? string.Empty,
                ManufacturerRecallReason = Text(row, ManufacturerRecallReason) ?? string.Empty,
            });
        }

        db.Notices.AddRange(notices);
        await db.SaveChangesAsync();

        var byClass = notices
            .GroupBy(n => n.RecallClass)
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Key}={g.Count()}");

        logger.LogInformation(
            "Dataset import: {Imported} notices imported, {NoRecallNumber} rows skipped (no recall number), {Unparsable} rows skipped (unreadable class or date). Classes {Classes}.",
            notices.Count, skippedWithoutRecallNumber, skippedUnparsable, string.Join(" ", byClass));
    }

    private static string? Text(IXLRow row, int column)
    {
        var value = row.Cell(column).GetFormattedString().Replace("_x000D_", string.Empty);
        value = WhitespaceRuns().Replace(value, " ").Trim();
        return value.Length == 0 ? null : value;
    }

    private static DateOnly? Date(IXLRow row, int column)
    {
        var cell = row.Cell(column);
        if (cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        var text = Text(row, column);
        if (text is null)
        {
            return null;
        }

        return DateOnly.TryParseExact(text, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }
}
