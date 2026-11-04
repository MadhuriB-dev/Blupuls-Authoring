namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents a UDS/KWP Diagnostic Service (e.g., ReadDataByIdentifier 0x22, DiagnosticSessionControl 0x10).
/// </summary>
public class DiagnosticService
{
    public string Id { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Semantic { get; set; } = string.Empty;

    /// <summary>
    /// Hexadecimal Service ID (e.g., "0x10", "0x22", "0x2E", "0x19", "0x14", "0x27", "0x31").
    /// </summary>
    public string ServiceIdHex { get; set; } = string.Empty;

    /// <summary>
    /// Hexadecimal Sub-Function if applicable (e.g., "0x01", "0x03").
    /// </summary>
    public string SubFunctionHex { get; set; } = string.Empty;

    /// <summary>
    /// Positive Response Service ID (e.g., "0x50", "0x62", "0x6E").
    /// </summary>
    public string PositiveResponseSidHex { get; set; } = string.Empty;

    /// <summary>
    /// Request parameters structure.
    /// </summary>
    public List<DiagnosticParameter> RequestParameters { get; set; } = new();

    /// <summary>
    /// Positive response parameters structure.
    /// </summary>
    public List<DiagnosticParameter> ResponseParameters { get; set; } = new();

    /// <summary>
    /// Supported Negative Response Codes (NRCs).
    /// </summary>
    public List<string> NegativeResponses { get; set; } = new();

    /// <summary>
    /// Human-friendly display summary (e.g. "0x22 ReadDataByIdentifier").
    /// </summary>
    public string FullDisplayName => string.IsNullOrWhiteSpace(ServiceIdHex)
        ? ShortName
        : $"{ServiceIdHex} {ShortName}";
}

