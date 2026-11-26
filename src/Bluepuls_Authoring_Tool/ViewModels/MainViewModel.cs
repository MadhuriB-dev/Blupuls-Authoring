using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Bluepuls_Authoring_Tool.Models;
using Bluepuls_Authoring_Tool.Utilities;
using OdxDiagnosticParser.Models;
using OdxDiagnosticParser.Parsers;
using OdxDiagnosticParser.Exporters;

namespace Bluepuls_Authoring_Tool.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly Dictionary<string, DiagnosticPackage> _loadedPackages = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DlcTreeNode> _ecuNodeLookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<DiagnosticLayer, DlcTreeNode> _layerNodeLookup = new();
        private readonly List<string> _tempWorkspaces = new();

        private string _selectedOdxProject = string.Empty;
        private string _selectedEcu = string.Empty;
        private DiagnosticPackage? _currentPackage;
        private DiagnosticLayer? _activeLayer;
        private DlcTreeNode? _selectedTreeNode;
        private DlcTreeNode? _lastSelectedTreeNode;
        private DlcTreeNode? _pendingProgrammaticTarget;
        private bool _programmaticSelectHandledByUi;

        private bool _isBusy;
        private string _busyMessage = string.Empty;
        private DispatcherTimer? _serviceSearchTimer;
        private DispatcherTimer? _didSearchTimer;
        private DispatcherTimer? _dopSearchTimer;

        private DiagnosticService? _selectedService;
        private DiagnosticParameter? _selectedRequestParam;
        private DiagnosticParameter? _selectedResponseParam;
        private DiagnosticDataIdentifier? _selectedDid;
        private DataObjectProperty? _selectedDop;
        private DiagnosticTroubleCode? _selectedDtc;
        private CommunicationParameter? _selectedComParam;
        private IsoConnectionRow? _selectedIsoConnection;
        private VariantPatternRow? _selectedVariantPattern;

        private string _activeWorkspaceTitle = "No PDX / ODX Package Loaded";
        private string _activeWorkspaceSubtitle = "Click 'New Project', 'Load PDX', or 'Import ODX' to author or edit ISO 22901 containers.";
        private string _statusMessage = "Ready | Workspace empty. Create a New Project or Load PDX / Import ODX.";
        private int _currentDataServicesCount;
        private int _storedDataServicesCount;
        private int _totalServicesCount;
        private int _inheritedServicesCount;
        private int _selectedTabIndex;

        // Filter state
        private string _serviceCategoryFilter = "All";
        private string _serviceScopeFilter = "All (Effective ISO)";
        private string _serviceSearchText = string.Empty;
        private string _didSearchText = string.Empty;
        private string _dopSearchText = string.Empty;
        private bool _isUpdatingSelectionFromTree;

        // DID Editor properties
        private string _editDidHex = string.Empty;
        private string _editDidName = string.Empty;
        private string _editDidLongName = string.Empty;
        private string _editDidCategory = "Current Data";
        private string _editDidAccess = "Read";
        private DataObjectProperty? _editDidSelectedDop;
        private string _editDidDataType = "A_UINT32";
        private int _editDidByteLength = 2;
        private string _editDidUnit = string.Empty;
        private double _editDidScalingFactor = 1.0;
        private double _editDidOffset;
        private string _editDidDescription = string.Empty;
        private bool _autoSyncDidServices = true;

        // DOP Editor properties
        private string _editDopId = string.Empty;
        private string _editDopShortName = string.Empty;
        private string _editDopCategory = "LINEAR";
        private string _editDopBaseDataType = "A_UINT32";
        private int _editDopBitLength = 16;
        private string _editDopPhysicalUnit = string.Empty;
        private double _editDopFactor = 1.0;
        private double _editDopOffset;

        // Service & NRC Editor
        private string _editServiceNrcsText = string.Empty;
        private DataObjectProperty? _selectedParamAssignDop;

        // DTC Editor
        private string _editDtcDisplayCode = string.Empty;
        private string _editDtcHex = string.Empty;
        private string _editDtcSeverity = "Warning";
        private string _editDtcDescription = string.Empty;

        // ComParam Editor
        private string _editComParamName = string.Empty;
        private string _editComParamValue = string.Empty;
        private string _editComParamUnit = string.Empty;
        private string _editComParamDesc = string.Empty;

        // ISO Layer & ParentRef Authoring
        private string _newEcuLayerName = string.Empty;
        private DiagnosticLayer? _selectedParentTargetLayer;
        private string _selectedParentRefType = "BASE-VARIANT-REF";
        private string _newPatternName = "Pat_Custom";
        private string _newPatternService = "ReadDataByIdentifier_SoftwareVersion";
        private string _newPatternParam = "SW_Version";
        private string _newPatternExpectedValue = "V1.0";

        private readonly List<DiagnosticService> _allLayerServices = new();
        private readonly List<DiagnosticDataIdentifier> _allLayerDids = new();
        private readonly List<DataObjectProperty> _allLayerDops = new();

        // TreeView root collection
        public RangeObservableCollection<DlcTreeNode> DiagnosticTreeNodes { get; } = new();

        // Dropdown collections
        public RangeObservableCollection<string> OdxProjects { get; } = new();
        public RangeObservableCollection<string> EcuList { get; } = new();

        // Static choice lists for ComboBoxes
        public ObservableCollection<string> ServiceCategoryFilters { get; } = new() { "All", "Current Data", "Stored Data", "System Control" };
        public ObservableCollection<string> ServiceScopeFilters { get; } = new() { "All (Effective ISO)", "Own Layer Only", "Inherited From Parents" };
        public ObservableCollection<string> DataCategoryOptions { get; } = new() { "Current Data", "Stored Data" };
        public ObservableCollection<string> ServiceCategoryOptions { get; } = new() { "Current Data", "Stored Data", "System Control" };
        public ObservableCollection<string> DidAccessOptions { get; } = new() { "Read", "Write", "Read/Write" };
        public ObservableCollection<string> AddressingOptions { get; } = new() { "Physical", "Functional", "Physical/Functional" };
        public ObservableCollection<string> DopCategoryOptions { get; } = new() { "IDENTICAL", "LINEAR", "TEXTTABLE" };
        public ObservableCollection<string> BaseDataTypeOptions { get; } = new() { "A_UINT32", "A_INT32", "A_FLOAT32", "A_ASCIISTRING", "A_BYTEFIELD" };
        public ObservableCollection<string> ParamTypeOptions { get; } = new() { "VALUE", "CODED-CONST", "MATCHING-REQUEST-PARAM", "RESERVED" };
        public ObservableCollection<string> DtcSeverityOptions { get; } = new() { "Information", "Warning", "Fatal" };
        public ObservableCollection<string> ParentRefTypeOptions { get; } = new() { "BASE-VARIANT-REF", "PROTOCOL-REF", "FUNCTIONAL-GROUP-REF", "ECU-SHARED-DATA-REF" };

        // Workspace Collections (RangeObservableCollection for single-notification batch refreshes)
        public RangeObservableCollection<DiagnosticService> LayerServices { get; } = new();
        public RangeObservableCollection<DiagnosticParameter> ServiceRequestParams { get; } = new();
        public RangeObservableCollection<DiagnosticParameter> ServiceResponseParams { get; } = new();
        public RangeObservableCollection<DiagnosticDataIdentifier> LayerDids { get; } = new();
        public RangeObservableCollection<DataObjectProperty> LayerDops { get; } = new();
        public RangeObservableCollection<DataObjectProperty> AvailableDops { get; } = new();
        public RangeObservableCollection<DiagnosticTroubleCode> LayerDtcs { get; } = new();
        public RangeObservableCollection<CommunicationParameter> LayerComParams { get; } = new();
        public RangeObservableCollection<DiagnosticLogItem> CommunicationLogs { get; } = new();
        public RangeObservableCollection<PackageMetadataItem> PackageMetadata { get; } = new();

        // ISO Collections
        public RangeObservableCollection<IsoConnectionRow> IsoConnections { get; } = new();
        public RangeObservableCollection<LogicalLinkRow> LogicalLinksView { get; } = new();
        public RangeObservableCollection<VariantPatternRow> VariantPatternsView { get; } = new();
        public RangeObservableCollection<AblockManifestRow> AblockManifestRows { get; } = new();
        public RangeObservableCollection<OdxConsistencyIssue> ConsistencyIssuesView { get; } = new();
        public RangeObservableCollection<DiagnosticLayer> AvailableTargetLayers { get; } = new();

        public bool IsPackageLoaded => _currentPackage != null;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }

        public bool IsNotBusy => !_isBusy;

        public string BusyMessage
        {
            get => _busyMessage;
            private set { _busyMessage = value; OnPropertyChanged(); }
        }

        public string SelectedOdxProject
        {
            get => _selectedOdxProject;
            set
            {
                if (_selectedOdxProject == value) return;
                _selectedOdxProject = value;
                OnPropertyChanged();

                if (!string.IsNullOrWhiteSpace(value) && _loadedPackages.TryGetValue(value, out var pkg))
                {
                    _currentPackage = pkg;
                    OnPropertyChanged(nameof(IsPackageLoaded));
                    BuildTreeFromDiagnosticPackage(pkg);
                }
            }
        }

        public string SelectedEcu
        {
            get => _selectedEcu;
            set
            {
                if (_selectedEcu == value) return;
                _selectedEcu = value;
                OnPropertyChanged();

                if (!_isUpdatingSelectionFromTree && !string.IsNullOrWhiteSpace(value) &&
                    _ecuNodeLookup.TryGetValue(value, out var targetNode))
                {
                    SelectNodeProgrammatically(targetNode);
                }
            }
        }

        public DlcTreeNode? SelectedTreeNode
        {
            get => _selectedTreeNode;
            set { _selectedTreeNode = value; OnPropertyChanged(); }
        }

        public DiagnosticService? SelectedService
        {
            get => _selectedService;
            set
            {
                _selectedService = value;
                OnPropertyChanged();
                SyncServiceParamsFromSelection();
                EditServiceNrcsText = value != null ? string.Join(", ", value.NegativeResponses) : string.Empty;
                OnPropertyChanged(nameof(SelectedServiceRequestPreview));
                OnPropertyChanged(nameof(SelectedServiceResponsePreview));
                OnPropertyChanged(nameof(SelectedServiceNegativeResponsesText));
            }
        }

        public DiagnosticParameter? SelectedRequestParam
        {
            get => _selectedRequestParam;
            set { _selectedRequestParam = value; OnPropertyChanged(); }
        }

        public DiagnosticParameter? SelectedResponseParam
        {
            get => _selectedResponseParam;
            set { _selectedResponseParam = value; OnPropertyChanged(); }
        }

        public DiagnosticDataIdentifier? SelectedDid
        {
            get => _selectedDid;
            set
            {
                _selectedDid = value;
                OnPropertyChanged();
                LoadDidIntoEditor(value);
            }
        }

        public DataObjectProperty? SelectedDop
        {
            get => _selectedDop;
            set
            {
                _selectedDop = value;
                OnPropertyChanged();
                LoadDopIntoEditor(value);
            }
        }

        public DiagnosticTroubleCode? SelectedDtc
        {
            get => _selectedDtc;
            set
            {
                _selectedDtc = value;
                OnPropertyChanged();
                if (value != null)
                {
                    EditDtcDisplayCode = value.DisplayCode;
                    EditDtcHex = value.TroubleCodeHex;
                    EditDtcSeverity = string.IsNullOrWhiteSpace(value.Severity) ? "Warning" : value.Severity;
                    EditDtcDescription = value.Description;
                }
            }
        }

        public CommunicationParameter? SelectedComParam
        {
            get => _selectedComParam;
            set
            {
                _selectedComParam = value;
                OnPropertyChanged();
                if (value != null)
                {
                    EditComParamName = value.Name;
                    EditComParamValue = value.Value;
                    EditComParamUnit = value.Unit;
                    EditComParamDesc = value.Description;
                }
            }
        }

        public IsoConnectionRow? SelectedIsoConnection
        {
            get => _selectedIsoConnection;
            set { _selectedIsoConnection = value; OnPropertyChanged(); }
        }

        public VariantPatternRow? SelectedVariantPattern
        {
            get => _selectedVariantPattern;
            set { _selectedVariantPattern = value; OnPropertyChanged(); }
        }

        // Editor Bindable Properties - DID
        public string EditDidHex { get => _editDidHex; set { _editDidHex = value; OnPropertyChanged(); } }
        public string EditDidName { get => _editDidName; set { _editDidName = value; OnPropertyChanged(); } }
        public string EditDidLongName { get => _editDidLongName; set { _editDidLongName = value; OnPropertyChanged(); } }
        public string EditDidCategory { get => _editDidCategory; set { _editDidCategory = value; OnPropertyChanged(); } }
        public string EditDidAccess { get => _editDidAccess; set { _editDidAccess = value; OnPropertyChanged(); } }

        public DataObjectProperty? EditDidSelectedDop
        {
            get => _editDidSelectedDop;
            set
            {
                _editDidSelectedDop = value;
                OnPropertyChanged();
                if (value != null)
                {
                    EditDidDataType = value.BaseDataType;
                    EditDidByteLength = value.BitLength > 0 ? (value.BitLength + 7) / 8 : 1;
                    EditDidUnit = value.PhysicalUnit;
                    EditDidScalingFactor = value.Factor;
                    EditDidOffset = value.Offset;
                }
            }
        }

        public string EditDidDataType { get => _editDidDataType; set { _editDidDataType = value; OnPropertyChanged(); } }
        public int EditDidByteLength { get => _editDidByteLength; set { _editDidByteLength = value; OnPropertyChanged(); } }
        public string EditDidUnit { get => _editDidUnit; set { _editDidUnit = value; OnPropertyChanged(); } }
        public double EditDidScalingFactor { get => _editDidScalingFactor; set { _editDidScalingFactor = value; OnPropertyChanged(); } }
        public double EditDidOffset { get => _editDidOffset; set { _editDidOffset = value; OnPropertyChanged(); } }
        public string EditDidDescription { get => _editDidDescription; set { _editDidDescription = value; OnPropertyChanged(); } }
        public bool AutoSyncDidServices { get => _autoSyncDidServices; set { _autoSyncDidServices = value; OnPropertyChanged(); } }

        // Editor Bindable Properties - DOP
        public string EditDopId { get => _editDopId; set { _editDopId = value; OnPropertyChanged(); } }
        public string EditDopShortName { get => _editDopShortName; set { _editDopShortName = value; OnPropertyChanged(); } }
        public string EditDopCategory { get => _editDopCategory; set { _editDopCategory = value; OnPropertyChanged(); } }
        public string EditDopBaseDataType { get => _editDopBaseDataType; set { _editDopBaseDataType = value; OnPropertyChanged(); } }
        public int EditDopBitLength { get => _editDopBitLength; set { _editDopBitLength = value; OnPropertyChanged(); } }
        public string EditDopPhysicalUnit { get => _editDopPhysicalUnit; set { _editDopPhysicalUnit = value; OnPropertyChanged(); } }
        public double EditDopFactor { get => _editDopFactor; set { _editDopFactor = value; OnPropertyChanged(); } }
        public double EditDopOffset { get => _editDopOffset; set { _editDopOffset = value; OnPropertyChanged(); } }

        // Service & Parameter Editor
        public string EditServiceNrcsText { get => _editServiceNrcsText; set { _editServiceNrcsText = value; OnPropertyChanged(); } }
        public DataObjectProperty? SelectedParamAssignDop { get => _selectedParamAssignDop; set { _selectedParamAssignDop = value; OnPropertyChanged(); } }

        // DTC Editor
        public string EditDtcDisplayCode { get => _editDtcDisplayCode; set { _editDtcDisplayCode = value; OnPropertyChanged(); } }
        public string EditDtcHex { get => _editDtcHex; set { _editDtcHex = value; OnPropertyChanged(); } }
        public string EditDtcSeverity { get => _editDtcSeverity; set { _editDtcSeverity = value; OnPropertyChanged(); } }
        public string EditDtcDescription { get => _editDtcDescription; set { _editDtcDescription = value; OnPropertyChanged(); } }

        // ComParam Editor
        public string EditComParamName { get => _editComParamName; set { _editComParamName = value; OnPropertyChanged(); } }
        public string EditComParamValue { get => _editComParamValue; set { _editComParamValue = value; OnPropertyChanged(); } }
        public string EditComParamUnit { get => _editComParamUnit; set { _editComParamUnit = value; OnPropertyChanged(); } }
        public string EditComParamDesc { get => _editComParamDesc; set { _editComParamDesc = value; OnPropertyChanged(); } }

        // ISO Topology & Variant Editor
        public string NewEcuLayerName { get => _newEcuLayerName; set { _newEcuLayerName = value; OnPropertyChanged(); } }
        public DiagnosticLayer? SelectedParentTargetLayer { get => _selectedParentTargetLayer; set { _selectedParentTargetLayer = value; OnPropertyChanged(); } }
        public string SelectedParentRefType { get => _selectedParentRefType; set { _selectedParentRefType = value; OnPropertyChanged(); } }
        public string NewPatternName { get => _newPatternName; set { _newPatternName = value; OnPropertyChanged(); } }
        public string NewPatternService { get => _newPatternService; set { _newPatternService = value; OnPropertyChanged(); } }
        public string NewPatternParam { get => _newPatternParam; set { _newPatternParam = value; OnPropertyChanged(); } }
        public string NewPatternExpectedValue { get => _newPatternExpectedValue; set { _newPatternExpectedValue = value; OnPropertyChanged(); } }

        public string ActiveWorkspaceTitle { get => _activeWorkspaceTitle; set { _activeWorkspaceTitle = value; OnPropertyChanged(); } }
        public string ActiveWorkspaceSubtitle { get => _activeWorkspaceSubtitle; set { _activeWorkspaceSubtitle = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public int CurrentDataServicesCount { get => _currentDataServicesCount; set { _currentDataServicesCount = value; OnPropertyChanged(); } }
        public int StoredDataServicesCount { get => _storedDataServicesCount; set { _storedDataServicesCount = value; OnPropertyChanged(); } }
        public int TotalServicesCount { get => _totalServicesCount; set { _totalServicesCount = value; OnPropertyChanged(); } }
        public int InheritedServicesCount { get => _inheritedServicesCount; set { _inheritedServicesCount = value; OnPropertyChanged(); } }
        public int SelectedTabIndex { get => _selectedTabIndex; set { _selectedTabIndex = value; OnPropertyChanged(); } }

        public string ServiceCategoryFilter { get => _serviceCategoryFilter; set { _serviceCategoryFilter = value; OnPropertyChanged(); ApplyServiceFilters(); } }
        public string ServiceScopeFilter { get => _serviceScopeFilter; set { _serviceScopeFilter = value; OnPropertyChanged(); ApplyServiceFilters(); } }
        public string ServiceSearchText { get => _serviceSearchText; set { if (_serviceSearchText == value) return; _serviceSearchText = value; OnPropertyChanged(); ScheduleFilterDebounce(ref _serviceSearchTimer, _allLayerServices.Count, ApplyServiceFilters); } }
        public string DidSearchText { get => _didSearchText; set { if (_didSearchText == value) return; _didSearchText = value; OnPropertyChanged(); ScheduleFilterDebounce(ref _didSearchTimer, _allLayerDids.Count, ApplyDidFilters); } }
        public string DopSearchText { get => _dopSearchText; set { if (_dopSearchText == value) return; _dopSearchText = value; OnPropertyChanged(); ScheduleFilterDebounce(ref _dopSearchTimer, _allLayerDops.Count, ApplyDopFilters); } }

        private static void ScheduleFilterDebounce(ref DispatcherTimer? timer, int sourceCount, Action applyAction)
        {
            // Apply immediately for small datasets or headless callers without an active WPF Application dispatcher
            if (sourceCount <= 120 || Application.Current == null || SynchronizationContext.Current == null)
            {
                applyAction();
                return;
            }

            if (timer == null)
            {
                var createdTimer = new DispatcherTimer(DispatcherPriority.Background)
                {
                    Interval = TimeSpan.FromMilliseconds(90)
                };
                createdTimer.Tick += (_, _) =>
                {
                    createdTimer.Stop();
                    applyAction();
                };
                timer = createdTimer;
            }

            timer.Stop();
            timer.Start();
        }

        public string SelectedServiceRequestPreview
        {
            get
            {
                if (SelectedService == null) return string.Empty;
                var bytes = ServiceRequestParams
                    .OrderBy(p => p.BytePosition)
                    .Select(p => !string.IsNullOrWhiteSpace(p.DefaultValueHex) ? p.DefaultValueHex : $"<{p.Name}>");
                return string.Join(" ", bytes);
            }
        }

        public string SelectedServiceResponsePreview
        {
            get
            {
                if (SelectedService == null) return string.Empty;
                var bytes = ServiceResponseParams
                    .OrderBy(p => p.BytePosition)
                    .Select(p => !string.IsNullOrWhiteSpace(p.DefaultValueHex) ? p.DefaultValueHex : $"<{p.Name}>");
                var preview = string.Join(" ", bytes);
                return string.IsNullOrWhiteSpace(preview) ? SelectedService.PositiveResponseSidHex : preview;
            }
        }

        public string SelectedServiceNegativeResponsesText
        {
            get
            {
                if (SelectedService == null) return string.Empty;
                return SelectedService.NegativeResponses.Count > 0
                    ? string.Join(", ", SelectedService.NegativeResponses)
                    : "(No negative response codes defined in loaded PDX / ODX)";
            }
        }

        // File & Package Commands
        public ICommand NewProjectCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand LoadPdxCommand { get; }
        public ICommand ClearWorkspaceCommand { get; }
        public ICommand ConsistencyCheckCommand { get; }

        // DID Authoring Commands
        public ICommand CreateDidCommand { get; }
        public ICommand ApplyDidEditsCommand { get; }
        public ICommand DeleteDidCommand { get; }
        public ICommand AssignDopToDidCommand { get; }

        // DOP Authoring Commands
        public ICommand CreateDopCommand { get; }
        public ICommand ApplyDopEditsCommand { get; }
        public ICommand DeleteDopCommand { get; }
        public ICommand AssignSelectedDopRowToDidCommand { get; }

        // Service & Param Authoring Commands
        public ICommand CreateServiceCommand { get; }
        public ICommand ApplyServiceEditsCommand { get; }
        public ICommand DeleteServiceCommand { get; }
        public ICommand AddRequestParamCommand { get; }
        public ICommand DeleteRequestParamCommand { get; }
        public ICommand AssignDopToRequestParamCommand { get; }
        public ICommand AddResponseParamCommand { get; }
        public ICommand DeleteResponseParamCommand { get; }
        public ICommand AssignDopToResponseParamCommand { get; }

        // DTC & ComParam Authoring Commands
        public ICommand CreateDtcCommand { get; }
        public ICommand ApplyDtcEditsCommand { get; }
        public ICommand DeleteDtcCommand { get; }
        public ICommand CreateComParamCommand { get; }
        public ICommand ApplyComParamEditsCommand { get; }
        public ICommand DeleteComParamCommand { get; }

        // ISO Layer / Topology Authoring Commands
        public ICommand AddEcuVariantCommand { get; }
        public ICommand AddBaseVariantCommand { get; }
        public ICommand DeleteActiveLayerCommand { get; }
        public ICommand AddParentRefConnectionCommand { get; }
        public ICommand RemoveParentRefConnectionCommand { get; }
        public ICommand AddVariantPatternCommand { get; }
        public ICommand DeleteVariantPatternCommand { get; }

        public MainViewModel()
        {
            NewProjectCommand = new RelayCommand(ExecuteNewProject);
            ImportCommand = new RelayCommand(ExecuteImport);
            ExportCommand = new RelayCommand(ExecuteExport);
            LoadPdxCommand = new RelayCommand(ExecuteLoadPdx);
            ClearWorkspaceCommand = new RelayCommand(ExecuteClearWorkspace);
            ConsistencyCheckCommand = new RelayCommand(ExecuteConsistencyCheck);

            CreateDidCommand = new RelayCommand(ExecuteCreateDid);
            ApplyDidEditsCommand = new RelayCommand(ExecuteApplyDidEdits);
            DeleteDidCommand = new RelayCommand(ExecuteDeleteDid);
            AssignDopToDidCommand = new RelayCommand(ExecuteAssignDopToDid);

            CreateDopCommand = new RelayCommand(ExecuteCreateDop);
            ApplyDopEditsCommand = new RelayCommand(ExecuteApplyDopEdits);
            DeleteDopCommand = new RelayCommand(ExecuteDeleteDop);
            AssignSelectedDopRowToDidCommand = new RelayCommand(ExecuteAssignSelectedDopRowToDid);

            CreateServiceCommand = new RelayCommand(ExecuteCreateService);
            ApplyServiceEditsCommand = new RelayCommand(ExecuteApplyServiceEdits);
            DeleteServiceCommand = new RelayCommand(ExecuteDeleteService);
            AddRequestParamCommand = new RelayCommand(ExecuteAddRequestParam);
            DeleteRequestParamCommand = new RelayCommand(ExecuteDeleteRequestParam);
            AssignDopToRequestParamCommand = new RelayCommand(ExecuteAssignDopToRequestParam);
            AddResponseParamCommand = new RelayCommand(ExecuteAddResponseParam);
            DeleteResponseParamCommand = new RelayCommand(ExecuteDeleteResponseParam);
            AssignDopToResponseParamCommand = new RelayCommand(ExecuteAssignDopToResponseParam);

            CreateDtcCommand = new RelayCommand(ExecuteCreateDtc);
            ApplyDtcEditsCommand = new RelayCommand(ExecuteApplyDtcEdits);
            DeleteDtcCommand = new RelayCommand(ExecuteDeleteDtc);
            CreateComParamCommand = new RelayCommand(ExecuteCreateComParam);
            ApplyComParamEditsCommand = new RelayCommand(ExecuteApplyComParamEdits);
            DeleteComParamCommand = new RelayCommand(ExecuteDeleteComParam);

            AddEcuVariantCommand = new RelayCommand(ExecuteAddEcuVariant);
            AddBaseVariantCommand = new RelayCommand(ExecuteAddBaseVariant);
            DeleteActiveLayerCommand = new RelayCommand(ExecuteDeleteActiveLayer);
            AddParentRefConnectionCommand = new RelayCommand(ExecuteAddParentRefConnection);
            RemoveParentRefConnectionCommand = new RelayCommand(ExecuteRemoveParentRefConnection);
            AddVariantPatternCommand = new RelayCommand(ExecuteAddVariantPattern);
            DeleteVariantPatternCommand = new RelayCommand(ExecuteDeleteVariantPattern);
        }

        // =========================================================================
        // 1. PROJECT CREATION & PDX/ODX LOADING
        // =========================================================================

        public void CreateNewBlankProject(string ecuBaseName = "CustomECU")
        {
            var safeName = SanitizeIdentifier(string.IsNullOrWhiteSpace(ecuBaseName) ? "CustomECU" : ecuBaseName);
            var pkg = new DiagnosticPackage
            {
                Name = $"{safeName}_Authoring_Project",
                Version = "2.2.0",
                SourcePath = $"{safeName}.pdx",
                IsFromPdx = false,
                ExtractedAt = DateTime.UtcNow,
                Description = "User-authored ISO 22901-1 / ISO 22901-2 Diagnostic Workspace"
            };

            var protoLayer = new DiagnosticLayer
            {
                Id = "PR_UDSonCAN",
                ShortName = "PR_UDSonCAN",
                LongName = "UDS on CAN (ISO 14229 / ISO 15765-2)",
                LayerType = DiagnosticLayerType.Protocol,
                ContainerShortName = $"DLC_{safeName}",
                CommunicationParameters = new List<CommunicationParameter>
                {
                    new() { Name = "CP_Baudrate", Value = "500000", Unit = "bps", Protocol = "PR_UDSonCAN", DefinedInLayer = "PR_UDSonCAN" },
                    new() { Name = "CP_P2Max", Value = "50", Unit = "ms", Protocol = "PR_UDSonCAN", DefinedInLayer = "PR_UDSonCAN" }
                }
            };

            var bvLayer = new DiagnosticLayer
            {
                Id = $"BV_{safeName}",
                ShortName = $"BV_{safeName}",
                LongName = $"Base Variant - {safeName}",
                LayerType = DiagnosticLayerType.BaseVariant,
                ContainerShortName = $"DLC_{safeName}",
                ParentRefs = new List<LayerParentRef>
                {
                    new() { IdRef = "PR_UDSonCAN", DocRef = $"DLC_{safeName}", ParentRefType = "PROTOCOL-REF" }
                },
                DataObjectProperties = new List<DataObjectProperty>
                {
                    new() { Id = "DOP_Raw_UInt8", ShortName = "DOP_Raw_UInt8", Category = "IDENTICAL", BaseDataType = "A_UINT32", BitLength = 8, DefinedInLayer = $"BV_{safeName}" },
                    new() { Id = "DOP_Raw_UInt16", ShortName = "DOP_Raw_UInt16", Category = "IDENTICAL", BaseDataType = "A_UINT32", BitLength = 16, DefinedInLayer = $"BV_{safeName}" },
                    new() { Id = "DOP_Ascii_String", ShortName = "DOP_Ascii_String", Category = "IDENTICAL", BaseDataType = "A_ASCIISTRING", BitLength = 136, DefinedInLayer = $"BV_{safeName}" }
                }
            };

            var evLayer = new DiagnosticLayer
            {
                Id = $"EV_{safeName}_V1",
                ShortName = $"EV_{safeName}_V1",
                LongName = $"ECU Variant 1 - {safeName}",
                LayerType = DiagnosticLayerType.EcuVariant,
                ContainerShortName = $"DLC_{safeName}",
                ParentRefs = new List<LayerParentRef>
                {
                    new() { IdRef = $"BV_{safeName}", DocRef = $"DLC_{safeName}", ParentRefType = "BASE-VARIANT-REF" }
                }
            };

            pkg.Layers.Add(protoLayer);
            pkg.Layers.Add(bvLayer);
            pkg.Layers.Add(evLayer);
            OdxIsoLinker.LinkPackage(pkg);

            RegisterAndActivatePackage($"{safeName}.pdx", pkg);
            StatusMessage = $"Created editable project '{pkg.Name}'. Add DIDs, DOPs, Services, or DTCs and Export to .pdx / .odx-d.";
        }

        public void LoadPdxDiagnosticData(string pdxPath)
        {
            if (string.IsNullOrWhiteSpace(pdxPath) || !File.Exists(pdxPath))
            {
                MessageBox.Show("Please select an existing .pdx diagnostic package file.", "PDX File Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var parser = new OdxXmlParser();
                var extractor = new PdxPackageExtractor(parser);

                var tempExtractDir = Path.Combine(Path.GetTempPath(), "Bluepuls_PDX_" + Guid.NewGuid().ToString("N")[..8]);
                _tempWorkspaces.Add(tempExtractDir);

                var package = extractor.ExtractAndParse(pdxPath, tempExtractDir);
                var projectKey = Path.GetFileName(pdxPath);

                RegisterAndActivatePackage(projectKey, package);
                StatusMessage = $"Loaded PDX '{projectKey}' ({package.EcuVariants.Count()} ECU Variant(s)). All fields are editable.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse PDX diagnostic package: {ex.Message}", "PDX Parser Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public async Task LoadPdxDiagnosticDataAsync(string pdxPath)
        {
            if (IsBusy) return;
            if (string.IsNullOrWhiteSpace(pdxPath) || !File.Exists(pdxPath))
            {
                MessageBox.Show("Please select an existing .pdx diagnostic package file.", "PDX File Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var projectKey = Path.GetFileName(pdxPath);
            IsBusy = true;
            BusyMessage = $"Extracting & linking ISO 22901 PDX '{projectKey}'...";
            StatusMessage = BusyMessage;

            try
            {
                var tempExtractDir = Path.Combine(Path.GetTempPath(), "Bluepuls_PDX_" + Guid.NewGuid().ToString("N")[..8]);
                _tempWorkspaces.Add(tempExtractDir);

                var package = await Task.Run(() =>
                {
                    var parser = new OdxXmlParser();
                    var extractor = new PdxPackageExtractor(parser);
                    return extractor.ExtractAndParse(pdxPath, tempExtractDir);
                });

                BusyMessage = $"Populating workspace views for '{projectKey}'...";
                RegisterAndActivatePackage(projectKey, package);
                StatusMessage = $"Loaded PDX '{projectKey}' ({package.EcuVariants.Count()} ECU Variant(s)). All fields are editable.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"PDX load failed: {ex.Message}";
                MessageBox.Show($"Failed to parse PDX diagnostic package: {ex.Message}", "PDX Parser Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private void RegisterAndActivatePackage(string projectKey, DiagnosticPackage package)
        {
            _loadedPackages[projectKey] = package;
            if (!OdxProjects.Contains(projectKey))
                OdxProjects.Add(projectKey);

            _selectedOdxProject = projectKey;
            OnPropertyChanged(nameof(SelectedOdxProject));

            _currentPackage = package;
            OnPropertyChanged(nameof(IsPackageLoaded));
            BuildTreeFromDiagnosticPackage(package);
        }

        private void ExecuteNewProject(object? parameter)
        {
            if (IsBusy) return;
            var baseName = string.IsNullOrWhiteSpace(NewEcuLayerName) ? "CustomECU" : NewEcuLayerName.Trim();
            CreateNewBlankProject(baseName);
        }

        private async void ExecuteLoadPdx(object? parameter)
        {
            if (IsBusy) return;
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select ISO 22901-2 PDX Diagnostic Package (.pdx)",
                Filter = "PDX Archive (*.pdx;*.zip)|*.pdx;*.zip|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await LoadPdxDiagnosticDataAsync(openFileDialog.FileName);
            }
        }

        private async void ExecuteImport(object? parameter)
        {
            if (IsBusy) return;
            var openFileDialog = new OpenFileDialog
            {
                Title = "Import Diagnostic Database (PDX / ODX-D / ODX-C / ODX-V / XML)",
                Filter = "Diagnostic Files (*.pdx;*.odx;*.odx-d;*.odx-c;*.odx-v;*.odx-e;*.xml)|*.pdx;*.odx;*.odx-d;*.odx-c;*.odx-v;*.odx-e;*.xml|PDX Packages (*.pdx)|*.pdx|ODX Files (*.odx;*.odx-d;*.odx-c;*.odx-v)|*.odx;*.odx-d;*.odx-c;*.odx-v|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() != true) return;

            string filePath = openFileDialog.FileName;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            if (ext == ".pdx" || ext == ".zip")
            {
                await LoadPdxDiagnosticDataAsync(filePath);
                return;
            }

            var fileName = Path.GetFileName(filePath);
            IsBusy = true;
            BusyMessage = $"Parsing ODX file '{fileName}'...";
            StatusMessage = BusyMessage;

            try
            {
                var package = await Task.Run(() =>
                {
                    var parser = new OdxXmlParser();
                    return parser.ParseFile(filePath);
                });

                RegisterAndActivatePackage(fileName, package);
                StatusMessage = $"Imported ODX file '{fileName}'. Ready to edit.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Import failed: {ex.Message}";
                MessageBox.Show($"Failed to parse ODX file: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private async void ExecuteExport(object? parameter)
        {
            if (IsBusy) return;
            if (_currentPackage == null)
            {
                MessageBox.Show("No PDX or ODX diagnostic package is loaded.\nCreate a New Project or load a PDX / ODX file before exporting.", "Nothing to Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save / Export Diagnostic Database",
                FileName = Path.GetFileNameWithoutExtension(SelectedOdxProject),
                DefaultExt = ".pdx",
                Filter = "PDX Diagnostic Package (*.pdx)|*.pdx|ODX-D Diagnostic File (*.odx-d)|*.odx-d|Excel Diagnostic Report (*.xlsx)|*.xlsx|XML File (*.xml)|*.xml|CSV Service List (*.csv)|*.csv"
            };

            if (saveFileDialog.ShowDialog() != true) return;

            string filePath = saveFileDialog.FileName;
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            string fileName = Path.GetFileName(filePath);
            var pkgSnapshot = _currentPackage;
            var layerNameSnapshot = _activeLayer?.ShortName ?? SelectedEcu;
            var csvServicesSnapshot = ext == ".csv" ? LayerServices.ToList() : null;

            IsBusy = true;
            BusyMessage = $"Exporting '{fileName}'...";
            StatusMessage = BusyMessage;

            try
            {
                await Task.Run(() =>
                {
                    if (ext == ".pdx" || ext == ".zip")
                    {
                        OdxXmlSerializer.ExportToPdx(pkgSnapshot, filePath);
                    }
                    else if (ext == ".xlsx")
                    {
                        var exporter = new OdxExcelExporter();
                        exporter.ExportToExcel(pkgSnapshot, filePath);
                    }
                    else if (ext == ".csv" && csvServicesSnapshot != null)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Layer,OriginLayer,Scope,Category,ServiceIdHex,ServiceName,Semantic,SubFunction,Addressing");
                        foreach (var s in csvServicesSnapshot)
                        {
                            sb.AppendLine($"\"{layerNameSnapshot}\",\"{s.DefinedInLayer}\",\"{s.InheritanceScope}\",\"{s.DataCategory}\",\"{s.ServiceIdHex}\",\"{s.ShortName}\",\"{s.Semantic}\",\"{s.SubFunctionHex}\",\"{s.Addressing}\"");
                        }
                        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                    }
                    else
                    {
                        var xmlContent = OdxXmlSerializer.SerializePackageToOdxXml(pkgSnapshot);
                        File.WriteAllText(filePath, xmlContent, Encoding.UTF8);
                    }
                });

                StatusMessage = $"Saved/Exported '{fileName}' with all authored edits.";
                MessageBox.Show($"Saved '{fileName}' successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
                MessageBox.Show($"Export failed -- {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        // =========================================================================
        // 2. DID AUTHORING: CREATE DID, EDIT DID, DELETE DID, ASSIGN DOP
        // =========================================================================

        public DiagnosticDataIdentifier CreateDid(
            string didHexInput,
            string shortName,
            string dataCategory = "Current Data",
            string access = "Read",
            DataObjectProperty? assignedDop = null,
            string description = "")
        {
            var targetLayer = EnsureEditableLayer();
            var normalizedHex = NormalizeHex4(string.IsNullOrWhiteSpace(didHexInput) ? GenerateNextFreeDidHex(targetLayer) : didHexInput);
            var didDec = ParseHexInt(normalizedHex);

            var cleanName = string.IsNullOrWhiteSpace(shortName) ? $"DID_{normalizedHex.Replace("0x", "")}" : SanitizeIdentifier(shortName);

            var newDid = new DiagnosticDataIdentifier
            {
                DidHex = normalizedHex,
                DidDecimal = didDec,
                ShortName = cleanName,
                LongName = string.IsNullOrWhiteSpace(EditDidLongName) ? cleanName : EditDidLongName,
                DataCategory = string.IsNullOrWhiteSpace(dataCategory) ? "Current Data" : dataCategory,
                Access = string.IsNullOrWhiteSpace(access) ? "Read" : access,
                ByteLength = EditDidByteLength > 0 ? EditDidByteLength : 2,
                BitLength = (EditDidByteLength > 0 ? EditDidByteLength : 2) * 8,
                DataType = string.IsNullOrWhiteSpace(EditDidDataType) ? "A_UINT32" : EditDidDataType,
                Unit = EditDidUnit,
                ScalingFactor = EditDidScalingFactor,
                Offset = EditDidOffset,
                Description = string.IsNullOrWhiteSpace(description) ? EditDidDescription : description,
                AssociatedService = $"ReadDataByIdentifier_{cleanName}",
                DefinedInLayer = targetLayer.ShortName,
                IsInherited = false
            };

            if (assignedDop != null)
                newDid.ApplyDop(assignedDop);

            targetLayer.DataIdentifiers.Add(newDid);
            if (AutoSyncDidServices)
                SyncDidServicesOnLayer(targetLayer, newDid, previousAssociatedService: null);

            RefreshAfterLayerMutation(targetLayer);
            SelectedDid = newDid;
            StatusMessage = $"Created DID {newDid.DidHex} ({newDid.ShortName}){(assignedDop != null ? $" with DOP '{assignedDop.ShortName}'" : "")} on layer '{targetLayer.ShortName}'.";
            return newDid;
        }

        private void ExecuteCreateDid(object? parameter)
        {
            if (!EnsurePackageActive()) return;
            var targetLayer = EnsureEditableLayer();

            // Pick requested Hex only if it does not collide with an existing DID on the layer; otherwise generate next free hex
            var desiredHex = NormalizeHex4(EditDidHex);
            bool collides = string.IsNullOrEmpty(desiredHex) ||
                            targetLayer.DataIdentifiers.Any(d => d.DidHex.Equals(desiredHex, StringComparison.OrdinalIgnoreCase));
            var hexToUse = collides ? GenerateNextFreeDidHex(targetLayer) : desiredHex;
            var nameToUse = collides || string.IsNullOrWhiteSpace(EditDidName) ||
                            targetLayer.DataIdentifiers.Any(d => d.ShortName.Equals(EditDidName, StringComparison.OrdinalIgnoreCase))
                ? $"DID_{hexToUse.Replace("0x", "")}"
                : EditDidName;

            var dop = EditDidSelectedDop ?? AvailableDops.FirstOrDefault();
            CreateDid(hexToUse, nameToUse, EditDidCategory, EditDidAccess, dop, EditDidDescription);
        }

        public bool EditDid(DiagnosticDataIdentifier did)
        {
            if (did == null || _activeLayer == null) return false;
            var oldServiceName = did.AssociatedService;

            // If inherited from BaseVariant, promote to an Own override on active layer per ISO 22901-1
            if (did.IsInherited)
            {
                did.IsInherited = false;
                did.DefinedInLayer = _activeLayer.ShortName;
            }

            did.DidHex = NormalizeHex4(string.IsNullOrWhiteSpace(EditDidHex) ? did.DidHex : EditDidHex);
            did.DidDecimal = ParseHexInt(did.DidHex);
            did.ShortName = string.IsNullOrWhiteSpace(EditDidName) ? did.ShortName : EditDidName.Trim();
            did.LongName = string.IsNullOrWhiteSpace(EditDidLongName) ? did.ShortName : EditDidLongName.Trim();
            did.DataCategory = string.IsNullOrWhiteSpace(EditDidCategory) ? did.DataCategory : EditDidCategory;
            did.Access = string.IsNullOrWhiteSpace(EditDidAccess) ? did.Access : EditDidAccess;
            did.Description = EditDidDescription;

            if (EditDidSelectedDop != null)
            {
                did.ApplyDop(EditDidSelectedDop);
            }
            else
            {
                did.DataType = EditDidDataType;
                did.ByteLength = EditDidByteLength > 0 ? EditDidByteLength : 1;
                did.BitLength = did.ByteLength * 8;
                did.Unit = EditDidUnit;
                did.ScalingFactor = EditDidScalingFactor;
                did.Offset = EditDidOffset;
            }

            if (AutoSyncDidServices)
                SyncDidServicesOnLayer(_activeLayer, did, oldServiceName);

            RefreshAfterLayerMutation(_activeLayer);
            SelectedDid = did;
            StatusMessage = $"Updated DID {did.DidHex} ({did.ShortName}).";
            return true;
        }

        private void ExecuteApplyDidEdits(object? parameter)
        {
            if (SelectedDid == null)
            {
                MessageBox.Show("Select a DID to edit, or click '+ Create DID' to add a new one.", "No DID Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            EditDid(SelectedDid);
        }

        public bool AssignDopToDid(DiagnosticDataIdentifier did, DataObjectProperty dop)
        {
            if (did == null || dop == null || _activeLayer == null) return false;

            if (did.IsInherited)
            {
                did.IsInherited = false;
                did.DefinedInLayer = _activeLayer.ShortName;
            }

            // Ensure DOP exists or is registered on active layer if custom
            if (!_activeLayer.DataObjectProperties.Any(x => x.Id.Equals(dop.Id, StringComparison.OrdinalIgnoreCase)))
            {
                _activeLayer.DataObjectProperties.Add(dop);
            }

            did.ApplyDop(dop);
            LoadDidIntoEditor(did);

            if (AutoSyncDidServices)
                SyncDidServicesOnLayer(_activeLayer, did, did.AssociatedService);

            RefreshAfterLayerMutation(_activeLayer);
            SelectedDid = did;
            StatusMessage = $"Assigned DOP '{dop.ShortName}' ({dop.BaseDataType}, {dop.BitLength}b{(string.IsNullOrEmpty(dop.PhysicalUnit) ? "" : $", {dop.PhysicalUnit}")}) to DID {did.DidHex}.";
            return true;
        }

        private void ExecuteAssignDopToDid(object? parameter)
        {
            if (SelectedDid == null)
            {
                MessageBox.Show("Please select a DID row first.", "Select DID", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var dop = EditDidSelectedDop;
            if (dop == null)
            {
                MessageBox.Show("Please choose a DOP from the 'Assign DOP' dropdown (or create one in the DOPs tab).", "Select DOP", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            AssignDopToDid(SelectedDid, dop);
        }

        public bool DeleteDid(DiagnosticDataIdentifier did)
        {
            if (did == null || _activeLayer == null) return false;

            var deletedHex = did.DidHex;
            var assocName = did.AssociatedService;

            if (did.IsInherited)
            {
                // Add exclusion to matching PARENT-REF so relinking respects deletion per ISO 22901-1 NOT-INHERITED-DIAG-COMMS
                var pref = _activeLayer.ParentRefs.FirstOrDefault(p =>
                    p.IsResolved && p.ResolvedLayerShortName.Equals(did.DefinedInLayer, StringComparison.OrdinalIgnoreCase))
                    ?? _activeLayer.ParentRefs.FirstOrDefault(p => p.IsResolved);

                if (pref != null && !string.IsNullOrWhiteSpace(assocName) && !pref.NotInheritedDiagComms.Contains(assocName))
                    pref.NotInheritedDiagComms.Add(assocName);
            }

            _activeLayer.DataIdentifiers.Remove(did);

            // Remove associated 0x22 / 0x2E services for this DID
            _activeLayer.Services.RemoveAll(s =>
                (!string.IsNullOrWhiteSpace(assocName) && s.ShortName.Equals(assocName, StringComparison.OrdinalIgnoreCase)) ||
                ((s.ServiceIdHex == "0x22" || s.ServiceIdHex == "0x2E") &&
                 s.RequestParameters.Any(rp => rp.Semantic.Contains("DATA-ID", StringComparison.OrdinalIgnoreCase) &&
                                               NormalizeHex4(rp.DefaultValueHex).Equals(deletedHex, StringComparison.OrdinalIgnoreCase))));

            RefreshAfterLayerMutation(_activeLayer);
            SelectedDid = LayerDids.FirstOrDefault();
            StatusMessage = $"Deleted DID {deletedHex} from layer '{_activeLayer.ShortName}'.";
            return true;
        }

        private void ExecuteDeleteDid(object? parameter)
        {
            if (SelectedDid == null)
            {
                MessageBox.Show("Select a DID to delete.", "No DID Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            DeleteDid(SelectedDid);
        }

        private void SyncDidServicesOnLayer(DiagnosticLayer layer, DiagnosticDataIdentifier did, string? previousAssociatedService)
        {
            // Remove stale services linked to previous name/hex if access mode changed or renamed
            layer.Services.RemoveAll(s =>
                !s.IsInherited &&
                !string.IsNullOrWhiteSpace(previousAssociatedService) &&
                s.ShortName.Equals(previousAssociatedService, StringComparison.OrdinalIgnoreCase));

            var cleanName = SanitizeIdentifier(did.ShortName);
            bool needRead = did.Access.Contains("Read", StringComparison.OrdinalIgnoreCase);
            bool needWrite = did.Access.Contains("Write", StringComparison.OrdinalIgnoreCase);

            var payloadParam = new DiagnosticParameter
            {
                Name = $"{cleanName}_Data",
                Semantic = "DATA",
                ParameterType = "VALUE",
                BytePosition = 3,
                BitLength = did.BitLength > 0 ? did.BitLength : did.ByteLength * 8,
                DataType = did.DataType,
                DopRef = did.DopRef
            };

            if (needRead)
            {
                var readSvcName = $"ReadDataByIdentifier_{cleanName}";
                layer.Services.RemoveAll(s => !s.IsInherited && s.ShortName.Equals(readSvcName, StringComparison.OrdinalIgnoreCase));
                layer.Services.Add(new DiagnosticService
                {
                    Id = $"SVC_Read_{cleanName}",
                    ShortName = readSvcName,
                    LongName = $"Read {did.ShortName} (DID {did.DidHex})",
                    Description = did.Description,
                    Semantic = did.DataCategory == "Stored Data" ? "IDENTIFICATION" : "CURRENTDATA",
                    DataCategory = did.DataCategory,
                    ServiceIdHex = "0x22",
                    PositiveResponseSidHex = "0x62",
                    Addressing = "Physical",
                    DefinedInLayer = layer.ShortName,
                    DefinedInLayerType = layer.LayerType.ToString(),
                    IsInherited = false,
                    RequestParameters = new List<DiagnosticParameter>
                    {
                        new() { Name = "SID", Semantic = "SERVICE-ID", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = "0x22" },
                        new() { Name = "DID", Semantic = "DATA-ID", ParameterType = "CODED-CONST", BytePosition = 1, BitLength = 16, DefaultValueHex = did.DidHex }
                    },
                    ResponseParameters = new List<DiagnosticParameter>
                    {
                        new() { Name = "ResponseSID", Semantic = "SERVICE-ID", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = "0x62" },
                        new() { Name = "DID_Echo", Semantic = "DATA-ID", ParameterType = "CODED-CONST", BytePosition = 1, BitLength = 16, DefaultValueHex = did.DidHex },
                        payloadParam
                    }
                });
                did.AssociatedService = readSvcName;
            }

            if (needWrite)
            {
                var writeSvcName = $"WriteDataByIdentifier_{cleanName}";
                layer.Services.RemoveAll(s => !s.IsInherited && s.ShortName.Equals(writeSvcName, StringComparison.OrdinalIgnoreCase));
                layer.Services.Add(new DiagnosticService
                {
                    Id = $"SVC_Write_{cleanName}",
                    ShortName = writeSvcName,
                    LongName = $"Write {did.ShortName} (DID {did.DidHex})",
                    Description = did.Description,
                    Semantic = "CODING",
                    DataCategory = did.DataCategory,
                    ServiceIdHex = "0x2E",
                    PositiveResponseSidHex = "0x6E",
                    Addressing = "Physical",
                    DefinedInLayer = layer.ShortName,
                    DefinedInLayerType = layer.LayerType.ToString(),
                    IsInherited = false,
                    RequestParameters = new List<DiagnosticParameter>
                    {
                        new() { Name = "SID", Semantic = "SERVICE-ID", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = "0x2E" },
                        new() { Name = "DID", Semantic = "DATA-ID", ParameterType = "CODED-CONST", BytePosition = 1, BitLength = 16, DefaultValueHex = did.DidHex },
                        new()
                        {
                            Name = payloadParam.Name, Semantic = "DATA", ParameterType = "VALUE",
                            BytePosition = 3, BitLength = payloadParam.BitLength, DataType = payloadParam.DataType, DopRef = payloadParam.DopRef
                        }
                    },
                    ResponseParameters = new List<DiagnosticParameter>
                    {
                        new() { Name = "ResponseSID", Semantic = "SERVICE-ID", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = "0x6E" },
                        new() { Name = "DID_Echo", Semantic = "DATA-ID", ParameterType = "CODED-CONST", BytePosition = 1, BitLength = 16, DefaultValueHex = did.DidHex }
                    }
                });
                if (!needRead)
                    did.AssociatedService = writeSvcName;
            }
        }

        // =========================================================================
        // 3. DOP (DATA OBJECT PROPERTY) AUTHORING
        // =========================================================================

        public DataObjectProperty CreateDop(string shortName, string baseDataType = "A_UINT32", int bitLength = 16, string unit = "", double factor = 1.0, double offset = 0.0)
        {
            var layer = EnsureEditableLayer();
            var clean = SanitizeIdentifier(string.IsNullOrWhiteSpace(shortName) ? $"DOP_Custom_{layer.DataObjectProperties.Count + 1}" : shortName);
            var dop = new DataObjectProperty
            {
                Id = clean.StartsWith("DOP_", StringComparison.OrdinalIgnoreCase) ? clean : $"DOP_{clean}",
                ShortName = clean,
                LongName = clean,
                Category = (factor != 1.0 || offset != 0.0) ? "LINEAR" : (string.IsNullOrWhiteSpace(EditDopCategory) ? "IDENTICAL" : EditDopCategory),
                BaseDataType = string.IsNullOrWhiteSpace(baseDataType) ? "A_UINT32" : baseDataType,
                BitLength = bitLength > 0 ? bitLength : 16,
                PhysicalUnit = unit,
                Factor = factor,
                Offset = offset,
                DefinedInLayer = layer.ShortName,
                IsInherited = false
            };

            layer.DataObjectProperties.Add(dop);
            RefreshAfterLayerMutation(layer);
            SelectedDop = dop;
            EditDidSelectedDop = dop;
            StatusMessage = $"Created DOP '{dop.Id}' ({dop.BaseDataType}, {dop.BitLength}b).";
            return dop;
        }

        private void ExecuteCreateDop(object? parameter)
        {
            if (!EnsurePackageActive()) return;
            var layer = EnsureEditableLayer();
            var name = string.IsNullOrWhiteSpace(EditDopShortName) ||
                       layer.DataObjectProperties.Any(d => d.ShortName.Equals(EditDopShortName, StringComparison.OrdinalIgnoreCase))
                ? $"DOP_Custom_{layer.DataObjectProperties.Count + 1}"
                : EditDopShortName;

            CreateDop(name, EditDopBaseDataType, EditDopBitLength, EditDopPhysicalUnit, EditDopFactor, EditDopOffset);
        }

        private void ExecuteApplyDopEdits(object? parameter)
        {
            if (SelectedDop == null || _activeLayer == null)
            {
                MessageBox.Show("Select a DOP first, or click '+ Create DOP'.", "No DOP Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedDop.IsInherited)
            {
                SelectedDop.IsInherited = false;
                SelectedDop.DefinedInLayer = _activeLayer.ShortName;
            }

            var oldId = SelectedDop.Id;
            SelectedDop.Id = string.IsNullOrWhiteSpace(EditDopId) ? SelectedDop.Id : SanitizeIdentifier(EditDopId);
            SelectedDop.ShortName = string.IsNullOrWhiteSpace(EditDopShortName) ? SelectedDop.ShortName : EditDopShortName.Trim();
            SelectedDop.Category = EditDopCategory;
            SelectedDop.BaseDataType = EditDopBaseDataType;
            SelectedDop.BitLength = EditDopBitLength > 0 ? EditDopBitLength : 8;
            SelectedDop.PhysicalUnit = EditDopPhysicalUnit.Trim();
            SelectedDop.Factor = EditDopFactor;
            SelectedDop.Offset = EditDopOffset;

            // Propagate updated DOP specs to any DIDs & Service Parameters referencing this DOP!
            foreach (var did in _activeLayer.DataIdentifiers.Where(d => d.DopRef.Equals(oldId, StringComparison.OrdinalIgnoreCase) || d.DopRef.Equals(SelectedDop.Id, StringComparison.OrdinalIgnoreCase)))
            {
                did.ApplyDop(SelectedDop);
            }

            foreach (var p in _activeLayer.Services.SelectMany(s => s.RequestParameters.Concat(s.ResponseParameters))
                         .Where(p => p.DopRef.Equals(oldId, StringComparison.OrdinalIgnoreCase) || p.DopRef.Equals(SelectedDop.Id, StringComparison.OrdinalIgnoreCase)))
            {
                p.DopRef = SelectedDop.Id;
                p.DataType = SelectedDop.BaseDataType;
                p.BitLength = SelectedDop.BitLength;
            }

            RefreshAfterLayerMutation(_activeLayer);
            StatusMessage = $"Updated DOP '{SelectedDop.ShortName}' and synchronized linked DIDs/Parameters.";
        }

        private void ExecuteDeleteDop(object? parameter)
        {
            if (SelectedDop == null || _activeLayer == null) return;
            var dopId = SelectedDop.Id;
            _activeLayer.DataObjectProperties.Remove(SelectedDop);

            foreach (var did in _activeLayer.DataIdentifiers.Where(d => d.DopRef.Equals(dopId, StringComparison.OrdinalIgnoreCase)))
                did.DopRef = string.Empty;

            RefreshAfterLayerMutation(_activeLayer);
            SelectedDop = LayerDops.FirstOrDefault();
            StatusMessage = $"Deleted DOP '{dopId}'.";
        }

        private void ExecuteAssignSelectedDopRowToDid(object? parameter)
        {
            if (SelectedDop == null)
            {
                MessageBox.Show("Select a DOP row in the table first.", "Select DOP", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (SelectedDid == null)
            {
                MessageBox.Show("Select a DID in the 'Data Identifiers (DIDs)' tab first to assign this DOP to it.", "Select DID", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            AssignDopToDid(SelectedDid, SelectedDop);
        }

        // =========================================================================
        // 4. DIAGNOSTIC SERVICE & PARAMETER AUTHORING
        // =========================================================================

        private void ExecuteCreateService(object? parameter)
        {
            if (!EnsurePackageActive()) return;
            var layer = EnsureEditableLayer();

            var newSvc = new DiagnosticService
            {
                Id = $"SVC_Custom_{layer.Services.Count + 1}",
                ShortName = $"CustomService_{layer.Services.Count + 1}",
                LongName = $"Custom Diagnostic Service {layer.Services.Count + 1}",
                ServiceIdHex = "0x31",
                SubFunctionHex = "0x01",
                PositiveResponseSidHex = "0x71",
                DataCategory = "System Control",
                Semantic = "ROUTINE",
                Addressing = "Physical",
                DefinedInLayer = layer.ShortName,
                DefinedInLayerType = layer.LayerType.ToString(),
                IsInherited = false,
                RequestParameters = new List<DiagnosticParameter>
                {
                    new() { Name = "SID", Semantic = "SERVICE-ID", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = "0x31" },
                    new() { Name = "SubFunction", Semantic = "SUBFUNCTION", ParameterType = "VALUE", BytePosition = 1, BitLength = 8, DefaultValueHex = "0x01" }
                },
                ResponseParameters = new List<DiagnosticParameter>
                {
                    new() { Name = "ResponseSID", Semantic = "SERVICE-ID", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = "0x71" }
                }
            };

            layer.Services.Add(newSvc);
            RefreshAfterLayerMutation(layer);
            SelectedService = newSvc;
            StatusMessage = $"Created service [{newSvc.ServiceIdHex}] {newSvc.ShortName}. Edit details in Services or Service Inspector.";
        }

        private void ExecuteApplyServiceEdits(object? parameter)
        {
            if (SelectedService == null || _activeLayer == null) return;

            if (SelectedService.IsInherited)
            {
                SelectedService.IsInherited = false;
                SelectedService.DefinedInLayer = _activeLayer.ShortName;
            }

            SelectedService.ServiceIdHex = NormalizeHex2(SelectedService.ServiceIdHex);
            if (string.IsNullOrWhiteSpace(SelectedService.PositiveResponseSidHex) &&
                int.TryParse(SelectedService.ServiceIdHex.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var sidVal))
            {
                SelectedService.PositiveResponseSidHex = $"0x{(sidVal + 0x40):X2}";
            }

            SelectedService.RequestParameters = ServiceRequestParams.ToList();
            SelectedService.ResponseParameters = ServiceResponseParams.ToList();

            SelectedService.NegativeResponses = EditServiceNrcsText
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            OnPropertyChanged(nameof(SelectedServiceRequestPreview));
            OnPropertyChanged(nameof(SelectedServiceResponsePreview));
            OnPropertyChanged(nameof(SelectedServiceNegativeResponsesText));

            RefreshAfterLayerMutation(_activeLayer);
            StatusMessage = $"Saved changes to Service [{SelectedService.ServiceIdHex}] {SelectedService.ShortName}.";
        }

        private void ExecuteDeleteService(object? parameter)
        {
            if (SelectedService == null || _activeLayer == null) return;

            var name = SelectedService.ShortName;
            if (SelectedService.IsInherited)
            {
                var pref = _activeLayer.ParentRefs.FirstOrDefault(p =>
                    p.IsResolved && p.ResolvedLayerShortName.Equals(SelectedService.DefinedInLayer, StringComparison.OrdinalIgnoreCase))
                    ?? _activeLayer.ParentRefs.FirstOrDefault(p => p.IsResolved);
                if (pref != null && !pref.NotInheritedDiagComms.Contains(name))
                    pref.NotInheritedDiagComms.Add(name);
            }

            _activeLayer.Services.Remove(SelectedService);
            RefreshAfterLayerMutation(_activeLayer);
            SelectedService = LayerServices.FirstOrDefault();
            StatusMessage = $"Deleted Service '{name}'.";
        }

        private void ExecuteAddRequestParam(object? parameter)
        {
            if (SelectedService == null) return;
            var nextPos = ServiceRequestParams.Count > 0 ? ServiceRequestParams.Max(p => p.BytePosition) + 1 : 0;
            var p = new DiagnosticParameter
            {
                Name = $"ReqParam_{ServiceRequestParams.Count + 1}",
                Semantic = nextPos == 0 ? "SERVICE-ID" : "DATA",
                ParameterType = "VALUE",
                BytePosition = nextPos,
                BitLength = 8
            };
            SelectedService.RequestParameters.Add(p);
            ServiceRequestParams.Add(p);
            SelectedRequestParam = p;
            OnPropertyChanged(nameof(SelectedServiceRequestPreview));
            StatusMessage = $"Added Request Parameter '{p.Name}' at Byte {p.BytePosition}.";
        }

        private void ExecuteDeleteRequestParam(object? parameter)
        {
            if (SelectedService == null || SelectedRequestParam == null) return;
            SelectedService.RequestParameters.Remove(SelectedRequestParam);
            ServiceRequestParams.Remove(SelectedRequestParam);
            SelectedRequestParam = ServiceRequestParams.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedServiceRequestPreview));
        }

        private void ExecuteAssignDopToRequestParam(object? parameter)
        {
            if (SelectedRequestParam == null || SelectedParamAssignDop == null)
            {
                MessageBox.Show("Select a Request Parameter row AND choose a DOP first.", "Assign DOP to Parameter", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            SelectedRequestParam.DopRef = SelectedParamAssignDop.Id;
            SelectedRequestParam.DataType = SelectedParamAssignDop.BaseDataType;
            SelectedRequestParam.BitLength = SelectedParamAssignDop.BitLength;
            StatusMessage = $"Assigned DOP '{SelectedParamAssignDop.ShortName}' to Request Param '{SelectedRequestParam.Name}'.";
        }

        private void ExecuteAddResponseParam(object? parameter)
        {
            if (SelectedService == null) return;
            var nextPos = ServiceResponseParams.Count > 0 ? ServiceResponseParams.Max(p => p.BytePosition) + 1 : 0;
            var p = new DiagnosticParameter
            {
                Name = $"RespParam_{ServiceResponseParams.Count + 1}",
                Semantic = nextPos == 0 ? "SERVICE-ID" : "DATA",
                ParameterType = "VALUE",
                BytePosition = nextPos,
                BitLength = 16
            };
            SelectedService.ResponseParameters.Add(p);
            ServiceResponseParams.Add(p);
            SelectedResponseParam = p;
            OnPropertyChanged(nameof(SelectedServiceResponsePreview));
            StatusMessage = $"Added Positive Response Parameter '{p.Name}' at Byte {p.BytePosition}.";
        }

        private void ExecuteDeleteResponseParam(object? parameter)
        {
            if (SelectedService == null || SelectedResponseParam == null) return;
            SelectedService.ResponseParameters.Remove(SelectedResponseParam);
            ServiceResponseParams.Remove(SelectedResponseParam);
            SelectedResponseParam = ServiceResponseParams.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedServiceResponsePreview));
        }

        private void ExecuteAssignDopToResponseParam(object? parameter)
        {
            if (SelectedResponseParam == null || SelectedParamAssignDop == null)
            {
                MessageBox.Show("Select a Response Parameter row AND choose a DOP first.", "Assign DOP to Parameter", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            SelectedResponseParam.DopRef = SelectedParamAssignDop.Id;
            SelectedResponseParam.DataType = SelectedParamAssignDop.BaseDataType;
            SelectedResponseParam.BitLength = SelectedParamAssignDop.BitLength;
            StatusMessage = $"Assigned DOP '{SelectedParamAssignDop.ShortName}' to Response Param '{SelectedResponseParam.Name}'.";
        }

        // =========================================================================
        // 5. DTC & COMPARAM AUTHORING
        // =========================================================================

        private void ExecuteCreateDtc(object? parameter)
        {
            if (!EnsurePackageActive()) return;
            var layer = EnsureEditableLayer();

            var rawHex = NormalizeHex6(string.IsNullOrWhiteSpace(EditDtcHex) ? "0x042000" : EditDtcHex);
            long rawVal = long.TryParse(rawHex.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed) ? parsed : 270336;

            var dtc = new DiagnosticTroubleCode
            {
                Id = $"DTC_Custom_{layer.DiagnosticTroubleCodes.Count + 1}",
                TroubleCode = rawVal,
                TroubleCodeHex = $"0x{rawVal:X6}",
                FailureTypeByte = $"0x{(byte)(rawVal & 0xFF):X2}",
                DisplayCode = string.IsNullOrWhiteSpace(EditDtcDisplayCode) ? FormatUdsDtcCode(rawVal) : EditDtcDisplayCode.Trim(),
                Severity = string.IsNullOrWhiteSpace(EditDtcSeverity) ? "Warning" : EditDtcSeverity,
                Description = string.IsNullOrWhiteSpace(EditDtcDescription) ? "New Diagnostic Trouble Code" : EditDtcDescription.Trim(),
                DefinedInLayer = layer.ShortName,
                IsInherited = false
            };

            layer.DiagnosticTroubleCodes.Add(dtc);
            RefreshAfterLayerMutation(layer);
            SelectedDtc = dtc;
            StatusMessage = $"Created DTC [{dtc.DisplayCode}] on '{layer.ShortName}'.";
        }

        private void ExecuteApplyDtcEdits(object? parameter)
        {
            if (SelectedDtc == null || _activeLayer == null) return;
            if (SelectedDtc.IsInherited)
            {
                SelectedDtc.IsInherited = false;
                SelectedDtc.DefinedInLayer = _activeLayer.ShortName;
            }

            if (!string.IsNullOrWhiteSpace(EditDtcHex) &&
                long.TryParse(EditDtcHex.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var num))
            {
                SelectedDtc.TroubleCode = num;
                SelectedDtc.TroubleCodeHex = $"0x{num:X6}";
                SelectedDtc.FailureTypeByte = $"0x{(byte)(num & 0xFF):X2}";
            }

            SelectedDtc.DisplayCode = string.IsNullOrWhiteSpace(EditDtcDisplayCode)
                ? FormatUdsDtcCode(SelectedDtc.TroubleCode)
                : EditDtcDisplayCode.Trim();
            SelectedDtc.Severity = EditDtcSeverity;
            SelectedDtc.Description = EditDtcDescription;

            RefreshAfterLayerMutation(_activeLayer);
            StatusMessage = $"Updated DTC [{SelectedDtc.DisplayCode}].";
        }

        private void ExecuteDeleteDtc(object? parameter)
        {
            if (SelectedDtc == null || _activeLayer == null) return;
            var code = SelectedDtc.DisplayCode;
            _activeLayer.DiagnosticTroubleCodes.Remove(SelectedDtc);
            RefreshAfterLayerMutation(_activeLayer);
            SelectedDtc = LayerDtcs.FirstOrDefault();
            StatusMessage = $"Deleted DTC [{code}].";
        }

        private void ExecuteCreateComParam(object? parameter)
        {
            if (!EnsurePackageActive()) return;
            var layer = EnsureEditableLayer();

            var cp = new CommunicationParameter
            {
                Name = string.IsNullOrWhiteSpace(EditComParamName) ? $"CP_Custom_{layer.CommunicationParameters.Count + 1}" : EditComParamName.Trim(),
                Value = string.IsNullOrWhiteSpace(EditComParamValue) ? "0" : EditComParamValue.Trim(),
                Unit = EditComParamUnit.Trim(),
                Protocol = layer.LayerType == DiagnosticLayerType.Protocol ? layer.ShortName : "",
                Description = EditComParamDesc.Trim(),
                DefinedInLayer = layer.ShortName,
                IsInherited = false
            };

            layer.CommunicationParameters.Add(cp);
            RefreshAfterLayerMutation(layer);
            SelectedComParam = cp;
            StatusMessage = $"Added ComParam '{cp.Name} = {cp.Value}'.";
        }

        private void ExecuteApplyComParamEdits(object? parameter)
        {
            if (SelectedComParam == null || _activeLayer == null) return;
            if (SelectedComParam.IsInherited)
            {
                SelectedComParam.IsInherited = false;
                SelectedComParam.DefinedInLayer = _activeLayer.ShortName;
            }
            SelectedComParam.Name = EditComParamName.Trim();
            SelectedComParam.Value = EditComParamValue.Trim();
            SelectedComParam.Unit = EditComParamUnit.Trim();
            SelectedComParam.Description = EditComParamDesc.Trim();

            RefreshAfterLayerMutation(_activeLayer);
            StatusMessage = $"Updated ComParam '{SelectedComParam.Name}'.";
        }

        private void ExecuteDeleteComParam(object? parameter)
        {
            if (SelectedComParam == null || _activeLayer == null) return;
            var name = SelectedComParam.Name;
            _activeLayer.CommunicationParameters.Remove(SelectedComParam);
            RefreshAfterLayerMutation(_activeLayer);
            SelectedComParam = LayerComParams.FirstOrDefault();
            StatusMessage = $"Deleted ComParam '{name}'.";
        }

        // =========================================================================
        // 6. ISO LAYER, PARENT-REF & VARIANT PATTERN AUTHORING
        // =========================================================================

        private void ExecuteAddEcuVariant(object? parameter)
        {
            if (!EnsurePackageActive() || _currentPackage == null) return;
            var baseName = string.IsNullOrWhiteSpace(NewEcuLayerName)
                ? $"EV_Variant_{_currentPackage.EcuVariants.Count() + 1}"
                : SanitizeIdentifier(NewEcuLayerName);

            var bvParent = _currentPackage.BaseVariants.FirstOrDefault();
            var container = _activeLayer?.ContainerShortName ?? bvParent?.ContainerShortName ?? $"DLC_{_currentPackage.Name}";

            var newEv = new DiagnosticLayer
            {
                Id = baseName,
                ShortName = baseName,
                LongName = $"Authored ECU Variant - {baseName}",
                LayerType = DiagnosticLayerType.EcuVariant,
                ContainerShortName = container
            };

            if (bvParent != null)
            {
                newEv.ParentRefs.Add(new LayerParentRef
                {
                    IdRef = bvParent.Id,
                    DocRef = bvParent.ContainerShortName,
                    ParentRefType = "BASE-VARIANT-REF"
                });
            }

            _currentPackage.Layers.Add(newEv);
            OdxIsoLinker.RelinkPackage(_currentPackage);
            BuildTreeFromDiagnosticPackage(_currentPackage, preserveLayerName: newEv.ShortName);
            StatusMessage = $"Created ECU Variant '{newEv.ShortName}'{(bvParent != null ? $" inheriting '{bvParent.ShortName}'" : "")}.";
        }

        private void ExecuteAddBaseVariant(object? parameter)
        {
            if (!EnsurePackageActive() || _currentPackage == null) return;
            var name = string.IsNullOrWhiteSpace(NewEcuLayerName)
                ? $"BV_ECU_{_currentPackage.BaseVariants.Count() + 1}"
                : SanitizeIdentifier(NewEcuLayerName);

            var container = _activeLayer?.ContainerShortName ?? $"DLC_{_currentPackage.Name}";
            var newBv = new DiagnosticLayer
            {
                Id = name,
                ShortName = name,
                LongName = $"Authored Base Variant - {name}",
                LayerType = DiagnosticLayerType.BaseVariant,
                ContainerShortName = container
            };

            var proto = _currentPackage.Protocols.FirstOrDefault();
            if (proto != null)
            {
                newBv.ParentRefs.Add(new LayerParentRef
                {
                    IdRef = proto.Id,
                    DocRef = proto.ContainerShortName,
                    ParentRefType = "PROTOCOL-REF"
                });
            }

            _currentPackage.Layers.Add(newBv);
            OdxIsoLinker.RelinkPackage(_currentPackage);
            BuildTreeFromDiagnosticPackage(_currentPackage, preserveLayerName: newBv.ShortName);
            StatusMessage = $"Created BaseVariant '{newBv.ShortName}'.";
        }

        private void ExecuteDeleteActiveLayer(object? parameter)
        {
            if (_currentPackage == null || _activeLayer == null) return;
            var name = _activeLayer.ShortName;
            if (MessageBox.Show($"Delete diagnostic layer '{name}' from the package?", "Confirm Layer Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _currentPackage.Layers.Remove(_activeLayer);
            OdxIsoLinker.RelinkPackage(_currentPackage);
            BuildTreeFromDiagnosticPackage(_currentPackage);
            StatusMessage = $"Deleted diagnostic layer '{name}'.";
        }

        private void ExecuteAddParentRefConnection(object? parameter)
        {
            if (_currentPackage == null || _activeLayer == null || SelectedParentTargetLayer == null)
            {
                MessageBox.Show("Select an active ECU / BaseVariant and a Target Parent Layer first.", "ISO PARENT-REF Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (ReferenceEquals(_activeLayer, SelectedParentTargetLayer))
            {
                MessageBox.Show("A layer cannot inherit from itself per ISO 22901-1.", "Invalid Self-Reference", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _activeLayer.ParentRefs.Add(new LayerParentRef
            {
                IdRef = string.IsNullOrWhiteSpace(SelectedParentTargetLayer.Id) ? SelectedParentTargetLayer.ShortName : SelectedParentTargetLayer.Id,
                ShortNameRef = SelectedParentTargetLayer.ShortName,
                DocRef = SelectedParentTargetLayer.ContainerShortName,
                DocType = "CONTAINER",
                ParentRefType = SelectedParentRefType
            });

            OdxIsoLinker.RelinkPackage(_currentPackage);
            RefreshFullTreeAfterMutation(_activeLayer);
            StatusMessage = $"Linked '{_activeLayer.ShortName}' --({SelectedParentRefType})--> '{SelectedParentTargetLayer.ShortName}'.";
        }

        private void ExecuteRemoveParentRefConnection(object? parameter)
        {
            if (_currentPackage == null || SelectedIsoConnection?.SourceLayerObject == null || SelectedIsoConnection.ParentRefObject == null)
            {
                MessageBox.Show("Select a PARENT-REF row in the table above to remove.", "Select Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedIsoConnection.SourceLayerObject.ParentRefs.Remove(SelectedIsoConnection.ParentRefObject);
            OdxIsoLinker.RelinkPackage(_currentPackage);
            RefreshFullTreeAfterMutation(_activeLayer);
            StatusMessage = $"Removed PARENT-REF connection from '{SelectedIsoConnection.SourceLayer}'.";
        }

        private void ExecuteAddVariantPattern(object? parameter)
        {
            if (!EnsurePackageActive()) return;
            var layer = EnsureEditableLayer();
            var pat = new EcuVariantPattern
            {
                Name = string.IsNullOrWhiteSpace(NewPatternName) ? $"Pat_{layer.VariantPatterns.Count + 1}" : NewPatternName.Trim(),
                MatchingParameters = new List<MatchingParameterInfo>
                {
                    new()
                    {
                        DiagCommShortName = NewPatternService.Trim(),
                        OutParamShortName = NewPatternParam.Trim(),
                        ExpectedValue = NewPatternExpectedValue.Trim()
                    }
                }
            };
            layer.VariantPatterns.Add(pat);
            RefreshAfterLayerMutation(layer);
            StatusMessage = $"Added ECU Variant Pattern '{pat.Name}'.";
        }

        private void ExecuteDeleteVariantPattern(object? parameter)
        {
            if (SelectedVariantPattern?.LayerObject == null || SelectedVariantPattern.PatternObject == null) return;
            SelectedVariantPattern.LayerObject.VariantPatterns.Remove(SelectedVariantPattern.PatternObject);
            RefreshAfterLayerMutation(_activeLayer);
            StatusMessage = $"Deleted Variant Pattern '{SelectedVariantPattern.PatternName}'.";
        }

        // =========================================================================
        // 7. TREE & RIGHT PANEL POPULATION
        // =========================================================================

        private void RefreshFullTreeAfterMutation(DiagnosticLayer? layerToFocus)
        {
            if (_currentPackage == null) return;
            int tabBefore = SelectedTabIndex;
            var prevService = SelectedService;
            var prevDid = SelectedDid;
            var prevDop = SelectedDop;
            var prevDtc = SelectedDtc;
            var prevComParam = SelectedComParam;

            BuildTreeFromDiagnosticPackage(_currentPackage, preserveLayerName: layerToFocus?.ShortName);
            RestoreGridSelections(prevService, prevDid, prevDop, prevDtc, prevComParam);

            SelectedTabIndex = tabBefore;
        }

        private void RefreshAfterLayerMutation(DiagnosticLayer? layerToFocus)
        {
            if (_currentPackage == null) return;
            var layer = layerToFocus ?? _activeLayer;
            if (layer == null || !_layerNodeLookup.TryGetValue(layer, out var existingNode))
            {
                RefreshFullTreeAfterMutation(layer);
                return;
            }

            int tabBefore = SelectedTabIndex;
            var prevService = SelectedService;
            var prevDid = SelectedDid;
            var prevDop = SelectedDop;
            var prevDtc = SelectedDtc;
            var prevComParam = SelectedComParam;

            // Update only this layer's TreeNode label/children in place instead of rebuilding the whole package tree
            UpdateLayerTreeNodeInPlace(layer, existingNode);

            // Rebind only the active layer's grid collections in batch (single Reset per collection)
            PopulateActiveLayerPanels(layer);
            RestoreGridSelections(prevService, prevDid, prevDop, prevDtc, prevComParam);

            SelectedTabIndex = tabBefore;
        }

        private void RestoreGridSelections(
            DiagnosticService? prevService,
            DiagnosticDataIdentifier? prevDid,
            DataObjectProperty? prevDop,
            DiagnosticTroubleCode? prevDtc,
            CommunicationParameter? prevComParam)
        {
            if (prevService != null && LayerServices.Contains(prevService)) SelectedService = prevService;
            if (prevDid != null && LayerDids.Contains(prevDid)) SelectedDid = prevDid;
            if (prevDop != null && (LayerDops.Contains(prevDop) || AvailableDops.Contains(prevDop))) SelectedDop = prevDop;
            if (prevDtc != null && LayerDtcs.Contains(prevDtc)) SelectedDtc = prevDtc;
            if (prevComParam != null && LayerComParams.Contains(prevComParam)) SelectedComParam = prevComParam;
        }

        private void BuildTreeFromDiagnosticPackage(DiagnosticPackage package, string? preserveLayerName = null)
        {
            _ecuNodeLookup.Clear();
            _layerNodeLookup.Clear();
            _lastSelectedTreeNode = null;
            _pendingProgrammaticTarget = null;

            _allLayerServices.Clear();
            _allLayerDids.Clear();
            _allLayerDops.Clear();
            LayerServices.ReplaceAll(Array.Empty<DiagnosticService>());
            LayerDids.ReplaceAll(Array.Empty<DiagnosticDataIdentifier>());
            LayerDops.ReplaceAll(Array.Empty<DataObjectProperty>());
            AvailableDops.ReplaceAll(Array.Empty<DataObjectProperty>());
            LayerDtcs.ReplaceAll(Array.Empty<DiagnosticTroubleCode>());
            LayerComParams.ReplaceAll(Array.Empty<CommunicationParameter>());

            _activeLayer = null;
            SelectedService = null;
            SelectedDid = null;
            SelectedDop = null;
            SelectedDtc = null;
            CurrentDataServicesCount = 0;
            StoredDataServicesCount = 0;
            InheritedServicesCount = 0;
            TotalServicesCount = 0;

            AvailableTargetLayers.ReplaceAll(package.Layers);

            var metaList = new List<PackageMetadataItem>
            {
                new() { Key = "Package / ECU Name", Value = package.Name },
                new() { Key = "Standard Compliance", Value = "ISO 22901-1 (ODX 2.2.0) / ISO 22901-2 (PDX)" },
                new() { Key = "Source File", Value = Path.GetFileName(package.SourcePath) },
                new() { Key = "ODX / Catalog Version", Value = package.Version },
                new() { Key = "Updated UTC", Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
                new() { Key = "Total Layers (DLC)", Value = $"{package.Layers.Count} ({package.EcuVariants.Count()} ECU Variants, {package.BaseVariants.Count()} BaseVariants, {package.Protocols.Count()} Protocols)" }
            };

            var ablockRows = new List<AblockManifestRow>();
            if (package.Catalog != null)
            {
                if (!string.IsNullOrEmpty(package.Catalog.Creator))
                    metaList.Add(new PackageMetadataItem { Key = "Catalog Creator", Value = package.Catalog.Creator });
                if (!string.IsNullOrEmpty(package.Catalog.CompanyName))
                    metaList.Add(new PackageMetadataItem { Key = "OEM / Company", Value = package.Catalog.CompanyName });
                if (package.Catalog.Vehicles.Count > 0)
                    metaList.Add(new PackageMetadataItem { Key = "Vehicle Model (ODX-V)", Value = string.Join(", ", package.Catalog.Vehicles.Select(v => $"{v.Model} ({v.ModelYear})")) });
                if (package.Catalog.Ecus.Count > 0)
                    metaList.Add(new PackageMetadataItem { Key = "ECU Hardware Addresses", Value = string.Join(", ", package.Catalog.Ecus.Select(e => $"{e.EcuName} [{e.DiagnosticAddress}]")) });

                foreach (var ab in package.Catalog.Ablocks)
                {
                    foreach (var f in ab.Files)
                    {
                        ablockRows.Add(new AblockManifestRow
                        {
                            AblockName = ab.ShortName,
                            IsoCategory = ab.Category,
                            RelativePath = f.RelativePath,
                            MimeType = f.MimeType,
                            FileSizeBytes = f.FileSizeBytes,
                            Checksum = f.Checksum
                        });
                    }
                }
                if (ablockRows.Count == 0)
                {
                    foreach (var f in package.Catalog.Files)
                    {
                        ablockRows.Add(new AblockManifestRow
                        {
                            AblockName = f.ShortName,
                            IsoCategory = f.FileType,
                            RelativePath = f.RelativePath,
                            MimeType = f.MimeType,
                            FileSizeBytes = f.FileSizeBytes,
                            Checksum = f.Checksum
                        });
                    }
                }
            }

            PackageMetadata.ReplaceAll(metaList);
            AblockManifestRows.ReplaceAll(ablockRows);
            ConsistencyIssuesView.ReplaceAll(package.ConsistencyIssues);

            var ecuNames = new List<string>();
            var ecuNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void RegisterNode(string shortName, DlcTreeNode node)
            {
                if (string.IsNullOrWhiteSpace(shortName)) return;
                if (ecuNameSet.Add(shortName))
                    ecuNames.Add(shortName);
                _ecuNodeLookup[shortName] = node;
            }

            var pendingLogs = new List<DiagnosticLogItem>(256);

            var rootFolder = new DlcTreeNode
            {
                Name = $"Diagnostic Layer Containers [{package.Name}]",
                NodeType = "RootFolder",
                IsExpanded = true
            };
            var rootChildren = new List<DlcTreeNode>();

            if (package.VehicleInfos.Count > 0)
            {
                var odxVFolder = new DlcTreeNode
                {
                    Name = $"Vehicle Topology (ODX-V) ({package.VehicleInfos.Sum(v => v.LogicalLinks.Count)} Links)",
                    NodeType = "VehicleFolder",
                    IsExpanded = true
                };
                var odxVChildren = new List<DlcTreeNode>();
                foreach (var vi in package.VehicleInfos)
                {
                    var viNode = new DlcTreeNode { Name = $"{vi.ShortName} ({vi.Model})", NodeType = "VehicleFolder", IsExpanded = true, VehicleInfoData = vi };
                    var viChildren = new List<DlcTreeNode>();
                    foreach (var pvl in vi.PhysicalVehicleLinks)
                        viChildren.Add(new DlcTreeNode { Name = $"Bus: {pvl.ShortName} [{pvl.LinkType}]", NodeType = "ProtocolNode" });
                    foreach (var ll in vi.LogicalLinks)
                    {
                        var addrStr = !string.IsNullOrEmpty(ll.PhysicalRequestId) ? $" [{ll.PhysicalRequestId} -> {ll.PhysicalResponseId}]" : "";
                        viChildren.Add(new DlcTreeNode { Name = $"LogicalLink: {ll.ShortName}{addrStr}", NodeType = "LogicalLinkNode", LogicalLinkData = ll });
                    }
                    viNode.Children.ReplaceAll(viChildren);
                    odxVChildren.Add(viNode);
                }
                odxVFolder.Children.ReplaceAll(odxVChildren);
                rootChildren.Add(odxVFolder);
            }

            if (ablockRows.Count > 0)
            {
                var ablockFolder = new DlcTreeNode { Name = $"PDX Manifest ABLOCKs ({ablockRows.Count})", NodeType = "AblockNode" };
                ablockFolder.Children.ReplaceAll(ablockRows.Select(ab =>
                    new DlcTreeNode { Name = $"[{ab.IsoCategory}] {ab.AblockName} ({ab.RelativePath})", NodeType = "AblockNode" }));
                rootChildren.Add(ablockFolder);
            }

            if (package.Protocols.Any() || package.ComParamSpecs.Any())
            {
                var protoContainer = new DlcTreeNode { Name = $"DLC_Protocols_ODX-C ({package.Protocols.Count()})", NodeType = "Container" };
                var protoChildren = new List<DlcTreeNode>();
                foreach (var proto in package.Protocols)
                {
                    var protoNode = new DlcTreeNode { Name = $"Protocol: {proto.ShortName}", NodeType = "ProtocolNode", LayerData = proto };
                    RegisterNode(proto.ShortName, protoNode);
                    _layerNodeLookup[proto] = protoNode;
                    protoChildren.Add(protoNode);
                }
                protoContainer.Children.ReplaceAll(protoChildren);
                rootChildren.Add(protoContainer);
            }

            foreach (var sd in package.EcuSharedDatas)
            {
                var sdContainer = new DlcTreeNode { Name = $"DLC_{sd.ContainerShortName} ({sd.ShortName})", NodeType = "Container", LayerData = sd };
                RegisterNode(sd.ShortName, sdContainer);
                _layerNodeLookup[sd] = sdContainer;
                rootChildren.Add(sdContainer);
            }

            DlcTreeNode? firstPrimaryNode = null;
            DlcTreeNode? preservedNode = null;

            var ecuContainers = package.Layers
                .Where(l => l.LayerType is DiagnosticLayerType.BaseVariant or DiagnosticLayerType.EcuVariant or DiagnosticLayerType.FunctionalGroup)
                .GroupBy(l => string.IsNullOrWhiteSpace(l.ContainerShortName) ? l.ShortName : l.ContainerShortName, StringComparer.OrdinalIgnoreCase);

            foreach (var group in ecuContainers)
            {
                var dlcName = group.Key.StartsWith("DLC_", StringComparison.OrdinalIgnoreCase) ? group.Key : $"DLC_{group.Key}";
                var dlcContainer = new DlcTreeNode { Name = dlcName, NodeType = "Container", IsExpanded = true };
                var dlcChildren = new List<DlcTreeNode>();

                var functionalGroups = group.Where(l => l.LayerType == DiagnosticLayerType.FunctionalGroup).ToList();
                var baseVariants = group.Where(l => l.LayerType == DiagnosticLayerType.BaseVariant).ToList();
                var ecuVariants = group.Where(l => l.LayerType == DiagnosticLayerType.EcuVariant).ToList();

                dlcContainer.LayerData = ecuVariants.FirstOrDefault() ?? baseVariants.FirstOrDefault() ?? group.First();

                var dopList = group.SelectMany(l => l.DataObjectProperties).DistinctBy(d => d.Id).ToList();
                if (dopList.Any())
                {
                    var sharedFolder = new DlcTreeNode { Name = $"Shared Data & DOPs ({dopList.Count})", NodeType = "Folder" };
                    sharedFolder.Children.ReplaceAll(dopList.Select(dop => new DlcTreeNode { Name = dop.DisplayLabel, NodeType = "Folder" }));
                    dlcChildren.Add(sharedFolder);
                }

                if (functionalGroups.Any())
                {
                    var fgFolder = new DlcTreeNode { Name = $"Functional Groups ({functionalGroups.Count})", NodeType = "Folder" };
                    var fgChildren = new List<DlcTreeNode>();
                    foreach (var fg in functionalGroups)
                    {
                        var fgNode = new DlcTreeNode { Name = fg.ShortName, NodeType = "FunctionalGroupNode", LayerData = fg };
                        RegisterNode(fg.ShortName, fgNode);
                        _layerNodeLookup[fg] = fgNode;
                        fgChildren.Add(fgNode);
                    }
                    fgFolder.Children.ReplaceAll(fgChildren);
                    dlcChildren.Add(fgFolder);
                }

                if (baseVariants.Any())
                {
                    var bvFolder = new DlcTreeNode { Name = $"ECU BaseVariants ({baseVariants.Count})", NodeType = "Folder", IsExpanded = true };
                    var bvChildren = new List<DlcTreeNode>();
                    foreach (var bv in baseVariants)
                    {
                        var bvNode = BuildLayerTreeNode(bv, "BaseVariant", expandChildren: !ecuVariants.Any(), pendingLogs);
                        bvChildren.Add(bvNode);
                        RegisterNode(bv.ShortName, bvNode);
                        _layerNodeLookup[bv] = bvNode;
                        firstPrimaryNode ??= bvNode;
                        if (preserveLayerName != null && bv.ShortName.Equals(preserveLayerName, StringComparison.OrdinalIgnoreCase))
                            preservedNode = bvNode;
                    }
                    bvFolder.Children.ReplaceAll(bvChildren);
                    dlcChildren.Add(bvFolder);
                }

                if (ecuVariants.Any())
                {
                    var evFolder = new DlcTreeNode { Name = $"ECU Variants ({ecuVariants.Count})", NodeType = "Folder", IsExpanded = true };
                    var evChildren = new List<DlcTreeNode>();
                    foreach (var ev in ecuVariants)
                    {
                        var evNode = BuildLayerTreeNode(ev, "EcuVariant", expandChildren: true, pendingLogs);
                        evChildren.Add(evNode);
                        RegisterNode(ev.ShortName, evNode);
                        _layerNodeLookup[ev] = evNode;
                        if (firstPrimaryNode == null || firstPrimaryNode.NodeType != "EcuVariant")
                            firstPrimaryNode = evNode;
                        if (preserveLayerName != null && ev.ShortName.Equals(preserveLayerName, StringComparison.OrdinalIgnoreCase))
                            preservedNode = evNode;
                    }
                    evFolder.Children.ReplaceAll(evChildren);
                    dlcChildren.Add(evFolder);
                }

                dlcContainer.Children.ReplaceAll(dlcChildren);
                rootChildren.Add(dlcContainer);
            }

            rootFolder.Children.ReplaceAll(rootChildren);
            EcuList.ReplaceAll(ecuNames);
            CommunicationLogs.ReplaceAll(pendingLogs);
            DiagnosticTreeNodes.ReplaceAll(new[] { rootFolder });
            PopulateGlobalIsoCollections(package);

            var targetSelection = preservedNode ?? firstPrimaryNode ?? FindFirstLayerNode(rootFolder);
            if (targetSelection != null)
                SelectNodeProgrammatically(targetSelection);
            else
                OnTreeNodeSelected(rootFolder);
        }

        private void UpdateLayerTreeNodeInPlace(DiagnosticLayer layer, DlcTreeNode existingNode)
        {
            if (layer.LayerType is DiagnosticLayerType.Protocol or DiagnosticLayerType.EcuSharedData or DiagnosticLayerType.FunctionalGroup)
            {
                existingNode.Name = layer.LayerType == DiagnosticLayerType.Protocol
                    ? $"Protocol: {layer.ShortName}"
                    : layer.ShortName;
                return;
            }

            var updated = BuildLayerTreeNode(layer, existingNode.NodeType, existingNode.IsExpanded, pendingLogs: null);
            existingNode.Name = updated.Name;
            existingNode.Children.ReplaceAll(updated.Children);
        }

        private DlcTreeNode BuildLayerTreeNode(DiagnosticLayer layer, string nodeType, bool expandChildren, List<DiagnosticLogItem>? pendingLogs)
        {
            int ownCount = layer.Services.Count(s => !s.IsInherited);
            int inhCount = layer.Services.Count - ownCount;
            var labelSuffix = inhCount > 0 ? $" ({ownCount} Own + {inhCount} Inherited)" : $" ({layer.Services.Count} Services)";

            var layerNode = new DlcTreeNode
            {
                Name = $"{layer.ShortName}{labelSuffix}",
                NodeType = nodeType,
                IsExpanded = expandChildren,
                LayerData = layer
            };
            var layerChildren = new List<DlcTreeNode>(6);

            if (layer.ParentRefs.Count > 0)
            {
                var parentLinksFolder = new DlcTreeNode
                {
                    Name = $"ISO Parent Connections / PARENT-REFS ({layer.ParentRefs.Count})",
                    NodeType = "Folder",
                    IsExpanded = expandChildren && layer.ParentRefs.Count <= 15
                };
                parentLinksFolder.Children.ReplaceAll(layer.ParentRefs.Select(pref => new DlcTreeNode
                {
                    Name = $"-> {pref.DisplayTarget} ({(!string.IsNullOrEmpty(pref.ResolvedLayerType) ? pref.ResolvedLayerType : pref.ParentRefType)}) [{(pref.IsResolved ? "Linked" : "Unresolved")}]",
                    NodeType = "ParentLinkNode",
                    ParentRefData = pref
                }));
                layerChildren.Add(parentLinksFolder);
            }

            if (layer.VariantPatterns.Count > 0)
            {
                var patFolder = new DlcTreeNode { Name = $"Variant Patterns ({layer.VariantPatterns.Count})", NodeType = "Folder" };
                patFolder.Children.ReplaceAll(layer.VariantPatterns.Select(vp =>
                    new DlcTreeNode { Name = $"{vp.Name}: {vp.Summary}", NodeType = "PatternNode", VariantPatternData = vp }));
                layerChildren.Add(patFolder);
            }

            var currentDataServices = new List<DiagnosticService>();
            var storedDataServices = new List<DiagnosticService>();
            var controlServices = new List<DiagnosticService>();

            foreach (var svc in layer.Services)
            {
                switch (svc.DataCategory)
                {
                    case "Current Data":
                        currentDataServices.Add(svc);
                        break;
                    case "Stored Data":
                        storedDataServices.Add(svc);
                        break;
                    default:
                        controlServices.Add(svc);
                        break;
                }

                if (pendingLogs != null && pendingLogs.Count < 250)
                {
                    var scopeTag = svc.IsInherited ? $"Inherited <- {svc.DefinedInLayer}" : "Own Layer";
                    pendingLogs.Add(new DiagnosticLogItem
                    {
                        Timestamp = DateTime.Now,
                        Direction = "ISO",
                        ServiceId = svc.ServiceIdHex,
                        Description = $"{svc.ShortName} [{svc.DataCategory}] ({scopeTag})",
                        RawData = $"{svc.ServiceIdHex} {svc.SubFunctionHex}".Trim(),
                        Status = "Linked"
                    });
                }
            }

            // Avoid eagerly expanding huge folders (> 30 items) so large OEM PDX loads render immediately
            var currentFolder = new DlcTreeNode
            {
                Name = $"Current Data Services ({currentDataServices.Count})",
                NodeType = "Folder",
                IsExpanded = expandChildren && currentDataServices.Count <= 30
            };
            currentFolder.Children.ReplaceAll(currentDataServices.Select(CreateServiceNode));
            layerChildren.Add(currentFolder);

            var storedFolder = new DlcTreeNode
            {
                Name = $"Stored Data Services ({storedDataServices.Count})",
                NodeType = "Folder",
                IsExpanded = expandChildren && storedDataServices.Count <= 30
            };
            storedFolder.Children.ReplaceAll(storedDataServices.Select(CreateServiceNode));
            layerChildren.Add(storedFolder);

            if (controlServices.Count > 0)
            {
                var controlFolder = new DlcTreeNode { Name = $"System Control Services ({controlServices.Count})", NodeType = "Folder" };
                controlFolder.Children.ReplaceAll(controlServices.Select(CreateServiceNode));
                layerChildren.Add(controlFolder);
            }

            if (layer.DataIdentifiers.Count > 0)
            {
                var didsFolder = new DlcTreeNode { Name = $"Data Identifiers / DIDs ({layer.DataIdentifiers.Count})", NodeType = "Folder" };
                didsFolder.Children.ReplaceAll(layer.DataIdentifiers.Select(did =>
                {
                    var unitStr = !string.IsNullOrEmpty(did.Unit) ? $" [{did.Unit}]" : "";
                    var originTag = did.IsInherited ? $" [<- {did.DefinedInLayer}]" : " [Own]";
                    return new DlcTreeNode
                    {
                        Name = $"[{did.DidHex}] {did.ShortName} ({did.DataCategory}){unitStr}{originTag}",
                        NodeType = "DidNode",
                        DidData = did
                    };
                }));
                layerChildren.Add(didsFolder);
            }

            if (layer.DiagnosticTroubleCodes.Count > 0)
            {
                var dtcsFolder = new DlcTreeNode { Name = $"Trouble Codes / DTCs ({layer.DiagnosticTroubleCodes.Count})", NodeType = "Folder" };
                dtcsFolder.Children.ReplaceAll(layer.DiagnosticTroubleCodes.Select(dtc =>
                {
                    var originTag = dtc.IsInherited ? $" [<- {dtc.DefinedInLayer}]" : " [Own]";
                    return new DlcTreeNode
                    {
                        Name = $"[{dtc.DisplayCode}] {dtc.Description}{originTag}",
                        NodeType = "DtcNode",
                        DtcData = dtc
                    };
                }));
                layerChildren.Add(dtcsFolder);
            }

            layerNode.Children.ReplaceAll(layerChildren);
            return layerNode;
        }

        private static DlcTreeNode CreateServiceNode(DiagnosticService svc)
        {
            var nodeType = svc.DataCategory switch
            {
                "Current Data" => "ServiceCurrentData",
                "Stored Data" => "ServiceStoredData",
                _ => "ServiceControl"
            };
            var originSuffix = svc.IsInherited ? $" [<- {svc.DefinedInLayer}]" : " [Own]";
            return new DlcTreeNode { Name = $"[{svc.ServiceIdHex}] {svc.ShortName}{originSuffix}", NodeType = nodeType, ServiceData = svc };
        }

        private static DlcTreeNode? FindFirstLayerNode(DlcTreeNode node)
        {
            if (node.LayerData != null) return node;
            foreach (var child in node.Children)
            {
                var found = FindFirstLayerNode(child);
                if (found != null) return found;
            }
            return null;
        }

        private void SelectNodeProgrammatically(DlcTreeNode targetNode)
        {
            if (_lastSelectedTreeNode != null && !ReferenceEquals(_lastSelectedTreeNode, targetNode))
                _lastSelectedTreeNode.IsSelected = false;

            targetNode.IsExpanded = true;
            _pendingProgrammaticTarget = targetNode;
            _programmaticSelectHandledByUi = false;
            targetNode.IsSelected = true;

            bool alreadyHandled = _programmaticSelectHandledByUi;
            _pendingProgrammaticTarget = null;
            _programmaticSelectHandledByUi = false;

            if (!alreadyHandled)
                OnTreeNodeSelected(targetNode);
        }

        public void OnTreeNodeSelected(DlcTreeNode node)
        {
            if (ReferenceEquals(node, _pendingProgrammaticTarget))
                _programmaticSelectHandledByUi = true;

            _lastSelectedTreeNode = node;
            SelectedTreeNode = node;

            if (node.LayerData != null)
            {
                PopulateActiveLayerPanels(node.LayerData);
                return;
            }

            if (node.NodeType is "RootFolder" or "VehicleFolder" or "LogicalLinkNode" or "AblockNode" or "ParentLinkNode" or "PatternNode")
            {
                if (_currentPackage != null)
                    PopulateGlobalIsoCollections(_currentPackage, null);
                ActiveWorkspaceTitle = $"ISO Architecture View: {node.Name}";
                ActiveWorkspaceSubtitle = "ISO 22901-1 ODX-V Topology, Layer Inheritance (PARENT-REFS) & ISO 22901-2 PDX ABLOCKs";
                return;
            }

            if (node.ServiceData != null)
            {
                SelectedService = node.ServiceData;
                ActiveWorkspaceTitle = $"Service [{node.ServiceData.ServiceIdHex}] {node.ServiceData.ShortName} ({node.ServiceData.DataCategory})";
                ActiveWorkspaceSubtitle = $"Origin: {node.ServiceData.InheritanceScope} | Addressing: {node.ServiceData.Addressing}";
                return;
            }

            if (node.DidData != null)
            {
                SelectedDid = node.DidData;
                ActiveWorkspaceTitle = $"DID [{node.DidData.DidHex}] {node.DidData.ShortName}";
                ActiveWorkspaceSubtitle = $"Category: {node.DidData.DataCategory} | Access: {node.DidData.Access} | DOP: {node.DidData.DopRef}";
                return;
            }

            if (node.DtcData != null)
            {
                SelectedDtc = node.DtcData;
                ActiveWorkspaceTitle = $"DTC [{node.DtcData.DisplayCode}] ({node.DtcData.TroubleCodeHex})";
                ActiveWorkspaceSubtitle = $"Severity: {node.DtcData.Severity} | FTB: {node.DtcData.FailureTypeByte}";
            }
        }

        private void PopulateActiveLayerPanels(DiagnosticLayer layer)
        {
            _activeLayer = layer;

            _isUpdatingSelectionFromTree = true;
            if (EcuList.Contains(layer.ShortName) && _selectedEcu != layer.ShortName)
            {
                _selectedEcu = layer.ShortName;
                OnPropertyChanged(nameof(SelectedEcu));
            }
            _isUpdatingSelectionFromTree = false;

            ActiveWorkspaceTitle = $"{layer.LayerType}: {layer.ShortName}";
            var parentSummary = layer.ParentRefs.Count > 0 ? $" | Inherits From: {string.Join(", ", layer.ParentRefs.Select(p => p.DisplayTarget))}" : "";
            ActiveWorkspaceSubtitle = $"Container: {layer.ContainerShortName} | Source: {layer.SourceFileName}{parentSummary}";

            _allLayerServices.Clear();
            _allLayerServices.AddRange(layer.Services);
            ApplyServiceFilters();

            int currentCount = 0;
            int storedCount = 0;
            int inheritedCount = 0;
            foreach (var s in layer.Services)
            {
                if (s.DataCategory == "Current Data") currentCount++;
                else if (s.DataCategory == "Stored Data") storedCount++;
                if (s.IsInherited) inheritedCount++;
            }
            CurrentDataServicesCount = currentCount;
            StoredDataServicesCount = storedCount;
            InheritedServicesCount = inheritedCount;
            TotalServicesCount = layer.Services.Count;

            _allLayerDids.Clear();
            _allLayerDids.AddRange(layer.DataIdentifiers);
            ApplyDidFilters();

            _allLayerDops.Clear();
            _allLayerDops.AddRange(layer.DataObjectProperties);
            ApplyDopFilters();
            RefreshAvailableDopsPool(layer);

            LayerDtcs.ReplaceAll(layer.DiagnosticTroubleCodes);
            LayerComParams.ReplaceAll(layer.CommunicationParameters);

            SelectedService = layer.Services.FirstOrDefault();
            SelectedDid = layer.DataIdentifiers.FirstOrDefault();
            SelectedDop = AvailableDops.FirstOrDefault();
            SelectedDtc = layer.DiagnosticTroubleCodes.FirstOrDefault();
            SelectedComParam = layer.CommunicationParameters.FirstOrDefault();

            if (_currentPackage != null)
                PopulateGlobalIsoCollections(_currentPackage, layer);
        }

        private void RefreshAvailableDopsPool(DiagnosticLayer focusLayer)
        {
            var pool = new List<DataObjectProperty>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var d in focusLayer.DataObjectProperties)
            {
                if (seenIds.Add(d.Id)) pool.Add(d);
            }

            if (_currentPackage != null)
            {
                foreach (var l in _currentPackage.Layers)
                {
                    foreach (var d in l.DataObjectProperties)
                    {
                        if (seenIds.Add(d.Id)) pool.Add(d);
                    }
                }
            }

            if (AvailableDops.Count == pool.Count && AvailableDops.SequenceEqual(pool))
                return;

            AvailableDops.ReplaceAll(pool);
        }

        private void PopulateGlobalIsoCollections(DiagnosticPackage package, DiagnosticLayer? focusLayer = null)
        {
            var isoRows = new List<IsoConnectionRow>();
            var patternRows = new List<VariantPatternRow>();
            var linkRows = new List<LogicalLinkRow>();

            var layersToInspect = focusLayer != null ? new[] { focusLayer } : package.Layers.ToArray();

            foreach (var l in layersToInspect)
            {
                foreach (var pref in l.ParentRefs)
                {
                    var excludedList = pref.NotInheritedDiagComms.Concat(pref.NotInheritedDops).Concat(pref.NotInheritedTables).ToList();
                    isoRows.Add(new IsoConnectionRow
                    {
                        SourceLayer = l.ShortName,
                        SourceType = l.LayerType.ToString(),
                        RelationType = pref.ParentRefType,
                        TargetLayer = pref.DisplayTarget,
                        TargetType = !string.IsNullOrEmpty(pref.ResolvedLayerType) ? pref.ResolvedLayerType : pref.DocType,
                        DocRefContainer = string.IsNullOrEmpty(pref.DocRef) ? l.ContainerShortName : pref.DocRef,
                        Status = pref.ResolutionStatusText,
                        ExcludedItems = excludedList.Count > 0 ? string.Join(", ", excludedList) : "None",
                        SourceLayerObject = l,
                        ParentRefObject = pref
                    });
                }

                foreach (var pat in l.VariantPatterns)
                {
                    if (pat.MatchingParameters.Count == 0)
                    {
                        patternRows.Add(new VariantPatternRow { EcuVariant = l.ShortName, PatternName = pat.Name, DiagService = "-", ParameterName = "-", ExpectedValue = "-", LayerObject = l, PatternObject = pat });
                    }
                    else
                    {
                        foreach (var mp in pat.MatchingParameters)
                        {
                            patternRows.Add(new VariantPatternRow
                            {
                                EcuVariant = l.ShortName,
                                PatternName = pat.Name,
                                DiagService = mp.DiagCommShortName,
                                ParameterName = mp.OutParamShortName,
                                ExpectedValue = mp.ExpectedValue,
                                LayerObject = l,
                                PatternObject = pat
                            });
                        }
                    }
                }
            }

            foreach (var vi in package.VehicleInfos)
            {
                foreach (var ll in vi.LogicalLinks)
                {
                    linkRows.Add(new LogicalLinkRow
                    {
                        VehicleModel = vi.Model,
                        LogicalLinkName = ll.ShortName,
                        Role = ll.LinkRole,
                        PhysicalBus = !string.IsNullOrEmpty(ll.PhysicalVehicleLinkShortName) ? ll.PhysicalVehicleLinkShortName : ll.PhysicalVehicleLinkRef,
                        Protocol = !string.IsNullOrEmpty(ll.ResolvedProtocolName) ? ll.ResolvedProtocolName : ll.ProtocolRef,
                        BaseVariant = !string.IsNullOrEmpty(ll.ResolvedBaseVariantName) ? ll.ResolvedBaseVariantName : ll.BaseVariantRef,
                        FunctionalGroup = !string.IsNullOrEmpty(ll.ResolvedFunctionalGroupName) ? ll.ResolvedFunctionalGroupName : ll.FunctionalGroupRef,
                        PhysicalReqId = ll.PhysicalRequestId,
                        PhysicalRespId = ll.PhysicalResponseId,
                        FunctionalReqId = ll.FunctionalRequestId
                    });
                }
            }

            IsoConnections.ReplaceAll(isoRows);
            VariantPatternsView.ReplaceAll(patternRows);
            LogicalLinksView.ReplaceAll(linkRows);
        }

        private void SyncServiceParamsFromSelection()
        {
            if (SelectedService == null)
            {
                ServiceRequestParams.ReplaceAll(Array.Empty<DiagnosticParameter>());
                ServiceResponseParams.ReplaceAll(Array.Empty<DiagnosticParameter>());
                SelectedRequestParam = null;
                SelectedResponseParam = null;
                return;
            }

            ServiceRequestParams.ReplaceAll(SelectedService.RequestParameters);
            ServiceResponseParams.ReplaceAll(SelectedService.ResponseParameters);

            SelectedRequestParam = ServiceRequestParams.FirstOrDefault();
            SelectedResponseParam = ServiceResponseParams.FirstOrDefault();
        }

        private void LoadDidIntoEditor(DiagnosticDataIdentifier? did)
        {
            if (did == null)
            {
                EditDidHex = string.Empty;
                EditDidName = string.Empty;
                EditDidLongName = string.Empty;
                EditDidCategory = "Current Data";
                EditDidAccess = "Read";
                EditDidSelectedDop = null;
                EditDidDataType = "A_UINT32";
                EditDidByteLength = 2;
                EditDidUnit = string.Empty;
                EditDidScalingFactor = 1.0;
                EditDidOffset = 0.0;
                EditDidDescription = string.Empty;
                return;
            }

            EditDidHex = did.DidHex;
            EditDidName = did.ShortName;
            EditDidLongName = did.LongName;
            EditDidCategory = string.IsNullOrWhiteSpace(did.DataCategory) ? "Current Data" : did.DataCategory;
            EditDidAccess = string.IsNullOrWhiteSpace(did.Access) ? "Read" : did.Access;
            EditDidDataType = did.DataType;
            EditDidByteLength = did.ByteLength > 0 ? did.ByteLength : 1;
            EditDidUnit = did.Unit;
            EditDidScalingFactor = did.ScalingFactor;
            EditDidOffset = did.Offset;
            EditDidDescription = did.Description;

            var matchedDop = AvailableDops.FirstOrDefault(d =>
                d.Id.Equals(did.DopRef, StringComparison.OrdinalIgnoreCase) ||
                d.ShortName.Equals(did.DopRef, StringComparison.OrdinalIgnoreCase));
            _editDidSelectedDop = matchedDop;
            OnPropertyChanged(nameof(EditDidSelectedDop));
        }

        private void LoadDopIntoEditor(DataObjectProperty? dop)
        {
            if (dop == null) return;
            EditDopId = dop.Id;
            EditDopShortName = dop.ShortName;
            EditDopCategory = string.IsNullOrWhiteSpace(dop.Category) ? "IDENTICAL" : dop.Category;
            EditDopBaseDataType = string.IsNullOrWhiteSpace(dop.BaseDataType) ? "A_UINT32" : dop.BaseDataType;
            EditDopBitLength = dop.BitLength > 0 ? dop.BitLength : 16;
            EditDopPhysicalUnit = dop.PhysicalUnit;
            EditDopFactor = dop.Factor;
            EditDopOffset = dop.Offset;
        }

        private void ApplyServiceFilters()
        {
            IEnumerable<DiagnosticService> query = _allLayerServices;

            if (!string.IsNullOrEmpty(ServiceCategoryFilter) && ServiceCategoryFilter != "All")
                query = query.Where(s => string.Equals(s.DataCategory, ServiceCategoryFilter, StringComparison.OrdinalIgnoreCase));

            if (ServiceScopeFilter == "Own Layer Only")
                query = query.Where(s => !s.IsInherited);
            else if (ServiceScopeFilter == "Inherited From Parents")
                query = query.Where(s => s.IsInherited);

            if (!string.IsNullOrWhiteSpace(ServiceSearchText))
            {
                var term = ServiceSearchText.Trim();
                query = query.Where(s =>
                    s.ShortName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    s.ServiceIdHex.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    s.Semantic.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    s.DefinedInLayer.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    s.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            LayerServices.ReplaceAll(query);
        }

        private void ApplyDidFilters()
        {
            IEnumerable<DiagnosticDataIdentifier> query = _allLayerDids;

            if (!string.IsNullOrWhiteSpace(DidSearchText))
            {
                var term = DidSearchText.Trim();
                query = query.Where(d =>
                    d.ShortName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    d.DidHex.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    d.DataCategory.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    d.DopRef.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    d.Unit.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            LayerDids.ReplaceAll(query);
        }

        private void ApplyDopFilters()
        {
            IEnumerable<DataObjectProperty> query = _allLayerDops;

            if (!string.IsNullOrWhiteSpace(DopSearchText))
            {
                var term = DopSearchText.Trim();
                query = query.Where(d =>
                    d.Id.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    d.ShortName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    d.BaseDataType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    d.PhysicalUnit.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            LayerDops.ReplaceAll(query);
        }

        private void ExecuteClearWorkspace(object? parameter) => ClearWorkspace();

        public void ClearWorkspace()
        {
            _serviceSearchTimer?.Stop();
            _didSearchTimer?.Stop();
            _dopSearchTimer?.Stop();

            DiagnosticTreeNodes.ReplaceAll(Array.Empty<DlcTreeNode>());
            _loadedPackages.Clear();
            _ecuNodeLookup.Clear();
            _layerNodeLookup.Clear();
            _lastSelectedTreeNode = null;
            _pendingProgrammaticTarget = null;
            OdxProjects.ReplaceAll(Array.Empty<string>());
            EcuList.ReplaceAll(Array.Empty<string>());
            AvailableTargetLayers.ReplaceAll(Array.Empty<DiagnosticLayer>());

            _allLayerServices.Clear();
            _allLayerDids.Clear();
            _allLayerDops.Clear();
            LayerServices.ReplaceAll(Array.Empty<DiagnosticService>());
            ServiceRequestParams.ReplaceAll(Array.Empty<DiagnosticParameter>());
            ServiceResponseParams.ReplaceAll(Array.Empty<DiagnosticParameter>());
            LayerDids.ReplaceAll(Array.Empty<DiagnosticDataIdentifier>());
            LayerDops.ReplaceAll(Array.Empty<DataObjectProperty>());
            AvailableDops.ReplaceAll(Array.Empty<DataObjectProperty>());
            LayerDtcs.ReplaceAll(Array.Empty<DiagnosticTroubleCode>());
            LayerComParams.ReplaceAll(Array.Empty<CommunicationParameter>());
            CommunicationLogs.ReplaceAll(Array.Empty<DiagnosticLogItem>());
            PackageMetadata.ReplaceAll(Array.Empty<PackageMetadataItem>());
            IsoConnections.ReplaceAll(Array.Empty<IsoConnectionRow>());
            LogicalLinksView.ReplaceAll(Array.Empty<LogicalLinkRow>());
            VariantPatternsView.ReplaceAll(Array.Empty<VariantPatternRow>());
            AblockManifestRows.ReplaceAll(Array.Empty<AblockManifestRow>());
            ConsistencyIssuesView.ReplaceAll(Array.Empty<OdxConsistencyIssue>());

            _currentPackage = null;
            _activeLayer = null;
            SelectedTreeNode = null;
            SelectedService = null;
            SelectedDid = null;
            SelectedDop = null;
            SelectedDtc = null;
            SelectedComParam = null;
            _selectedOdxProject = string.Empty;
            _selectedEcu = string.Empty;
            OnPropertyChanged(nameof(SelectedOdxProject));
            OnPropertyChanged(nameof(SelectedEcu));
            OnPropertyChanged(nameof(IsPackageLoaded));

            CurrentDataServicesCount = 0;
            StoredDataServicesCount = 0;
            InheritedServicesCount = 0;
            TotalServicesCount = 0;

            ActiveWorkspaceTitle = "No PDX / ODX Package Loaded";
            ActiveWorkspaceSubtitle = "Click 'New Project', 'Load PDX', or 'Import ODX' to author or edit ISO 22901 containers.";
            StatusMessage = "Workspace cleared.";

            CleanupTempWorkspaces();
        }

        private async void ExecuteConsistencyCheck(object? parameter)
        {
            if (IsBusy) return;
            if (_currentPackage == null)
            {
                MessageBox.Show("No PDX or ODX package is currently loaded.\nCreate a New Project or load a package first.", "ISO 22901 Consistency Check", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var pkgSnapshot = _currentPackage;
            IsBusy = true;
            BusyMessage = "Validating ISO 22901 cross-references & inheritance...";

            try
            {
                await Task.Run(() => OdxIsoLinker.LinkPackage(pkgSnapshot));
                ConsistencyIssuesView.ReplaceAll(pkgSnapshot.ConsistencyIssues);

                int issues = pkgSnapshot.ConsistencyIssues.Count;
                int resolvedParents = pkgSnapshot.Layers.Sum(l => l.ParentRefs.Count(p => p.IsResolved));
                int totalParents = pkgSnapshot.Layers.Sum(l => l.ParentRefs.Count);

                MessageBox.Show(
                    $"{(issues == 0 ? "ISO 22901-1 / ISO 22901-2 Check PASSED!" : $"Found {issues} consistency issue(s) (see ISO Connections tab).")}\n\n" +
                    $"Active Layer Services: {TotalServicesCount} ({InheritedServicesCount} Inherited, {TotalServicesCount - InheritedServicesCount} Own)\n" +
                    $"DIDs: {LayerDids.Count} | DOPs: {LayerDops.Count} | DTCs: {LayerDtcs.Count}\n" +
                    $"PARENT-REFS Resolved: {resolvedParents}/{totalParents}",
                    "ISO 22901 Consistency Check", MessageBoxButton.OK,
                    issues == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private bool EnsurePackageActive()
        {
            if (_currentPackage == null)
                CreateNewBlankProject("CustomECU");
            return _currentPackage != null;
        }

        private DiagnosticLayer EnsureEditableLayer()
        {
            if (_activeLayer != null) return _activeLayer;
            EnsurePackageActive();
            _activeLayer = _currentPackage!.EcuVariants.FirstOrDefault() ?? _currentPackage.Layers.First();
            return _activeLayer;
        }

        private static string GenerateNextFreeDidHex(DiagnosticLayer layer)
        {
            int candidate = 0x1000;
            var existing = layer.DataIdentifiers.Select(d => d.DidHex).ToHashSet(StringComparer.OrdinalIgnoreCase);
            while (existing.Contains($"0x{candidate:X4}") && candidate < 0xEFFF)
                candidate++;
            return $"0x{candidate:X4}";
        }

        private static string NormalizeHex2(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "0x22";
            var clean = input.Trim().Replace("0x", "", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var num))
                return $"0x{num:X2}";
            return input;
        }

        private static string NormalizeHex4(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var clean = input.Trim().Replace("0x", "", StringComparison.OrdinalIgnoreCase);
            if (int.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var num))
                return $"0x{num:X4}";
            return string.Empty;
        }

        private static string NormalizeHex6(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "0x000000";
            var clean = input.Trim().Replace("0x", "", StringComparison.OrdinalIgnoreCase);
            if (long.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var num))
                return $"0x{num:X6}";
            return "0x000000";
        }

        private static int ParseHexInt(string hex) =>
            int.TryParse((hex ?? "").Replace("0x", "", StringComparison.OrdinalIgnoreCase), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v) ? v : 0;

        private static string FormatUdsDtcCode(long code)
        {
            var highByte = (byte)((code >> 16) & 0xFF);
            var lowByte = (byte)((code >> 8) & 0xFF);
            var ftb = (byte)(code & 0xFF);
            char prefix = (highByte >> 6) switch { 0 => 'P', 1 => 'C', 2 => 'B', 3 => 'U', _ => 'P' };
            return $"{prefix}{(highByte >> 4) & 0x03:X1}{highByte & 0x0F:X1}{(lowByte >> 4) & 0x0F:X1}{lowByte & 0x0F:X1}-{ftb:X2}";
        }

        private static string SanitizeIdentifier(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "Item";
            var sb = new StringBuilder();
            foreach (char c in input.Trim())
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            return sb.ToString();
        }

        public void CleanupTempWorkspaces()
        {
            foreach (var dir in _tempWorkspaces)
            {
                try
                {
                    if (Directory.Exists(dir))
                        Directory.Delete(dir, true);
                }
                catch { }
            }
            _tempWorkspaces.Clear();
        }
    }
}
