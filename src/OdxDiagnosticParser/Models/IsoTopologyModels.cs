namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents an ISO 22901-1 PARENT-REF relationship linking a child diagnostic layer
/// (e.g. ECU-VARIANT) to a parent layer (e.g. BASE-VARIANT, PROTOCOL, FUNCTIONAL-GROUP, ECU-SHARED-DATA).
/// </summary>
public class LayerParentRef
{
    public string IdRef { get; set; } = string.Empty;
    public string ShortNameRef { get; set; } = string.Empty;
    public string DocRef { get; set; } = string.Empty;
    public string DocType { get; set; } = "CONTAINER";
    public string ParentRefType { get; set; } = string.Empty; // BASE-VARIANT-REF, PROTOCOL-REF, FUNCTIONAL-GROUP-REF, ECU-SHARED-DATA-REF

    /// <summary>
    /// Resolved layer ID in the loaded DiagnosticPackage (if found).
    /// </summary>
    public string ResolvedLayerId { get; set; } = string.Empty;

    /// <summary>
    /// Resolved layer short name (if found).
    /// </summary>
    public string ResolvedLayerShortName { get; set; } = string.Empty;

    /// <summary>
    /// Resolved layer type enum string.
    /// </summary>
    public string ResolvedLayerType { get; set; } = string.Empty;

    /// <summary>
    /// True if the target layer was successfully located and linked.
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// Service short-names explicitly excluded via NOT-INHERITED-DIAG-COMMS.
    /// </summary>
    public List<string> NotInheritedDiagComms { get; set; } = new();

    /// <summary>
    /// DOP short-names explicitly excluded via NOT-INHERITED-DOPS.
    /// </summary>
    public List<string> NotInheritedDops { get; set; } = new();

    /// <summary>
    /// Table short-names explicitly excluded via NOT-INHERITED-TABLES.
    /// </summary>
    public List<string> NotInheritedTables { get; set; } = new();

    public string DisplayTarget => !string.IsNullOrEmpty(ResolvedLayerShortName)
        ? ResolvedLayerShortName
        : (!string.IsNullOrEmpty(ShortNameRef) ? ShortNameRef : IdRef);

    public string ResolutionStatusText => IsResolved ? "Linked (ISO 22901-1)" : "Unresolved External Ref";
}

/// <summary>
/// Represents an ECU-VARIANT-PATTERN used to match and identify an ECU Variant at runtime.
/// </summary>
public class EcuVariantPattern
{
    public string Name { get; set; } = string.Empty;
    public List<MatchingParameterInfo> MatchingParameters { get; set; } = new();

    public string Summary => MatchingParameters.Count == 0
        ? Name
        : string.Join(" AND ", MatchingParameters.Select(m => $"{m.DiagCommShortName}.{m.OutParamShortName} == '{m.ExpectedValue}'"));
}

public class MatchingParameterInfo
{
    public string DiagCommShortName { get; set; } = string.Empty;
    public string OutParamShortName { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
}

/// <summary>
/// Represents a physical engineering UNIT defined under UNIT-SPEC in ODX.
/// </summary>
public class PhysicalUnitInfo
{
    public string Id { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PhysicalDimensionRef { get; set; } = string.Empty;
    public double FactorSiToUnit { get; set; } = 1.0;
    public double OffsetSiToUnit { get; set; } = 0.0;
}

/// <summary>
/// Represents an ODX TABLE-ROW used for DID / Routine key-mapped data structures.
/// </summary>
public class OdxTableRowInfo
{
    public string TableId { get; set; } = string.Empty;
    public string TableShortName { get; set; } = string.Empty;
    public string RowId { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string KeyHex { get; set; } = string.Empty;
    public string Semantic { get; set; } = string.Empty;
    public string DopRef { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents an ISO 22901-1 ODX-V VEHICLE-INFO-SPEC vehicle topology definition.
/// </summary>
public class VehicleInfoSpec
{
    public string Id { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string ModelYear { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;

    public List<PhysicalVehicleLink> PhysicalVehicleLinks { get; set; } = new();
    public List<LogicalLink> LogicalLinks { get; set; } = new();
}

/// <summary>
/// Represents a PHYSICAL-VEHICLE-LINK (CAN, CAN-FD, DoIP Ethernet, LIN, FlexRay).
/// </summary>
public class PhysicalVehicleLink
{
    public string Id { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string LinkType { get; set; } = "CAN";
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents an ODX-V LOGICAL-LINK connecting an ECU physical bus address to a PROTOCOL, BASE-VARIANT, and FUNCTIONAL-GROUP.
/// </summary>
public class LogicalLink
{
    public string Id { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string LinkRole { get; set; } = "MEMBER-LOGICAL-LINK"; // GATEWAY-LOGICAL-LINK, MEMBER-LOGICAL-LINK, FUNCTIONAL-GROUP-LOGICAL-LINK

    public string PhysicalVehicleLinkRef { get; set; } = string.Empty;
    public string PhysicalVehicleLinkShortName { get; set; } = string.Empty;

    public string ProtocolRef { get; set; } = string.Empty;
    public string ProtocolDocRef { get; set; } = string.Empty;
    public string ResolvedProtocolName { get; set; } = string.Empty;

    public string BaseVariantRef { get; set; } = string.Empty;
    public string BaseVariantDocRef { get; set; } = string.Empty;
    public string ResolvedBaseVariantName { get; set; } = string.Empty;

    public string FunctionalGroupRef { get; set; } = string.Empty;
    public string FunctionalGroupDocRef { get; set; } = string.Empty;
    public string ResolvedFunctionalGroupName { get; set; } = string.Empty;

    public string PhysicalRequestId { get; set; } = string.Empty;
    public string PhysicalResponseId { get; set; } = string.Empty;
    public string FunctionalRequestId { get; set; } = string.Empty;

    public List<CommunicationParameter> LinkComParams { get; set; } = new();

    public string ConnectionSummary =>
        $"{ShortName} -> BV:{(string.IsNullOrEmpty(ResolvedBaseVariantName) ? BaseVariantRef : ResolvedBaseVariantName)} | Proto:{(string.IsNullOrEmpty(ResolvedProtocolName) ? ProtocolRef : ResolvedProtocolName)}";
}

/// <summary>
/// Represents an ISO 22901-1 ODX-C COMPARAM-SPEC defining protocol stacks and communication parameter definitions.
/// </summary>
public class ComParamSpecInfo
{
    public string Id { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;

    public List<ProtStackInfo> ProtStacks { get; set; } = new();
    public List<CommunicationParameter> DefaultComParams { get; set; } = new();
}

public class ProtStackInfo
{
    public string Id { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string PduProtocolType { get; set; } = string.Empty;
    public string PhysicalLinkType { get; set; } = string.Empty;
    public List<string> ComParamSubsetRefs { get; set; } = new();
}

/// <summary>
/// Represents an ISO 22901-1 / ISO 22901-2 validation or cross-reference consistency issue.
/// </summary>
public class OdxConsistencyIssue
{
    public string Severity { get; set; } = "Warning"; // Info, Warning, Error
    public string StandardRule { get; set; } = "ISO 22901-1";
    public string SourceObject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
