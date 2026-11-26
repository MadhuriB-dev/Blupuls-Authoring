namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents a complete diagnostic package derived from an ODX XML file or a PDX container (ISO 22901-1 &amp; ISO 22901-2).
/// </summary>
public class DiagnosticPackage
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string SourcePath { get; set; } = string.Empty;
    public string ExtractedDirectory { get; set; } = string.Empty;
    public bool IsFromPdx { get; set; }
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Metadata from PDX index.xml catalog (ISO 22901-2).
    /// </summary>
    public PdxCatalog? Catalog { get; set; }

    /// <summary>
    /// ISO 22901-1 ODX-V Vehicle Information Specs (Vehicle Topology &amp; Logical Links).
    /// </summary>
    public List<VehicleInfoSpec> VehicleInfos { get; set; } = new();

    /// <summary>
    /// ISO 22901-1 ODX-C Communication Parameter Specifications (Prot-Stacks &amp; ComParam Subsets).
    /// </summary>
    public List<ComParamSpecInfo> ComParamSpecs { get; set; } = new();

    /// <summary>
    /// Diagnostic layers parsed from the package (ECU Variants, Base Variants, Protocols, Functional Groups, Shared Data).
    /// </summary>
    public List<DiagnosticLayer> Layers { get; set; } = new();

    /// <summary>
    /// Cross-reference and ISO 22901 consistency validation results.
    /// </summary>
    public List<OdxConsistencyIssue> ConsistencyIssues { get; set; } = new();

    public IEnumerable<DiagnosticLayer> EcuVariants =>
        Layers.Where(l => l.LayerType == DiagnosticLayerType.EcuVariant);

    public IEnumerable<DiagnosticLayer> BaseVariants =>
        Layers.Where(l => l.LayerType == DiagnosticLayerType.BaseVariant);

    public IEnumerable<DiagnosticLayer> Protocols =>
        Layers.Where(l => l.LayerType == DiagnosticLayerType.Protocol);

    public IEnumerable<DiagnosticLayer> FunctionalGroups =>
        Layers.Where(l => l.LayerType == DiagnosticLayerType.FunctionalGroup);

    public IEnumerable<DiagnosticLayer> EcuSharedDatas =>
        Layers.Where(l => l.LayerType == DiagnosticLayerType.EcuSharedData);

    public int TotalServicesCount => Layers.Sum(l => l.Services.Count);
    public int UniqueServicesCount => Layers.Sum(l => l.Services.Count(s => !s.IsInherited));
    public int TotalCurrentDataServicesCount => Layers.Sum(l => l.Services.Count(s => s.DataCategory == "Current Data"));
    public int TotalStoredDataServicesCount => Layers.Sum(l => l.Services.Count(s => s.DataCategory == "Stored Data"));
    public int TotalSystemControlServicesCount => Layers.Sum(l => l.Services.Count(s => s.DataCategory == "System Control"));
    public int TotalDidsCount => Layers.Sum(l => l.DataIdentifiers.Count);
    public int TotalCurrentDataDidsCount => Layers.Sum(l => l.DataIdentifiers.Count(d => d.DataCategory == "Current Data"));
    public int TotalStoredDataDidsCount => Layers.Sum(l => l.DataIdentifiers.Count(d => d.DataCategory == "Stored Data"));
    public int TotalDtcsCount => Layers.Sum(l => l.DiagnosticTroubleCodes.Count);
    public int TotalComParamsCount => Layers.Sum(l => l.CommunicationParameters.Count);
}
