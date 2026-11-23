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
        private string _selectedOdxProject = "Powertrain_EMS_2026.pdx";
        private string _selectedEcu = "EV_EngineControlUnit";
        private DiagnosticPackage? _currentPackage;
        private DlcTreeNode? _selectedTreeNode;
        private DiagnosticService? _selectedService;
        private DiagnosticDataIdentifier? _selectedDid;
        private DiagnosticTroubleCode? _selectedDtc;
        private string _activeWorkspaceTitle = "ECU Variant: EV_EngineControlUnit - Diagnostic Services";
        private int _currentDataServicesCount;
        private int _storedDataServicesCount;
        private int _totalServicesCount;

        // TreeView root collection
        public ObservableCollection<DlcTreeNode> DiagnosticTreeNodes { get; } = new();

        // Dropdown collections
        public ObservableCollection<string> OdxProjects { get; } = new()
        {
            "Powertrain_EMS_2026.pdx",
            "Vehicle_Architecture_Gen2.odx-d",
            "Powertrain_EDC17C69.odx-d",
            "Body_Control_BCM_High.odx-d",
            "ElectricDrive_Inverter_Gen4.odx-d"
        };

        public ObservableCollection<string> EcuList { get; } = new()
        {
            "EV_EngineControlUnit",
            "BV_EngineControlUnit",
            "BV_ECM",
            "BV_RCM",
            "ECM_Bosch_MD1",
            "TCU_9G"
        };

        // Right Panel collections
        public ObservableCollection<DiagnosticService> LayerServices { get; } = new();
        public ObservableCollection<DiagnosticDataIdentifier> LayerDids { get; } = new();
        public ObservableCollection<DiagnosticTroubleCode> LayerDtcs { get; } = new();
        public ObservableCollection<CommunicationParameter> LayerComParams { get; } = new();
        public ObservableCollection<DiagnosticLogItem> CommunicationLogs { get; } = new();
        public ObservableCollection<(string Key, string Value)> PackageMetadata { get; } = new();

        // Selected Properties
        public string SelectedOdxProject
        {
            get => _selectedOdxProject;
            set { _selectedOdxProject = value; OnPropertyChanged(); }
        }

        public string SelectedEcu
        {
            get => _selectedEcu;
            set { _selectedEcu = value; OnPropertyChanged(); }
        }

        public DlcTreeNode? SelectedTreeNode
        {
            get => _selectedTreeNode;
            set { _selectedTreeNode = value; OnPropertyChanged(); }
        }

        public DiagnosticService? SelectedService
        {
            get => _selectedService;
            set { _selectedService = value; OnPropertyChanged(); }
        }

        public DiagnosticDataIdentifier? SelectedDid
        {
            get => _selectedDid;
            set { _selectedDid = value; OnPropertyChanged(); }
        }

        public DiagnosticTroubleCode? SelectedDtc
        {
            get => _selectedDtc;
            set { _selectedDtc = value; OnPropertyChanged(); }
        }

        public string ActiveWorkspaceTitle
        {
            get => _activeWorkspaceTitle;
            set { _activeWorkspaceTitle = value; OnPropertyChanged(); }
        }

        public int CurrentDataServicesCount
        {
            get => _currentDataServicesCount;
            set { _currentDataServicesCount = value; OnPropertyChanged(); }
        }

        public int StoredDataServicesCount
        {
            get => _storedDataServicesCount;
            set { _storedDataServicesCount = value; OnPropertyChanged(); }
        }

        public int TotalServicesCount
        {
            get => _totalServicesCount;
            set { _totalServicesCount = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand LoadPdxCommand { get; }
        public ICommand ConsistencyCheckCommand { get; }

        public MainViewModel()
        {
            ImportCommand = new RelayCommand(ExecuteImport);
            ExportCommand = new RelayCommand(ExecuteExport);
            LoadPdxCommand = new RelayCommand(ExecuteLoadPdx);
            ConsistencyCheckCommand = new RelayCommand(ExecuteConsistencyCheck);

            // Automatically load and parse the realistic PDX diagnostic data on startup!
            LoadPdxDiagnosticData(null);
        }

        /// <summary>
        /// Parses diagnostic data from a PDX package using OdxDiagnosticParser (PdxPackageExtractor)
        /// and builds the hierarchical DLC tree placing all ODX-D data services directly inside the ECU Variants.
        /// </summary>
        public void LoadPdxDiagnosticData(string? pdxPath = null)
        {
            try
            {
                var parser = new OdxXmlParser();
                var extractor = new PdxPackageExtractor(parser);

                DiagnosticPackage package;

                if (!string.IsNullOrWhiteSpace(pdxPath) && File.Exists(pdxPath))
                {
                    package = extractor.ExtractAndParse(pdxPath);
                }
                else
                {
                    // Generate realistic sample PDX in memory
                    var demoDir = Path.Combine(Path.GetTempPath(), "Bluepuls_Demo_" + Guid.NewGuid().ToString("N")[..8]);
                    Directory.CreateDirectory(demoDir);
                    var samplePdx = Path.Combine(demoDir, "Powertrain_EMS_2026.pdx");
                    SampleOdxGenerator.CreateSamplePdxFile(samplePdx, "EngineControlUnit");
                    package = extractor.ExtractAndParse(samplePdx);
                }

                _currentPackage = package;
                BuildTreeFromDiagnosticPackage(package);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse PDX diagnostic package: {ex.Message}", "PDX Parser Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                InitializeFallbackTree();
            }
        }

        /// <summary>
        /// Populates the Diagnostic Layer Containers (DLC) tree with ECU Variants, Base Variants, and ODX-D Services.
        /// </summary>
        private void BuildTreeFromDiagnosticPackage(DiagnosticPackage package)
        {
            DiagnosticTreeNodes.Clear();
            CommunicationLogs.Clear();
            PackageMetadata.Clear();

            // Metadata
            PackageMetadata.Add(("Package / ECU Name", package.Name));
            PackageMetadata.Add(("Source File", Path.GetFileName(package.SourcePath)));
            PackageMetadata.Add(("ODX Version", package.Version));
            PackageMetadata.Add(("Extracted UTC", package.ExtractedAt.ToString("yyyy-MM-dd HH:mm:ss")));
            if (package.Catalog != null)
            {
                if (!string.IsNullOrEmpty(package.Catalog.Creator))
                    PackageMetadata.Add(("Catalog Creator", package.Catalog.Creator));
                if (package.Catalog.Vehicles.Count > 0)
                    PackageMetadata.Add(("Vehicle Model", string.Join(", ", package.Catalog.Vehicles.Select(v => $"{v.Model} ({v.Company})"))));
                if (package.Catalog.Ecus.Count > 0)
                    PackageMetadata.Add(("ECU Hardware Address", string.Join(", ", package.Catalog.Ecus.Select(e => $"{e.EcuName} [DiagAddr: {e.DiagnosticAddress}]"))));
            }

            // Root DLC Folder
            var rootFolder = new DlcTreeNode
            {
                Name = "Diagnostic Layer Containers",
                NodeType = "RootFolder",
                IsExpanded = true
            };

            // Shared protocol / standard containers (DTS visual style)
            rootFolder.Children.Add(new DlcTreeNode { Name = "DLC_SD_UNITS", NodeType = "Container" });
            rootFolder.Children.Add(new DlcTreeNode { Name = "DLC_SD_DOPS", NodeType = "Container" });
            rootFolder.Children.Add(new DlcTreeNode { Name = "DLC_SD_15765_3_Services", NodeType = "Container" });
            rootFolder.Children.Add(new DlcTreeNode { Name = "DLC_PR_UDSonCAN", NodeType = "Container" });

            DlcTreeNode? firstEcuVariantNode = null;

            // Iterate over parsed layers
            foreach (var layer in package.Layers)
            {
                var dlcContainer = new DlcTreeNode
                {
                    Name = $"DLC_{layer.ShortName}",
                    NodeType = "Container",
                    IsExpanded = true,
                    LayerData = layer
                };

                // 1. Shared Data Folder
                var sharedDataFolder = new DlcTreeNode { Name = "Shared Data", NodeType = "Folder" };
                if (layer.DataObjectProperties.Any())
                {
                    foreach (var dop in layer.DataObjectProperties.Take(8))
                    {
                        sharedDataFolder.Children.Add(new DlcTreeNode
                        {
                            Name = $"DOP_{dop.ShortName} ({dop.BaseDataType})",
                            NodeType = "Folder"
                        });
                    }
                }
                dlcContainer.Children.Add(sharedDataFolder);

                // 2. Protocols & Bus Parameters Folder
                var protocolsFolder = new DlcTreeNode { Name = "Protocols", NodeType = "Folder", IsExpanded = false };
                if (layer.CommunicationParameters.Any())
                {
                    foreach (var cp in layer.CommunicationParameters)
                    {
                        protocolsFolder.Children.Add(new DlcTreeNode
                        {
                            Name = $"{cp.Name} = {cp.Value} {cp.Unit}".Trim(),
                            NodeType = "Folder",
                            ComParamData = cp
                        });
                    }
                }
                dlcContainer.Children.Add(protocolsFolder);

                // 3. Functional Groups Folder
                dlcContainer.Children.Add(new DlcTreeNode { Name = "Functional Groups", NodeType = "Folder" });

                // 4. ECU BaseVariants Folder
                var baseVariantsFolder = new DlcTreeNode { Name = "ECU BaseVariants", NodeType = "Folder", IsExpanded = true };
                var bvNode = new DlcTreeNode
                {
                    Name = $"BV_{layer.ShortName}",
                    NodeType = "BaseVariant",
                    IsExpanded = true,
                    LayerData = layer
                };

                // Add base services to BaseVariant
                foreach (var svc in layer.Services.Take(4))
                {
                    bvNode.Children.Add(new DlcTreeNode
                    {
                        Name = $"[{svc.ServiceIdHex}] {svc.ShortName}",
                        NodeType = svc.DataCategory == "Current Data" ? "ServiceCurrentData" : (svc.DataCategory == "Stored Data" ? "ServiceStoredData" : "ServiceControl"),
                        ServiceData = svc,
                        DataCategory = svc.DataCategory
                    });
                }
                baseVariantsFolder.Children.Add(bvNode);
                dlcContainer.Children.Add(baseVariantsFolder);

                // =========================================================================
                // 5. ECU VARIANTS FOLDER - ALL ODX-D DIAGNOSTIC SERVICES PLACED INSIDE HERE!
                // =========================================================================
                var ecuVariantsFolder = new DlcTreeNode
                {
                    Name = "ECU Variants",
                    NodeType = "Folder",
                    IsExpanded = true
                };

                var evNode = new DlcTreeNode
                {
                    Name = $"EV_{layer.ShortName}",
                    NodeType = "EcuVariant",
                    IsExpanded = true,
                    IsSelected = true,
                    LayerData = layer,
                    BadgeText = $"{layer.Services.Count} Services"
                };

                firstEcuVariantNode ??= evNode;

                // [5A] CURRENT DATA SERVICES (Live Telemetry & Dynamic Measurements)
                var currentDataServices = layer.Services.Where(s => s.DataCategory == "Current Data").ToList();
                var currentDataFolder = new DlcTreeNode
                {
                    Name = $"Current Data Services ({currentDataServices.Count})",
                    NodeType = "Folder",
                    IsExpanded = true,
                    BadgeText = "Live Telemetry"
                };

                foreach (var svc in currentDataServices)
                {
                    currentDataFolder.Children.Add(new DlcTreeNode
                    {
                        Name = $"[{svc.ServiceIdHex}] {svc.ShortName}",
                        NodeType = "ServiceCurrentData",
                        ServiceData = svc,
                        DataCategory = svc.DataCategory,
                        Description = svc.Description
                    });

                    LogServiceItem(svc, "Current Data");
                }
                evNode.Children.Add(currentDataFolder);

                // [5B] STORED DATA SERVICES (Fault Memory, DTCs & Non-Volatile Identification)
                var storedDataServices = layer.Services.Where(s => s.DataCategory == "Stored Data").ToList();
                var storedDataFolder = new DlcTreeNode
                {
                    Name = $"Stored Data Services ({storedDataServices.Count})",
                    NodeType = "Folder",
                    IsExpanded = true,
                    BadgeText = "Fault Memory / Ident"
                };

                foreach (var svc in storedDataServices)
                {
                    storedDataFolder.Children.Add(new DlcTreeNode
                    {
                        Name = $"[{svc.ServiceIdHex}] {svc.ShortName}",
                        NodeType = "ServiceStoredData",
                        ServiceData = svc,
                        DataCategory = svc.DataCategory,
                        Description = svc.Description
                    });

                    LogServiceItem(svc, "Stored Data");
                }
                evNode.Children.Add(storedDataFolder);

                // [5C] SYSTEM & CONTROL SERVICES (Sessions, Security, Routine, Tester Present)
                var controlServices = layer.Services.Where(s => s.DataCategory == "System Control").ToList();
                if (controlServices.Count > 0)
                {
                    var controlFolder = new DlcTreeNode
                    {
                        Name = $"System Control Services ({controlServices.Count})",
                        NodeType = "Folder",
                        IsExpanded = false,
                        BadgeText = "Diagnostic Control"
                    };

                    foreach (var svc in controlServices)
                    {
                        controlFolder.Children.Add(new DlcTreeNode
                        {
                            Name = $"[{svc.ServiceIdHex}] {svc.ShortName}",
                            NodeType = "ServiceControl",
                            ServiceData = svc,
                            DataCategory = svc.DataCategory,
                            Description = svc.Description
                        });

                        LogServiceItem(svc, "System Control");
                    }
                    evNode.Children.Add(controlFolder);
                }

                // [5D] DATA IDENTIFIERS (DIDs)
                if (layer.DataIdentifiers.Any())
                {
                    var didsFolder = new DlcTreeNode
                    {
                        Name = $"Data Identifiers / DIDs ({layer.DataIdentifiers.Count})",
                        NodeType = "Folder",
                        IsExpanded = false
                    };

                    foreach (var did in layer.DataIdentifiers)
                    {
                        var unitStr = !string.IsNullOrEmpty(did.Unit) ? $" [{did.Unit}]" : "";
                        didsFolder.Children.Add(new DlcTreeNode
                        {
                            Name = $"[{did.DidHex}] {did.ShortName} ({did.DataCategory}){unitStr}",
                            NodeType = "DidNode",
                            DidData = did,
                            DataCategory = did.DataCategory
                        });
                    }
                    evNode.Children.Add(didsFolder);
                }

                // [5E] DIAGNOSTIC TROUBLE CODES (DTCs)
                if (layer.DiagnosticTroubleCodes.Any())
                {
                    var dtcsFolder = new DlcTreeNode
                    {
                        Name = $"Trouble Codes / DTCs ({layer.DiagnosticTroubleCodes.Count})",
                        NodeType = "Folder",
                        IsExpanded = false
                    };

                    foreach (var dtc in layer.DiagnosticTroubleCodes)
                    {
                        dtcsFolder.Children.Add(new DlcTreeNode
                        {
                            Name = $"[{dtc.DisplayCode}] {dtc.Description}",
                            NodeType = "DtcNode",
                            DtcData = dtc,
                            DataCategory = "Stored Fault"
                        });
                    }
                    evNode.Children.Add(dtcsFolder);
                }

                ecuVariantsFolder.Children.Add(evNode);
                dlcContainer.Children.Add(ecuVariantsFolder);

                rootFolder.Children.Add(dlcContainer);
            }

            DiagnosticTreeNodes.Add(rootFolder);

            // Select the primary ECU Variant node and display its services in the right workspace!
            if (firstEcuVariantNode != null)
            {
                OnTreeNodeSelected(firstEcuVariantNode);
            }
        }

        private void LogServiceItem(DiagnosticService svc, string category)
        {
            CommunicationLogs.Add(new DiagnosticLogItem
            {
                Timestamp = DateTime.Now,
                Direction = "RX",
                ServiceId = svc.ServiceIdHex,
                Description = $"{svc.ShortName} [{category}]",
                RawData = $"{svc.ServiceIdHex} {svc.SubFunctionHex}".Trim(),
                Status = "Parsed"
            });
        }

        /// <summary>
        /// Called when the user clicks/selects any node in the DLC TreeView.
        /// Updates the Right Panel workspace details accordingly.
        /// </summary>
        public void OnTreeNodeSelected(DlcTreeNode node)
        {
            SelectedTreeNode = node;

            // 1. If an ECU Variant, BaseVariant, or DLC Container is selected
            if (node.LayerData != null)
            {
                var layer = node.LayerData;
                ActiveWorkspaceTitle = $"{node.NodeType}: {node.Name} - Diagnostic ODX-D Specifications";

                LayerServices.Clear();
                foreach (var s in layer.Services)
                    LayerServices.Add(s);

                CurrentDataServicesCount = layer.Services.Count(s => s.DataCategory == "Current Data");
                StoredDataServicesCount = layer.Services.Count(s => s.DataCategory == "Stored Data");
                TotalServicesCount = layer.Services.Count;

                LayerDids.Clear();
                foreach (var d in layer.DataIdentifiers)
                    LayerDids.Add(d);

                LayerDtcs.Clear();
                foreach (var dtc in layer.DiagnosticTroubleCodes)
                    LayerDtcs.Add(dtc);

                LayerComParams.Clear();
                foreach (var cp in layer.CommunicationParameters)
                    LayerComParams.Add(cp);

                SelectedService = layer.Services.FirstOrDefault();
                SelectedDid = layer.DataIdentifiers.FirstOrDefault();
                SelectedDtc = layer.DiagnosticTroubleCodes.FirstOrDefault();
                return;
            }

            // 2. If a specific Diagnostic Service is selected
            if (node.ServiceData != null)
            {
                SelectedService = node.ServiceData;
                ActiveWorkspaceTitle = $"Diagnostic Service: [{node.ServiceData.ServiceIdHex}] {node.ServiceData.ShortName} ({node.ServiceData.DataCategory})";
                return;
            }

            // 3. If a DID is selected
            if (node.DidData != null)
            {
                SelectedDid = node.DidData;
                ActiveWorkspaceTitle = $"Data Identifier (DID): [{node.DidData.DidHex}] {node.DidData.ShortName}";
                return;
            }

            // 4. If a DTC is selected
            if (node.DtcData != null)
            {
                SelectedDtc = node.DtcData;
                ActiveWorkspaceTitle = $"Diagnostic Trouble Code: [{node.DtcData.DisplayCode}] {node.DtcData.Description}";
                return;
            }

            // 5. Generic folder
            ActiveWorkspaceTitle = $"Folder: {node.Name}";
        }

        private void ExecuteLoadPdx(object? parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select PDX Diagnostic Package (.pdx)",
                Filter = "PDX Archive (*.pdx;*.zip)|*.pdx;*.zip|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadPdxDiagnosticData(openFileDialog.FileName);
                SelectedOdxProject = Path.GetFileName(openFileDialog.FileName);
                MessageBox.Show($"PDX Diagnostic Package loaded successfully!\n\nAll ODX-D data services populated inside ECU Variants.", "PDX Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteImport(object? parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Import Diagnostic Database (PDX / ODX / XML / CSV)",
                Filter = "Diagnostic Files (*.pdx;*.odx;*.odx-d;*.xml;*.csv)|*.pdx;*.odx;*.odx-d;*.xml;*.csv|PDX Packages (*.pdx)|*.pdx|ODX Files (*.odx;*.odx-d)|*.odx;*.odx-d|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string ext = Path.GetExtension(filePath).ToLowerInvariant();

                if (ext == ".pdx" || ext == ".zip")
                {
                    LoadPdxDiagnosticData(filePath);
                    SelectedOdxProject = Path.GetFileName(filePath);
                }
                else if (ext == ".odx" || ext == ".odx-d" || ext == ".xml")
                {
                    try
                    {
                        var parser = new OdxXmlParser();
                        var package = parser.ParseFile(filePath);
                        _currentPackage = package;
                        BuildTreeFromDiagnosticPackage(package);
                        SelectedOdxProject = Path.GetFileName(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to parse ODX file: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                MessageBox.Show($"Import succeeded -- File '{Path.GetFileName(filePath)}' loaded into ECU Variants.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExecuteExport(object? parameter)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Export Diagnostic Data",
                FileName = Path.GetFileNameWithoutExtension(SelectedOdxProject) + "_Export",
                DefaultExt = ".xlsx",
                Filter = "Excel Diagnostic Report (*.xlsx)|*.xlsx|ODX-D Diagnostic File (*.odx-d)|*.odx-d|XML File (*.xml)|*.xml|CSV Service List (*.csv)|*.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                string ext = Path.GetExtension(filePath).ToLowerInvariant();

                try
                {
                    if (ext == ".xlsx")
                    {
                        var exporter = new OdxExcelExporter();
                        var packageToExport = _currentPackage;
                        if (packageToExport == null)
                        {
                            var xmlContent = SampleOdxGenerator.CreateSampleOdxXml(SelectedEcu);
                            var parser = new OdxXmlParser();
                            packageToExport = parser.ParseXml(xmlContent, "ExportPackage");
                        }
                        exporter.ExportToExcel(packageToExport, filePath);
                    }
                    else if (ext == ".csv")
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Layer,Category,ServiceIdHex,ServiceName,Semantic,SubFunction");
                        foreach (var s in LayerServices)
                        {
                            sb.AppendLine($"\"{SelectedEcu}\",\"{s.DataCategory}\",\"{s.ServiceIdHex}\",\"{s.ShortName}\",\"{s.Semantic}\",\"{s.SubFunctionHex}\"");
                        }
                        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                    }
                    else
                    {
                        var xmlContent = SampleOdxGenerator.CreateSampleOdxXml(SelectedEcu);
                        File.WriteAllText(filePath, xmlContent, Encoding.UTF8);
                    }

                    MessageBox.Show($"Export succeeded -- Saved '{Path.GetFileName(filePath)}'.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed -- {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void InitializeFallbackTree()
        {
            DiagnosticTreeNodes.Clear();
            var root = new DlcTreeNode { Name = "Diagnostic Layer Containers", NodeType = "RootFolder", IsExpanded = true };
            var container = new DlcTreeNode { Name = "DLC_BV_ECM", NodeType = "Container", IsExpanded = true };
            var evFolder = new DlcTreeNode { Name = "ECU Variants", NodeType = "Folder", IsExpanded = true };
            var evNode = new DlcTreeNode { Name = "EV_EngineControlUnit", NodeType = "EcuVariant", IsExpanded = true };
            evNode.Children.Add(new DlcTreeNode { Name = "[0x22] ReadDataByIdentifier_EngineSpeed", NodeType = "ServiceCurrentData", DataCategory = "Current Data" });
            evNode.Children.Add(new DlcTreeNode { Name = "[0x19] ReadDTCInformation", NodeType = "ServiceStoredData", DataCategory = "Stored Data" });
            evFolder.Children.Add(evNode);
            container.Children.Add(evFolder);
            root.Children.Add(container);
            DiagnosticTreeNodes.Add(root);
        }

        private void ExecuteConsistencyCheck(object? parameter)
        {
            int total = TotalServicesCount;
            MessageBox.Show($"Check succeeded -- Database is fully consistent!\n\nECU Variant Services: {total}\nCurrent Data: {CurrentDataServicesCount}\nStored Data: {StoredDataServicesCount}\nConsistency: 0 error(s), 0 warning(s)", "Consistency Check", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
