namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents a UDS/KWP Diagnostic Service (e.g., ReadDataByIdentifier 0x22, DiagnosticSessionControl 0x10).
/// </summary>
public class DiagnosticService : ObservableModelBase
{
    private string _id = string.Empty;
    private string _shortName = string.Empty;
    private string _longName = string.Empty;
    private string _description = string.Empty;
    private string _semantic = string.Empty;
    private string _dataCategory = "Current Data";
    private string _serviceIdHex = string.Empty;
    private string _subFunctionHex = string.Empty;
    private string _positiveResponseSidHex = string.Empty;
    private string _addressing = "Physical";
    private string _definedInLayer = string.Empty;
    private string _definedInLayerType = string.Empty;
    private bool _isInherited;

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string ShortName
    {
        get => _shortName;
        set { if (SetProperty(ref _shortName, value)) OnPropertyChanged(nameof(FullDisplayName)); }
    }
    public string LongName { get => _longName; set => SetProperty(ref _longName, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public string Semantic { get => _semantic; set => SetProperty(ref _semantic, value); }
    public string DataCategory { get => _dataCategory; set => SetProperty(ref _dataCategory, value); }
    public string ServiceIdHex
    {
        get => _serviceIdHex;
        set { if (SetProperty(ref _serviceIdHex, value)) OnPropertyChanged(nameof(FullDisplayName)); }
    }
    public string SubFunctionHex { get => _subFunctionHex; set => SetProperty(ref _subFunctionHex, value); }
    public string PositiveResponseSidHex { get => _positiveResponseSidHex; set => SetProperty(ref _positiveResponseSidHex, value); }
    public string Addressing { get => _addressing; set => SetProperty(ref _addressing, value); }

    public string DefinedInLayer
    {
        get => _definedInLayer;
        set { if (SetProperty(ref _definedInLayer, value)) OnPropertyChanged(nameof(InheritanceScope)); }
    }

    public string DefinedInLayerType { get => _definedInLayerType; set => SetProperty(ref _definedInLayerType, value); }

    public bool IsInherited
    {
        get => _isInherited;
        set { if (SetProperty(ref _isInherited, value)) OnPropertyChanged(nameof(InheritanceScope)); }
    }

    public List<string> FunctionalClasses { get; set; } = new();
    public List<DiagnosticParameter> RequestParameters { get; set; } = new();
    public List<DiagnosticParameter> ResponseParameters { get; set; } = new();
    public List<string> NegativeResponses { get; set; } = new();

    public string InheritanceScope => IsInherited ? $"Inherited ({DefinedInLayer})" : "Own (Layer)";

    public string FullDisplayName => string.IsNullOrWhiteSpace(ServiceIdHex)
        ? ShortName
        : $"{ServiceIdHex} {ShortName}";

    public DiagnosticService CloneAsInherited(string originLayerName, string originLayerType)
    {
        return new DiagnosticService
        {
            Id = Id,
            ShortName = ShortName,
            LongName = LongName,
            Description = Description,
            Semantic = Semantic,
            DataCategory = DataCategory,
            ServiceIdHex = ServiceIdHex,
            SubFunctionHex = SubFunctionHex,
            PositiveResponseSidHex = PositiveResponseSidHex,
            Addressing = Addressing,
            DefinedInLayer = string.IsNullOrEmpty(DefinedInLayer) ? originLayerName : DefinedInLayer,
            DefinedInLayerType = string.IsNullOrEmpty(DefinedInLayerType) ? originLayerType : DefinedInLayerType,
            IsInherited = true,
            FunctionalClasses = new List<string>(FunctionalClasses),
            RequestParameters = new List<DiagnosticParameter>(RequestParameters),
            ResponseParameters = new List<DiagnosticParameter>(ResponseParameters),
            NegativeResponses = new List<string>(NegativeResponses)
        };
    }
}
