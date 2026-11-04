using System.Diagnostics;
using OdxDiagnosticParser.Exporters;
using OdxDiagnosticParser.Models;
using OdxDiagnosticParser.Parsers;

namespace OdxDiagnosticParser.Sessions;

/// <summary>
/// Manages a temporary, isolated diagnostic session where PDX files are unpacked
/// and Excel reports are generated temporarily. When the session ends or the application
/// closes, all extracted files, directories, and the Excel workbook are automatically deleted.
/// </summary>
public class DiagnosticSession : IDisposable
{
    private static readonly IOdxParser DefaultOdxParser = new OdxXmlParser();
    private static readonly IPdxExtractor DefaultPdxExtractor = new PdxPackageExtractor(DefaultOdxParser);
    private static readonly IOdxExcelExporter DefaultExcelExporter = new OdxExcelExporter();

    private bool _disposed;
    private readonly EventHandler _processExitHandler;

    public string SessionId { get; }
    public string SessionDirectory { get; }
    public string ExtractedDirectory { get; }
    public string TemporaryExcelFilePath { get; }
    public DiagnosticPackage Package { get; }
    public bool IsActive => !_disposed;
    public DateTime CreatedAt { get; }

    private DiagnosticSession(
        string pdxOrOdxPath,
        DiagnosticPackage package,
        string sessionDir,
        string extractedDir,
        string tempExcelPath)
    {
        SessionId = Path.GetFileName(sessionDir);
        SessionDirectory = sessionDir;
        ExtractedDirectory = extractedDir;
        TemporaryExcelFilePath = tempExcelPath;
        Package = package;
        CreatedAt = DateTime.UtcNow;

        // Register process exit hook to guarantee cleanup on unexpected termination
        _processExitHandler = (_, _) => Dispose();
        AppDomain.CurrentDomain.ProcessExit += _processExitHandler;
    }

    /// <summary>
    /// Creates a temporary session for a PDX archive: unzips to a session folder,
    /// parses diagnostic layers, and generates a temporary Excel file.
    /// </summary>
    public static DiagnosticSession CreateFromPdx(
        string pdxFilePath,
        string? customSessionParentDir = null,
        ExcelExportOptions? excelOptions = null)
    {
        if (string.IsNullOrWhiteSpace(pdxFilePath))
            throw new ArgumentException("PDX file path cannot be null or empty.", nameof(pdxFilePath));

        if (!File.Exists(pdxFilePath))
            throw new FileNotFoundException($"PDX file not found: {pdxFilePath}", pdxFilePath);

        var sessionId = $"Session_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}";
        var baseDir = string.IsNullOrWhiteSpace(customSessionParentDir)
            ? Path.Combine(Path.GetTempPath(), "Blupuls_OdxSessions")
            : customSessionParentDir;

        var sessionDir = Path.Combine(baseDir, sessionId);
        var extractedDir = Path.Combine(sessionDir, "Extracted_ODX");
        Directory.CreateDirectory(extractedDir);

        // 1. Unzip & Parse PDX into session directory
        var package = DefaultPdxExtractor.ExtractAndParse(pdxFilePath, extractedDir);

        // 2. Generate temporary Excel file in the session directory
        var excelFileName = $"{SanitizeFileName(package.Name)}_Diagnostics_Temp.xlsx";
        var tempExcelPath = Path.Combine(sessionDir, excelFileName);

        DefaultExcelExporter.ExportToExcel(package, tempExcelPath, excelOptions);

        return new DiagnosticSession(pdxFilePath, package, sessionDir, extractedDir, tempExcelPath);
    }

    /// <summary>
    /// Creates a temporary session for a single ODX XML file.
    /// </summary>
    public static DiagnosticSession CreateFromOdx(
        string odxFilePath,
        string? customSessionParentDir = null,
        ExcelExportOptions? excelOptions = null)
    {
        if (string.IsNullOrWhiteSpace(odxFilePath))
            throw new ArgumentException("ODX file path cannot be null or empty.", nameof(odxFilePath));

        if (!File.Exists(odxFilePath))
            throw new FileNotFoundException($"ODX file not found: {odxFilePath}", odxFilePath);

        var sessionId = $"Session_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..6]}";
        var baseDir = string.IsNullOrWhiteSpace(customSessionParentDir)
            ? Path.Combine(Path.GetTempPath(), "Blupuls_OdxSessions")
            : customSessionParentDir;

        var sessionDir = Path.Combine(baseDir, sessionId);
        var extractedDir = Path.Combine(sessionDir, "Working_ODX");
        Directory.CreateDirectory(extractedDir);

        // Copy source file to session folder
        var localCopy = Path.Combine(extractedDir, Path.GetFileName(odxFilePath));
        File.Copy(odxFilePath, localCopy, overwrite: true);

        var package = DefaultOdxParser.ParseFile(localCopy);

        var excelFileName = $"{SanitizeFileName(package.Name)}_Diagnostics_Temp.xlsx";
        var tempExcelPath = Path.Combine(sessionDir, excelFileName);

        DefaultExcelExporter.ExportToExcel(package, tempExcelPath, excelOptions);

        return new DiagnosticSession(odxFilePath, package, sessionDir, extractedDir, tempExcelPath);
    }

    /// <summary>
    /// Launches the temporary Excel file in the default spreadsheet viewer (e.g. Microsoft Excel).
    /// </summary>
    public bool OpenExcelInDefaultViewer()
    {
        if (!File.Exists(TemporaryExcelFilePath))
            return false;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = TemporaryExcelFilePath,
                UseShellExecute = true
            };
            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Could not automatically launch Excel viewer: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Allows the user to save a permanent copy of the Excel workbook before the session closes.
    /// </summary>
    public void SavePermanentCopy(string destinationFilePath)
    {
        if (!File.Exists(TemporaryExcelFilePath))
            throw new FileNotFoundException("Temporary Excel file does not exist.", TemporaryExcelFilePath);

        var targetDir = Path.GetDirectoryName(destinationFilePath);
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        File.Copy(TemporaryExcelFilePath, destinationFilePath, overwrite: true);
    }

    /// <summary>
    /// Cleans up and deletes the entire session folder, including extracted ODX files and the temporary Excel file.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            AppDomain.CurrentDomain.ProcessExit -= _processExitHandler;
        }
        catch { }

        CleanupSessionFiles();
        GC.SuppressFinalize(this);
    }

    private void CleanupSessionFiles()
    {
        if (!Directory.Exists(SessionDirectory))
            return;

        // Attempt deletion with retries in case Excel or another viewer still has a lock
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                Directory.Delete(SessionDirectory, recursive: true);
                break;
            }
            catch (IOException)
            {
                if (attempt < 3)
                {
                    Thread.Sleep(500);
                }
            }
            catch { }
        }
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "DiagnosticReport";
        var invalids = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalids.Contains(c) ? '_' : c));
    }
}

