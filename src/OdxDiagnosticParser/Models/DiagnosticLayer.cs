namespace OdxDiagnosticParser.Models;

public enum DiagnosticLayerType
{
    BaseVariant,
    EcuVariant,
    EcuSharedData,
    Protocol,
    FunctionalGroup
}

/// <summary>
/// Represents an ODX diagnostic layer (e.g. ECU-VARIANT, BASE-VARIANT, PROTOCOL).
/// </summary>
public class DiagnosticLayer
{
    public DiagnosticLayerType LayerType { get; set; } = DiagnosticLayerType.EcuVariant;
    public string Id { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = "2.2.0";
    public string SourceFileName { get; set; } = string.Empty;

    /// <summary>
    /// Diagnostic services defined in or inherited by this layer.
    /// </summary>
    public List<DiagnosticService> Services { get; set; } = new();

    /// <summary>
    /// Data Identifiers (DIDs) resolved for this ECU / layer.
    /// </summary>
    public List<DiagnosticDataIdentifier> DataIdentifiers { get; set; } = new();

    /// <summary>
    /// Diagnostic Trouble Codes (DTCs) defined for this ECU / layer.
    /// </summary>
    public List<DiagnosticTroubleCode> DiagnosticTroubleCodes { get; set; } = new();

    /// <summary>
    /// Data Object Properties (DOPs) defining types, scaling, and enums.
    /// </summary>
    public List<DataObjectProperty> DataObjectProperties { get; set; } = new();

    /// <summary>
    /// Communication parameters (CAN ID, Baudrate, Timings).
    /// </summary>
    public List<CommunicationParameter> CommunicationParameters { get; set; } = new();
}

