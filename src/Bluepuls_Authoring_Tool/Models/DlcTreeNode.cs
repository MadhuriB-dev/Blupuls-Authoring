using System.Collections.ObjectModel;

namespace Bluepuls_Authoring_Tool.Models
{
    public class DlcTreeNode
    {
        public string Name { get; set; } = string.Empty;
        public string NodeType { get; set; } = "Folder";
        public bool IsExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public ObservableCollection<DlcTreeNode> Children { get; set; } = new();
    }
}