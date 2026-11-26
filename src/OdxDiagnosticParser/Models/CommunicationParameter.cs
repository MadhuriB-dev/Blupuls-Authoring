namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents a communication parameter (ComParam) defining bus timing or addressing.
/// </summary>
public class CommunicationParameter : ObservableModelBase
{
    private string _name = string.Empty;
    private string _value = string.Empty;
    private string _unit = string.Empty;
    private string _protocol = string.Empty;
    private string _description = string.Empty;
    private string _paramClass = string.Empty;
    private string _definedInLayer = string.Empty;
    private bool _isInherited;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Value { get => _value; set => SetProperty(ref _value, value); }
    public string Unit { get => _unit; set => SetProperty(ref _unit, value); }
    public string Protocol { get => _protocol; set => SetProperty(ref _protocol, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
    public string ParamClass { get => _paramClass; set => SetProperty(ref _paramClass, value); }

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

    public CommunicationParameter CloneAsInherited(string originLayerName, string protocolName = "")
    {
        return new CommunicationParameter
        {
            Name = Name,
            Value = Value,
            Unit = Unit,
            Protocol = !string.IsNullOrEmpty(Protocol) ? Protocol : protocolName,
            Description = Description,
            ParamClass = ParamClass,
            DefinedInLayer = string.IsNullOrEmpty(DefinedInLayer) ? originLayerName : DefinedInLayer,
            IsInherited = true
        };
    }
}
