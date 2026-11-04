namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents a UDS Data Identifier (DID, Service 0x22 / 0x2E).
/// </summary>
public class DiagnosticDataIdentifier
{
    /// <summary>
    /// Hexadecimal identifier (e.g. "0xF190", "0x0105").
    /// </summary>
    public string DidHex { get; set; } = string.Empty;

    /// <summary>
    /// Decimal representation of the DID (e.g. 61840).
    /// </summary>
    public int DidDecimal { get; set; }

    /// <summary>
    /// Short identifier name (e.g., "VIN", "Engine_Coolant_Temperature").
    /// </summary>
    public string ShortName { get; set; } = string.Empty;

    /// <summary>
    /// Full descriptive name.
    /// </summary>
    public string LongName { get; set; } = string.Empty;

    /// <summary>
    /// Access permissions: "Read", "Write", "Read/Write".
    /// </summary>
    public string Access { get; set; } = "Read";

    /// <summary>
    /// Length in bytes.
    /// </summary>
    public int ByteLength { get; set; }

    /// <summary>
    /// Length in bits.
    /// </summary>
    public int BitLength { get; set; }

    /// <summary>
    /// Data type representation (ASCII, HEX, UNSIGNED, SIGNED, FLOAT, TEXTTABLE).
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Physical engineering unit (e.g. "degC", "rpm", "km/h", "V", "kPa").
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Linear scaling factor (e.g. 0.1, 0.25).
    /// </summary>
    public double ScalingFactor { get; set; } = 1.0;

    /// <summary>
    /// Linear offset (e.g. -40.0).
    /// </summary>
    public double Offset { get; set; } = 0.0;

    /// <summary>
    /// Minimum physical engineering value.
    /// </summary>
    public double? MinPhysicalValue { get; set; }

    /// <summary>
    /// Maximum physical engineering value.
    /// </summary>
    public double? MaxPhysicalValue { get; set; }

    /// <summary>
    /// Associated service short name (e.g., "ReadDataByIdentifier_VIN").
    /// </summary>
    public string AssociatedService { get; set; } = string.Empty;

    /// <summary>
    /// Free text description or comments.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

