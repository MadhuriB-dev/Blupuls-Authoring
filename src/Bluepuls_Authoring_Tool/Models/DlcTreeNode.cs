using System.Collections.ObjectModel;
using Bluepuls_Authoring_Tool.Utilities;
using OdxDiagnosticParser.Models;

namespace Bluepuls_Authoring_Tool.Models
{
    /// <summary>
    /// Represents a hierarchical node in the ISO 22901-1 / ISO 22901-2 Diagnostic Layer Containers (DLC) tree.
    /// Supports Root Containers, Vehicle Topology (ODX-V), PDX ABLOCKs, Protocols (ODX-C),
    /// Shared Data, Functional Groups, BaseVariants, ECU Variants, Parent Refs, Patterns, Services, DIDs, and DTCs.
    /// </summary>
    public class DlcTreeNode : ObservableObject
    {
        private bool _isExpanded;
        private bool _isSelected;
        private string _name = string.Empty;
        private string _nodeType = "Folder";
        private string _badgeText = string.Empty;

        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        public string NodeType
        {
            get => _nodeType;
            set { if (_nodeType != value) { _nodeType = value; OnPropertyChanged(); } }
        }

        public string BadgeText
        {
            get => _badgeText;
            set { if (_badgeText != value) { _badgeText = value; OnPropertyChanged(); } }
        }

        public string DataCategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public bool IsExpanded
        {
            get => _isExpanded;
            set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        // Attached diagnostic entities from OdxDiagnosticParser
        public DiagnosticService? ServiceData { get; set; }
        public DiagnosticDataIdentifier? DidData { get; set; }
        public DiagnosticTroubleCode? DtcData { get; set; }
        public DiagnosticLayer? LayerData { get; set; }
        public CommunicationParameter? ComParamData { get; set; }
        public VehicleInfoSpec? VehicleInfoData { get; set; }
        public LogicalLink? LogicalLinkData { get; set; }
        public ComParamSpecInfo? ComParamSpecData { get; set; }
        public LayerParentRef? ParentRefData { get; set; }
        public EcuVariantPattern? VariantPatternData { get; set; }
        public PdxAblock? AblockData { get; set; }

        /// <summary>
        /// Optional tab index hint (e.g., open Connections/Protocols/Manifest tab when clicking specialized ISO nodes).
        /// </summary>
        public int? PreferredTabIndex { get; set; }

        public RangeObservableCollection<DlcTreeNode> Children { get; set; } = new();
    }

    public class PackageMetadataItem
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class IsoConnectionRow
    {
        public string SourceLayer { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string RelationType { get; set; } = string.Empty;
        public string TargetLayer { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string DocRefContainer { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ExcludedItems { get; set; } = string.Empty;
        public DiagnosticLayer? SourceLayerObject { get; set; }
        public LayerParentRef? ParentRefObject { get; set; }
    }

    public class LogicalLinkRow
    {
        public string VehicleModel { get; set; } = string.Empty;
        public string LogicalLinkName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PhysicalBus { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public string BaseVariant { get; set; } = string.Empty;
        public string FunctionalGroup { get; set; } = string.Empty;
        public string PhysicalReqId { get; set; } = string.Empty;
        public string PhysicalRespId { get; set; } = string.Empty;
        public string FunctionalReqId { get; set; } = string.Empty;
    }

    public class VariantPatternRow
    {
        public string EcuVariant { get; set; } = string.Empty;
        public string PatternName { get; set; } = string.Empty;
        public string DiagService { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public DiagnosticLayer? LayerObject { get; set; }
        public EcuVariantPattern? PatternObject { get; set; }
    }

    public class AblockManifestRow
    {
        public string AblockName { get; set; } = string.Empty;
        public string IsoCategory { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string Checksum { get; set; } = string.Empty;
    }
}
