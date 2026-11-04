using OdxDiagnosticParser.Models;

namespace OdxDiagnosticParser.Parsers;

/// <summary>
/// Interface for unzipping PDX packages, parsing catalogs, and aggregating diagnostic data.
/// </summary>
public interface IPdxExtractor
{
    /// <summary>
    /// Unzips a PDX archive to the specified destination directory with path-traversal safety checks.
    /// </summary>
    string UnzipPdx(string pdxFilePath, string? targetDirectory = null);

    /// <summary>
    /// Parses the index.xml catalog file inside an unzipped PDX folder.
    /// </summary>
    PdxCatalog? ParseCatalog(string extractedDirectory);

    /// <summary>
    /// Unzips a PDX package, extracts all packaged ODX files, and aggregates them into a DiagnosticPackage.
    /// </summary>
    DiagnosticPackage ExtractAndParse(string pdxFilePath, string? targetDirectory = null);
}

