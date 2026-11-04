namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents the catalog structure found in the PDX archive's index.xml manifest.
/// </summary>
public class PdxCatalog
{
    public string ShortName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Creator { get; set; } = string.Empty;
    public string CreationDate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<PdxVehicleInfo> Vehicles { get; set; } = new();
    public List<PdxEcuInfo> Ecus { get; set; } = new();
    public List<PdxFileInfo> Files { get; set; } = new();
}

public class PdxVehicleInfo
{
    public string ShortName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string ModelYear { get; set; } = string.Empty;
}

public class PdxEcuInfo
{
    public string ShortName { get; set; } = string.Empty;
    public string EcuName { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string DiagnosticAddress { get; set; } = string.Empty;
}

public class PdxFileInfo
{
    public string ShortName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // ODX-D, ODX-C, ODX-V, ODX-E, etc.
    public long FileSizeBytes { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public string ChecksumType { get; set; } = "MD5";
}

