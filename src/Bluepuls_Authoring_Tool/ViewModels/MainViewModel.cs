using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Win32;
using Bluepuls_Authoring_Tool.Models;
using Bluepuls_Authoring_Tool.Utilities;
using OdxDiagnosticParser.Models;
using OdxDiagnosticParser.Parsers;
using OdxDiagnosticParser.Exporters;
using OdxDiagnosticParser.Samples;

namespace Bluepuls_Authoring_Tool.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private string _selectedOdxProject = "Vehicle_Architecture_Gen2.odx-d";
        private string _selectedEcu = "BV_ECM";
        private DiagnosticPackage? _lastParsedPackage;

        public ObservableCollection<DlcTreeNode> DiagnosticTreeNodes { get; } = new();

        public ObservableCollection<string> OdxProjects { get; } = new()
        {
            "Vehicle_Architecture_Gen2.odx-d",
            "Powertrain_EDC17C69.odx-d",
            "Body_Control_BCM_High.odx-d",
            "ElectricDrive_Inverter_Gen4.odx-d"
        };

        public ObservableCollection<string> EcuList { get; } = new()
        {
            "BV_ECM",
            "BV_RCM",
            "ECM_Bosch_MD1",
            "TCU_9G"
        };

        public ObservableCollection<DiagnosticLogItem> CommunicationLogs { get; } = new();

        public string SelectedOdxProject { get => _selectedOdxProject; set { _selectedOdxProject = value; OnPropertyChanged(); } }
        public string SelectedEcu { get => _selectedEcu; set { _selectedEcu = value; OnPropertyChanged(); } }

        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ConsistencyCheckCommand { get; }

        public MainViewModel()
        {
            InitializeDlcTreeStructure();
            ImportCommand = new RelayCommand(ExecuteImport);
            ExportCommand = new RelayCommand(ExecuteExport);
            ConsistencyCheckCommand = new RelayCommand(ExecuteConsistencyCheck);
        }

        private void InitializeDlcTreeStructure()
        {
            var rootNode = new DlcTreeNode
            {
                Name = "Diagnostic Layer Containers",
                NodeType = "RootFolder",
                IsExpanded = true
            };

            rootNode.Children.Add(new DlcTreeNode { Name = "DLC_SD_UNITS", NodeType = "Container" });
            rootNode.Children.Add(new DlcTreeNode { Name = "DLC_SD_ExP_DOPS", NodeType = "Container" });
            rootNode.Children.Add(new DlcTreeNode { Name = "DLC_SD_DTSJobLibrary", NodeType = "Container" });
            rootNode.Children.Add(new DlcTreeNode { Name = "DLC_SD_DOPS", NodeType = "Container" });
            rootNode.Children.Add(new DlcTreeNode { Name = "DLC_SD_15765_3_Services", NodeType = "Container" });
            rootNode.Children.Add(new DlcTreeNode { Name = "DLC_PR_UDSonCAN", NodeType = "Container" });
            rootNode.Children.Add(new DlcTreeNode { Name = "DLC_BV_RCM", NodeType = "Container" });

            var ecmContainer = new DlcTreeNode
            {
                Name = "DLC_BV_ECM",
                NodeType = "Container",
                IsExpanded = true
            };

            ecmContainer.Children.Add(new DlcTreeNode { Name = "Shared Data", NodeType = "Folder" });
            ecmContainer.Children.Add(new DlcTreeNode { Name = "Protocols", NodeType = "Folder" });
            ecmContainer.Children.Add(new DlcTreeNode { Name = "Functional Groups", NodeType = "Folder" });

            var baseVariantsFolder = new DlcTreeNode { Name = "ECU BaseVariants", NodeType = "Folder", IsExpanded = true };

            var bvNode = new DlcTreeNode
            {
                Name = "BV_ECM",
                NodeType = "BaseVariant",
                IsExpanded = true,
                IsSelected = true
            };

            bvNode.Children.Add(new DlcTreeNode { Name = "[22] ReadDataByIdentifier [SD_15765_3_Services]", NodeType = "ServiceGreen" });
            bvNode.Children.Add(new DlcTreeNode { Name = "[22] ReadDataByIdentifier_BootSoftwareIdentification", NodeType = "ServiceGreen" });
            bvNode.Children.Add(new DlcTreeNode { Name = "[22] ReadDataByIdentifier_CalibrationRepairShopCode", NodeType = "ServiceGreen" });
            bvNode.Children.Add(new DlcTreeNode { Name = "[22] ReadDataByIdentifier_ECUSerialNumber", NodeType = "ServiceGreen" });
            bvNode.Children.Add(new DlcTreeNode { Name = "[22] ReadDataByIdentifier_EngineCoolingTemperature", NodeType = "ServiceGreen" });

            baseVariantsFolder.Children.Add(bvNode);
            ecmContainer.Children.Add(baseVariantsFolder);

            ecmContainer.Children.Add(new DlcTreeNode { Name = "ECU Variants", NodeType = "Folder" });

            rootNode.Children.Add(ecmContainer);

            DiagnosticTreeNodes.Add(rootNode);
        }

        private void ExecuteImport(object? parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Import Diagnostic Database (ODX / XML / CSV)",
                Filter = "Diagnostic Files (*.odx;*.odx-d;*.pdx;*.xml;*.csv)|*.odx;*.odx-d;*.pdx;*.xml;*.csv|ODX Files (*.odx;*.odx-d;*.pdx)|*.odx;*.odx-d;*.pdx|XML Files (*.xml)|*.xml|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(filePath);
                string ext = Path.GetExtension(filePath).ToLowerInvariant();

                try
                {
                    var rootNode = DiagnosticTreeNodes.FirstOrDefault();
                    if (rootNode == null)
                    {
                        rootNode = new DlcTreeNode { Name = "Diagnostic Layer Containers", NodeType = "RootFolder", IsExpanded = true };
                        DiagnosticTreeNodes.Add(rootNode);
                    }

                    int importedServices = 0;

                    if (ext == ".pdx" || ext == ".zip")
                    {
                        var parser = new OdxXmlParser();
                        var extractor = new PdxPackageExtractor(parser);
                        _lastParsedPackage = extractor.ExtractAndParse(filePath);
                        PopulateTreeFromDiagnosticPackage(_lastParsedPackage, rootNode, ref importedServices);
                    }
                    else if (ext == ".odx" || ext == ".odx-d" || ext == ".xml")
                    {
                        var parser = new OdxXmlParser();
                        try
                        {
                            _lastParsedPackage = parser.ParseFile(filePath);
                            if (_lastParsedPackage.Layers.Any(l => l.Services.Any()))
                            {
                                PopulateTreeFromDiagnosticPackage(_lastParsedPackage, rootNode, ref importedServices);
                            }
                            else
                            {
                                ParseSimpleXml(filePath, fileName, rootNode, ref importedServices);
                            }
                        }
                        catch
                        {
                            ParseSimpleXml(filePath, fileName, rootNode, ref importedServices);
                        }
                    }
                    else if (ext == ".csv")
                    {
                        var lines = File.ReadAllLines(filePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                        string dlcName = $"DLC_{Path.GetFileNameWithoutExtension(fileName)}";
                        var dlcNode = new DlcTreeNode { Name = dlcName, NodeType = "Container", IsExpanded = true };
                        var bvFolder = new DlcTreeNode { Name = "ECU BaseVariants", NodeType = "Folder", IsExpanded = true };
                        var bvNode = new DlcTreeNode { Name = $"BV_{Path.GetFileNameWithoutExtension(fileName)}", NodeType = "BaseVariant", IsExpanded = true };

                        foreach (var line in lines.Skip(1))
                        {
                            var parts = line.Split(',');
                            string svcName = parts.Length > 1 ? $"{parts[0].Trim()} {parts[1].Trim()}" : parts[0].Trim();
                            bvNode.Children.Add(new DlcTreeNode { Name = svcName, NodeType = "ServiceGreen" });
                            importedServices++;
                        }

                        bvFolder.Children.Add(bvNode);
                        dlcNode.Children.Add(bvFolder);
                        rootNode.Children.Add(dlcNode);
                    }
                    else
                    {
                        string dlcName = $"DLC_{Path.GetFileNameWithoutExtension(fileName)}";
                        rootNode.Children.Add(new DlcTreeNode { Name = dlcName, NodeType = "Container", IsExpanded = true });
                    }

                    if (!OdxProjects.Contains(fileName))
                    {
                        OdxProjects.Add(fileName);
                    }
                    SelectedOdxProject = fileName;

                    MessageBox.Show($"Import succeeded -- File '{fileName}' loaded successfully ({importedServices} service item(s) imported).", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed -- {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteExport(object? parameter)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Export Diagnostic Layer Containers",
                FileName = Path.GetFileNameWithoutExtension(SelectedOdxProject) + "_Export",
                DefaultExt = ".odx-d",
                Filter = "ODX-D Diagnostic File (*.odx-d)|*.odx-d|Excel Diagnostic Report (*.xlsx)|*.xlsx|XML File (*.xml)|*.xml|CSV Service List (*.csv)|*.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                string fileName = Path.GetFileName(filePath);
                string ext = Path.GetExtension(filePath).ToLowerInvariant();

                try
                {
                    int exportedNodes = 0;

                    if (ext == ".xlsx")
                    {
                        var exporter = new OdxExcelExporter();
                        var packageToExport = _lastParsedPackage;
                        if (packageToExport == null)
                        {
                            var xmlContent = SampleOdxGenerator.CreateSampleOdxXml(SelectedEcu);
                            var parser = new OdxXmlParser();
                            packageToExport = parser.ParseXml(xmlContent, fileName);
                        }
                        exporter.ExportToExcel(packageToExport, filePath);
                        exportedNodes = packageToExport.TotalServicesCount;
                    }
                    else if (ext == ".csv")
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Container,Folder,BaseVariant,Service");
                        foreach (var root in DiagnosticTreeNodes)
                        {
                            ExportNodeToCsv(root, "", "", "", sb, ref exportedNodes);
                        }
                        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                    }
                    else
                    {
                        var rootElement = new XElement("ODX",
                            new XAttribute("MODEL-VERSION", "2.2.0"),
                            new XAttribute("ExportedAt", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")));

                        foreach (var node in DiagnosticTreeNodes)
                        {
                            rootElement.Add(SerializeNodeToXml(node, ref exportedNodes));
                        }

                        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), rootElement);
                        doc.Save(filePath);
                    }

                    MessageBox.Show($"Export succeeded -- Saved '{fileName}' ({exportedNodes} diagnostic node(s) exported).", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed -- {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PopulateTreeFromDiagnosticPackage(DiagnosticPackage package, DlcTreeNode rootNode, ref int count)
        {
            foreach (var layer in package.Layers)
            {
                var dlcNode = new DlcTreeNode
                {
                    Name = $"DLC_{layer.ShortName}",
                    NodeType = "Container",
                    IsExpanded = true
                };

                if (layer.CommunicationParameters.Any())
                {
                    var protocolsFolder = new DlcTreeNode { Name = "Protocols", NodeType = "Folder", IsExpanded = false };
                    foreach (var cp in layer.CommunicationParameters)
                    {
                        protocolsFolder.Children.Add(new DlcTreeNode { Name = $"{cp.Name} = {cp.Value} {cp.Unit}".Trim(), NodeType = "Folder" });
                    }
                    dlcNode.Children.Add(protocolsFolder);
                }

                var bvFolder = new DlcTreeNode { Name = "ECU BaseVariants", NodeType = "Folder", IsExpanded = true };
                var bvNode = new DlcTreeNode
                {
                    Name = $"BV_{layer.ShortName}",
                    NodeType = "BaseVariant",
                    IsExpanded = true,
                    IsSelected = true
                };

                // Add Services
                foreach (var svc in layer.Services)
                {
                    string sidPrefix = !string.IsNullOrEmpty(svc.ServiceIdHex) ? $"[{svc.ServiceIdHex}] " : "";
                    bvNode.Children.Add(new DlcTreeNode
                    {
                        Name = $"{sidPrefix}{svc.ShortName} ({svc.DataCategory})",
                        NodeType = "ServiceGreen"
                    });
                    count++;

                    CommunicationLogs.Add(new DiagnosticLogItem
                    {
                        Timestamp = DateTime.Now,
                        Direction = "RX",
                        ServiceId = svc.ServiceIdHex,
                        Description = svc.ShortName,
                        RawData = $"{svc.ServiceIdHex} {svc.SubFunctionHex}".Trim(),
                        Status = "Parsed"
                    });
                }

                // Add DIDs
                if (layer.DataIdentifiers.Any())
                {
                    var didsFolder = new DlcTreeNode { Name = "Data Identifiers (DIDs)", NodeType = "Folder", IsExpanded = false };
                    foreach (var did in layer.DataIdentifiers)
                    {
                        string unitStr = !string.IsNullOrEmpty(did.Unit) ? $" [{did.Unit}]" : "";
                        didsFolder.Children.Add(new DlcTreeNode
                        {
                            Name = $"[{did.DidHex}] {did.ShortName} ({did.DataCategory}){unitStr}",
                            NodeType = "Folder"
                        });
                        count++;
                    }
                    bvNode.Children.Add(didsFolder);
                }

                bvFolder.Children.Add(bvNode);
                dlcNode.Children.Add(bvFolder);
                rootNode.Children.Add(dlcNode);
            }
        }

        private void ParseSimpleXml(string filePath, string fileName, DlcTreeNode rootNode, ref int importedServices)
        {
            var doc = XDocument.Load(filePath);
            var dlcElements = doc.Descendants("DLC").ToList();
            if (dlcElements.Any())
            {
                foreach (var dlcEl in dlcElements)
                {
                    string dlcName = dlcEl.Attribute("Name")?.Value ?? $"DLC_Imported_{Path.GetFileNameWithoutExtension(fileName)}";
                    var dlcNode = new DlcTreeNode { Name = dlcName, NodeType = "Container", IsExpanded = true };
                    var bvFolder = new DlcTreeNode { Name = "ECU BaseVariants", NodeType = "Folder", IsExpanded = true };
                    var bvNode = new DlcTreeNode { Name = dlcEl.Attribute("BaseVariant")?.Value ?? "BV_Imported", NodeType = "BaseVariant", IsExpanded = true };

                    foreach (var svcEl in dlcEl.Descendants("Service"))
                    {
                        string svcName = svcEl.Attribute("Name")?.Value ?? svcEl.Value.Trim();
                        if (!string.IsNullOrWhiteSpace(svcName))
                        {
                            bvNode.Children.Add(new DlcTreeNode { Name = svcName, NodeType = "ServiceGreen" });
                            importedServices++;
                        }
                    }

                    bvFolder.Children.Add(bvNode);
                    dlcNode.Children.Add(bvFolder);
                    rootNode.Children.Add(dlcNode);
                }
            }
            else
            {
                string dlcName = $"DLC_{Path.GetFileNameWithoutExtension(fileName)}";
                var dlcNode = new DlcTreeNode { Name = dlcName, NodeType = "Container", IsExpanded = true };
                var bvFolder = new DlcTreeNode { Name = "ECU BaseVariants", NodeType = "Folder", IsExpanded = true };
                var bvNode = new DlcTreeNode { Name = $"BV_{Path.GetFileNameWithoutExtension(fileName)}", NodeType = "BaseVariant", IsExpanded = true };

                foreach (var el in doc.Root?.Elements() ?? Enumerable.Empty<XElement>())
                {
                    string label = el.Attribute("ShortName")?.Value ?? el.Attribute("Name")?.Value ?? el.Name.LocalName;
                    bvNode.Children.Add(new DlcTreeNode { Name = label, NodeType = "ServiceGreen" });
                    importedServices++;
                }

                bvFolder.Children.Add(bvNode);
                dlcNode.Children.Add(bvFolder);
                rootNode.Children.Add(dlcNode);
            }
        }

        private XElement SerializeNodeToXml(DlcTreeNode node, ref int count)
        {
            count++;
            string tagName = node.NodeType switch
            {
                "RootFolder" => "DIAGNOSTIC-LAYER-CONTAINERS",
                "Container" => "DLC",
                "BaseVariant" => "BASE-VARIANT",
                "ServiceGreen" => "Service",
                _ => "Folder"
            };

            var el = new XElement(tagName, new XAttribute("Name", node.Name));
            foreach (var child in node.Children)
            {
                el.Add(SerializeNodeToXml(child, ref count));
            }
            return el;
        }

        private void ExportNodeToCsv(DlcTreeNode node, string dlc, string folder, string bv, StringBuilder sb, ref int count)
        {
            count++;
            if (node.NodeType == "Container") dlc = node.Name;
            else if (node.NodeType == "Folder") folder = node.Name;
            else if (node.NodeType == "BaseVariant") bv = node.Name;
            else if (node.NodeType == "ServiceGreen")
            {
                sb.AppendLine($"\"{dlc}\",\"{folder}\",\"{bv}\",\"{node.Name}\"");
            }

            foreach (var child in node.Children)
            {
                ExportNodeToCsv(child, dlc, folder, bv, sb, ref count);
            }
        }

        private void ExecuteConsistencyCheck(object? parameter)
        {
            MessageBox.Show("Check succeeded -- database is consistent! Consistency 0 error(s), 0 warning(s)", "Consistency Check", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}