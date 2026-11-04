using OdxDiagnosticParser.Models;

namespace OdxDiagnosticParser.Parsers;

/// <summary>
/// Interface for parsing raw ODX XML files into diagnostic models.
/// </summary>
public interface IOdxParser
{
    /// <summary>
    /// Parses an ODX XML file from the given file path.
    /// </summary>
    DiagnosticPackage ParseFile(string filePath);

    /// <summary>
    /// Parses an ODX XML content string.
    /// </summary>
    DiagnosticPackage ParseXml(string xmlContent, string sourceName = "InlineXml");

    /// <summary>
    /// Parses an ODX XML stream.
    /// </summary>
    DiagnosticPackage ParseStream(Stream stream, string sourceName = "Stream");
}

