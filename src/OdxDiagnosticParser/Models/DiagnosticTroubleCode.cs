namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents a Diagnostic Trouble Code (DTC) according to ISO 14229 / SAE J2012.
/// </summary>
public class DiagnosticTroubleCode
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Raw integer trouble code as defined in ODX (e.g. 12595219).
    /// </summary>
    public long TroubleCode { get; set; }

    /// <summary>
    /// Hexadecimal trouble code representation (e.g. "0x013313").
    /// </summary>
    public string TroubleCodeHex { get; set; } = string.Empty;

    /// <summary>
    /// Standard alphanumeric display code (e.g. "P0133-13", "B1000", "U0100").
    /// </summary>
    public string DisplayCode { get; set; } = string.Empty;

    /// <summary>
    /// Trouble code text description (e.g. "O2 Sensor Circuit Slow Response (Bank 1, Sensor 1)").
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Severity level (e.g., "Information", "Warning", "Fatal").
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Failure Type Byte (FTB) hex and description (e.g. "0x13 (Circuit open)").
    /// </summary>
    public string FailureTypeByte { get; set; } = string.Empty;

    /// <summary>
    /// Status mask byte support.
    /// </summary>
    public string StatusMask { get; set; } = string.Empty;
}

