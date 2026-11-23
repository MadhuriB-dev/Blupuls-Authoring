using System.Collections.ObjectModel;
using OdxDiagnosticParser.Models;

namespace Bluepuls_Authoring_Tool.Models
{
    /// <summary>
    /// Represents a hierarchical node in the Diagnostic Layer Containers (DLC) tree.
    /// Can represent Root Containers, Folders, BaseVariants, ECU Variants, Diagnostic Services, DIDs, or DTCs.
    /// </summary>
    public class DlcTreeNode
    {
        public string Name { get; set; } = string.Empty;
        public string NodeType { get; set; } = "Folder";
        public bool IsExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public string BadgeText { get; set; } = string.Empty;
        public string DataCategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Attached diagnostic entities from OdxDiagnosticParser
        public DiagnosticService? ServiceData { get; set; }
        public DiagnosticDataIdentifier? DidData { get; set; }
        public DiagnosticTroubleCode? DtcData { get; set; }
        public DiagnosticLayer? LayerData { get; set; }
        public CommunicationParameter? ComParamData { get; set; }

        public ObservableCollection<DlcTreeNode> Children { get; set; } = new();
    }
}
