using OdxDiagnosticParser.Models;

namespace OdxDiagnosticParser.Exporters;

/// <summary>
/// Options for configuring the generated Excel diagnostic workbook.
/// </summary>
public class ExcelExportOptions
{
    public bool IncludeOverviewSheet { get; set; } = true;
    public bool IncludeServicesSheet { get; set; } = true;
    public bool IncludeDidsSheet { get; set; } = true;
    public bool IncludeDtcsSheet { get; set; } = true;
    public bool IncludeComParamsSheet { get; set; } = true;
    public bool IncludePdxManifestSheet { get; set; } = true;

    /// <summary>
    /// Hex color for header background (Default: automotive dark navy #1B365D).
    /// </summary>
    public string HeaderBackgroundColorHex { get; set; } = "#1B365D";

    /// <summary>
    /// Hex color for alternating row striping (Default: light ice slate #F8FAFC).
    /// </summary>
    public string AlternateRowColorHex { get; set; } = "#F8FAFC";

    /// <summary>
    /// Maximum width in characters for auto-fitted columns.
    /// </summary>
    public double MaxColumnWidth { get; set; } = 55.0;
}

/// <summary>
/// Interface for exporting DiagnosticPackage data into styled Excel (.xlsx) workbooks.
/// </summary>
public interface IOdxExcelExporter
{
    /// <summary>
    /// Exports the diagnostic package to an Excel file at the specified output path.
    /// </summary>
    void ExportToExcel(DiagnosticPackage package, string outputFilePath, ExcelExportOptions? options = null);

    /// <summary>
    /// Exports the diagnostic package to a byte array stream in memory.
    /// </summary>
    byte[] ExportToBytes(DiagnosticPackage package, ExcelExportOptions? options = null);
}

