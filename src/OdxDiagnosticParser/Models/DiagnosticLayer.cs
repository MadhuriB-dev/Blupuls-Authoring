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
/// Represents an ODX diagnostic layer (e.g. ECU-VARIANT, BASE-VARIANT, PROTOCOL, FUNCTIONAL-GROUP, ECU-SHARED-DATA) per ISO 22901-1.
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
    /// ISO 22901-1 parent DIAG-LAYER-CONTAINER SHORT-NAME (used as DOCREF in cross-file PDX ODX-LINK references).
    /// </summary>
    public string ContainerShortName { get; set; } = string.Empty;

    /// <summary>
    /// ISO 22901-1 PARENT-REFS establishing inheritance links to BaseVariant, Protocol, FunctionalGroup, or EcuSharedData.
    /// </summary>
    public List<LayerParentRef> ParentRefs { get; set; } = new();

    /// <summary>
    /// True once ISO 22901-1 parent layer inheritance has been resolved onto this layer.
    /// </summary>
    public bool InheritanceResolved { get; set; }

    /// <summary>
    /// For PROTOCOL layers: reference to COMPARAM-SPEC in ODX-C.
    /// </summary>
    public string ComParamSpecRef { get; set; } = string.Empty;
    public string ComParamSpecDocRef { get; set; } = string.Empty;
    public string ProtStackShortName { get; set; } = string.Empty;

    /// <summary>
    /// For ECU-VARIANT layers: identification patterns used to match this ECU Variant at runtime.
    /// </summary>
    public List<EcuVariantPattern> VariantPatterns { get; set; } = new();

    /// <summary>
    /// Effective diagnostic services available on this layer (own services + inherited services per ISO 22901-1).
    /// </summary>
    public List<DiagnosticService> Services { get; set; } = new();

    /// <summary>
    /// Services directly defined in this layer (not inherited).
    /// </summary>
    public IEnumerable<DiagnosticService> OwnServices => Services.Where(s => !s.IsInherited);

    /// <summary>
    /// Services inherited from ParentRefs (BaseVariant, Protocol, FunctionalGroup, EcuSharedData).
    /// </summary>
    public IEnumerable<DiagnosticService> InheritedServices => Services.Where(s => s.IsInherited);

    /// <summary>
    /// Services categorized as Current Data (Live dynamic sensor telemetry / periodic).
    /// </summary>
    public IEnumerable<DiagnosticService> CurrentDataServices => Services.Where(s => s.DataCategory == "Current Data");

    /// <summary>
    /// Services categorized as Stored Data (Fault memory, DTCs, freeze frames, stored identification).
    /// </summary>
    public IEnumerable<DiagnosticService> StoredDataServices => Services.Where(s => s.DataCategory == "Stored Data");

    /// <summary>
    /// Services categorized as System Control (Diagnostic sessions, security access, routine control, tester present).
    /// </summary>
    public IEnumerable<DiagnosticService> SystemControlServices => Services.Where(s => s.DataCategory == "System Control");

    /// <summary>
    /// Data Identifiers (DIDs) resolved for this ECU / layer.
    /// </summary>
    public List<DiagnosticDataIdentifier> DataIdentifiers { get; set; } = new();

    /// <summary>
    /// Data Identifiers categorized as Current Data (Live sensors, RPM, coolant temp, voltage, speed).
    /// </summary>
    public IEnumerable<DiagnosticDataIdentifier> CurrentDataDids => DataIdentifiers.Where(d => d.DataCategory == "Current Data");

    /// <summary>
    /// Data Identifiers categorized as Stored Data (VIN, serial number, software version, calibration).
    /// </summary>
    public IEnumerable<DiagnosticDataIdentifier> StoredDataDids => DataIdentifiers.Where(d => d.DataCategory == "Stored Data");

    /// <summary>
    /// Diagnostic Trouble Codes (DTCs) defined for or inherited by this ECU / layer.
    /// </summary>
    public List<DiagnosticTroubleCode> DiagnosticTroubleCodes { get; set; } = new();

    /// <summary>
    /// Data Object Properties (DOPs) defining types, scaling, and enums.
    /// </summary>
    public List<DataObjectProperty> DataObjectProperties { get; set; } = new();

    /// <summary>
    /// Physical units parsed from UNIT-SPEC.
    /// </summary>
    public List<PhysicalUnitInfo> Units { get; set; } = new();

    /// <summary>
    /// ODX Tables / TableRows (keyed DID or Routine structures).
    /// </summary>
    public List<OdxTableRowInfo> TableRows { get; set; } = new();

    /// <summary>
    /// Communication parameters (CAN ID, Baudrate, Timings).
    /// </summary>
    public List<CommunicationParameter> CommunicationParameters { get; set; } = new();
}
