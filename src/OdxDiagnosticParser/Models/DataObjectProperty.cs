namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents an ODX Data Object Property (DOP) specifying serialization and scaling.
/// </summary>
public class DataObjectProperty : ObservableModelBase
{
    private string _id = string.Empty;
    private string _shortName = string.Empty;
    private string _longName = string.Empty;
    private string _category = "IDENTICAL";
    private string _baseDataType = "A_UINT32";
    private int _bitLength = 8;
    private string _physicalUnit = string.Empty;
    private double _factor = 1.0;
    private double _offset;
    private double? _minPhysicalValue;
    private double? _maxPhysicalValue;
    private string _definedInLayer = string.Empty;
    private bool _isInherited;

    public string Id { get => _id; set => SetProperty(ref _id, value); }
    public string ShortName { get => _shortName; set => SetProperty(ref _shortName, value); }
    public string LongName { get => _longName; set => SetProperty(ref _longName, value); }
    public string Category { get => _category; set => SetProperty(ref _category, value); } // IDENTICAL, LINEAR, TEXTTABLE, SCALE-LINEAR
    public string BaseDataType { get => _baseDataType; set => SetProperty(ref _baseDataType, value); }
    public int BitLength { get => _bitLength; set => SetProperty(ref _bitLength, value); }
    public string PhysicalUnit { get => _physicalUnit; set => SetProperty(ref _physicalUnit, value); }
    public double Factor { get => _factor; set => SetProperty(ref _factor, value); }
    public double Offset { get => _offset; set => SetProperty(ref _offset, value); }
    public double? MinPhysicalValue { get => _minPhysicalValue; set => SetProperty(ref _minPhysicalValue, value); }
    public double? MaxPhysicalValue { get => _maxPhysicalValue; set => SetProperty(ref _maxPhysicalValue, value); }
    public Dictionary<long, string> TextTableEntries { get; set; } = new();

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
    public string DisplayLabel => $"{ShortName} [{BaseDataType}, {BitLength}b{(string.IsNullOrWhiteSpace(PhysicalUnit) ? "" : $", {PhysicalUnit}")}]";

    public DataObjectProperty CloneAsInherited(string originLayerName)
    {
        return new DataObjectProperty
        {
            Id = Id,
            ShortName = ShortName,
            LongName = LongName,
            Category = Category,
            BaseDataType = BaseDataType,
            BitLength = BitLength,
            PhysicalUnit = PhysicalUnit,
            Factor = Factor,
            Offset = Offset,
            MinPhysicalValue = MinPhysicalValue,
            MaxPhysicalValue = MaxPhysicalValue,
            TextTableEntries = new Dictionary<long, string>(TextTableEntries),
            DefinedInLayer = string.IsNullOrEmpty(DefinedInLayer) ? originLayerName : DefinedInLayer,
            IsInherited = true
        };
    }
}
