namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents a Diagnostic Trouble Code (DTC) according to ISO 14229 / SAE J2012.
/// </summary>
public class DiagnosticTroubleCode : ObservableModelBase
{
    private string _id = string.Empty;
    private long _troubleCode;
    private string _troubleCodeHex = string.Empty;
    private string _displayCode = string.Empty;
    private string _description = string.Empty;
    private string _severity = string.Empty;
    private string _failureTypeByte = string.Empty;
    private string _statusMask = string.Empty;
    private string _definedInLayer = string.Empty;
    private bool _isInherited;

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public long TroubleCode { get => _troubleCode; set => SetProperty(ref _troubleCode, value); }
    public string TroubleCodeHex { get => _troubleCodeHex; set => SetProperty(ref _troubleCodeHex, value); }
    public string DisplayCode { get => _displayCode; set => SetProperty(ref _displayCode, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public string Severity { get => _severity; set => SetProperty(ref _severity, value); }
    public string FailureTypeByte { get => _failureTypeByte; set => SetProperty(ref _failureTypeByte, value); }
    public string StatusMask { get => _statusMask; set => SetProperty(ref _statusMask, value); }

    public string DefinedInLayer
    {
        get => _definedInLayer;
        set { if (SetProperty(ref _definedInLayer, value)) OnPropertyChanged(nameof(InheritanceScope)); }
    }

    public bool IsInherited
    {
        get => _isInherited;
        set { if (SetProperty(ref _isInherited, value)) OnPropertyChanged(nameof(InheritanceScope)); }
    }

    public string InheritanceScope => IsInherited ? $"Inherited ({DefinedInLayer})" : "Own";

    public DiagnosticTroubleCode CloneAsInherited(string originLayerName)
    {
        return new DiagnosticTroubleCode
        {
            Id = Id,
            TroubleCode = TroubleCode,
            TroubleCodeHex = TroubleCodeHex,
            DisplayCode = DisplayCode,
            Description = Description,
            Severity = Severity,
            FailureTypeByte = FailureTypeByte,
            StatusMask = StatusMask,
            DefinedInLayer = string.IsNullOrEmpty(DefinedInLayer) ? originLayerName : DefinedInLayer,
            IsInherited = true
        };
    }
}
