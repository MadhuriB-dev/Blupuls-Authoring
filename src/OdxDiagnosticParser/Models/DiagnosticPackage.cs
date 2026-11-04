namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents a complete diagnostic package derived from an ODX XML file or a PDX container.
/// </summary>
public class DiagnosticPackage
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string SourcePath { get; set; } = string.Empty;
    public bool IsFromPdx { get; set; }
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Metadata from PDX index.xml catalog (if extracted from PDX).
    /// </summary>
    public PdxCatalog? Catalog { get; set; }

    /// <summary>
    /// Diagnostic layers parsed from the package (ECU Variants, Base Variants, etc.).
    /// </summary>
    public List<DiagnosticLayer> Layers { get; set; } = new();

    /// <summary>
    /// Aggregated list of all ECU Variants across layers.
    /// </summary>
    public IEnumerable<DiagnosticLayer> EcuVariants =>
        Layers.Where(l => l.LayerType == DiagnosticLayerType.EcuVariant);

    /// <summary>
    /// Aggregated list of all Base Variants across layers.
    /// </summary>
    public IEnumerable<DiagnosticLayer> BaseVariants =>
        Layers.Where(l => l.LayerType == DiagnosticLayerType.BaseVariant);

    /// <summary>
    /// Total count of unique services across all layers.
    /// </summary>
    public int TotalServicesCount => Layers.Sum(l => l.Services.Count);

    /// <summary>
    /// Total count of Data Identifiers (DIDs) across all layers.
    /// </summary>
    public int TotalDidsCount => Layers.Sum(l => l.DataIdentifiers.Count);

    /// <summary>
    /// Total count of Diagnostic Trouble Codes (DTCs) across all layers.
    /// </summary>
    public int TotalDtcsCount => Layers.Sum(l => l.DiagnosticTroubleCodes.Count);

    /// <summary>
    /// Total count of Communication Parameters across all layers.
    /// </summary>
    public int TotalComParamsCount => Layers.Sum(l => l.CommunicationParameters.Count);
}

