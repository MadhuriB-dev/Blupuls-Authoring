namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents an ODX Data Object Property (DOP) specifying serialization and scaling.
/// </summary>
public class DataObjectProperty
{
    public string Id { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string Category { get; set; } = "IDENTICAL"; // IDENTICAL, LINEAR, TEXTTABLE, SCALE-LINEAR
    public string BaseDataType { get; set; } = "A_UINT32";
    public int BitLength { get; set; } = 8;
    public string PhysicalUnit { get; set; } = string.Empty;
    public double Factor { get; set; } = 1.0;
    public double Offset { get; set; } = 0.0;
    public double? MinPhysicalValue { get; set; }
    public double? MaxPhysicalValue { get; set; }
    public Dictionary<long, string> TextTableEntries { get; set; } = new();
}

