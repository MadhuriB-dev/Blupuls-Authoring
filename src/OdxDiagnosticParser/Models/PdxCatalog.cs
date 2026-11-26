namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents the catalog structure found in the PDX archive's index.xml manifest (ISO 22901-2).
/// </summary>
public class PdxCatalog
{
    public string ShortName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string StandardVersion { get; set; } = "2.2.0"; // CATALOG/@F-VERSION or MODEL-VERSION
    public string Creator { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CreationDate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<PdxVehicleInfo> Vehicles { get; set; } = new();
    public List<PdxEcuInfo> Ecus { get; set; } = new();
    public List<PdxAblock> Ablocks { get; set; } = new();
    public List<PdxFileInfo> Files { get; set; } = new();
}

/// <summary>
/// Represents an ISO 22901-2 ABLOCK entry inside index.xml cataloging ODX component archives.
/// </summary>
public class PdxAblock
{
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // ODX-DATA, ODX-COMPARAM-SPEC, ODX-VEHICLE-INFO, ODX-FLASH, etc.
    public string RevisionLabel { get; set; } = string.Empty;
    public List<PdxFileInfo> Files { get; set; } = new();

    public string PrimaryFilePath => Files.FirstOrDefault()?.RelativePath ?? string.Empty;
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
    public string AblockShortName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // ODX-D, ODX-C, ODX-V, ODX-E, ODX-DATA, ODX-COMPARAM-SPEC, ODX-VEHICLE-INFO, etc.
    public string MimeType { get; set; } = string.Empty;
    public string CreationDate { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public string ChecksumType { get; set; } = "MD5";
}
