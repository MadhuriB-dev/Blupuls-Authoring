namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents a UDS Data Identifier (DID, Service 0x22 / 0x2E).
/// </summary>
public class DiagnosticDataIdentifier : ObservableModelBase
{
    private string _didHex = string.Empty;
    private int _didDecimal;
    private string _shortName = string.Empty;
    private string _longName = string.Empty;
    private string _dataCategory = "Current Data";
    private string _access = "Read";
    private int _byteLength = 1;
    private int _bitLength = 8;
    private string _dataType = string.Empty;
    private string _unit = string.Empty;
    private double _scalingFactor = 1.0;
    private double _offset;
    private double? _minPhysicalValue;
    private double? _maxPhysicalValue;
    private string _associatedService = string.Empty;
    private string _dopRef = string.Empty;
    private string _description = string.Empty;
    private string _definedInLayer = string.Empty;
    private bool _isInherited;

    public string DidHex { get => _didHex; set => SetProperty(ref _didHex, value); }
    public int DidDecimal { get => _didDecimal; set => SetProperty(ref _didDecimal, value); }
    public string ShortName { get => _shortName; set => SetProperty(ref _shortName, value); }
    public string LongName { get => _longName; set => SetProperty(ref _longName, value); }
    public string DataCategory { get => _dataCategory; set => SetProperty(ref _dataCategory, value); }
    public string Access { get => _access; set => SetProperty(ref _access, value); }
    public int ByteLength { get => _byteLength; set => SetProperty(ref _byteLength, value); }
    public int BitLength { get => _bitLength; set => SetProperty(ref _bitLength, value); }
    public string DataType { get => _dataType; set => SetProperty(ref _dataType, value); }
    public string Unit { get => _unit; set => SetProperty(ref _unit, value); }
    public double ScalingFactor { get => _scalingFactor; set => SetProperty(ref _scalingFactor, value); }
    public double Offset { get => _offset; set => SetProperty(ref _offset, value); }
    public double? MinPhysicalValue { get => _minPhysicalValue; set => SetProperty(ref _minPhysicalValue, value); }
    public double? MaxPhysicalValue { get => _maxPhysicalValue; set => SetProperty(ref _maxPhysicalValue, value); }
    public string AssociatedService { get => _associatedService; set => SetProperty(ref _associatedService, value); }
    public string DopRef { get => _dopRef; set => SetProperty(ref _dopRef, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }

    /// <summary>
    /// ISO 22901-1 layer that defined this DID.
    /// </summary>
    public string DefinedInLayer
    {
        get => _definedInLayer;
        set { if (SetProperty(ref _definedInLayer, value)) OnPropertyChanged(nameof(InheritanceScope)); }
    }

    /// <summary>
    /// True if inherited from a parent diagnostic layer.
    /// </summary>
    public bool IsInherited
    {
        get => _isInherited;
        set { if (SetProperty(ref _isInherited, value)) OnPropertyChanged(nameof(InheritanceScope)); }
    }

    public string InheritanceScope => IsInherited ? $"Inherited ({DefinedInLayer})" : "Own";

    public void ApplyDop(DataObjectProperty dop)
    {
        if (dop == null) return;
        DopRef = !string.IsNullOrEmpty(dop.Id) ? dop.Id : dop.ShortName;
        DataType = dop.BaseDataType;
        BitLength = dop.BitLength > 0 ? dop.BitLength : 8;
        ByteLength = BitLength > 0 ? (BitLength + 7) / 8 : 1;
        Unit = dop.PhysicalUnit;
        ScalingFactor = dop.Factor;
        Offset = dop.Offset;
        MinPhysicalValue = dop.MinPhysicalValue;
        MaxPhysicalValue = dop.MaxPhysicalValue;
    }

    public DiagnosticDataIdentifier CloneAsInherited(string originLayerName)
    {
        return new DiagnosticDataIdentifier
        {
            DidHex = DidHex,
            DidDecimal = DidDecimal,
            ShortName = ShortName,
            LongName = LongName,
            DataCategory = DataCategory,
            Access = Access,
            ByteLength = ByteLength,
            BitLength = BitLength,
            DataType = DataType,
            Unit = Unit,
            ScalingFactor = ScalingFactor,
            Offset = Offset,
            MinPhysicalValue = MinPhysicalValue,
            MaxPhysicalValue = MaxPhysicalValue,
            AssociatedService = AssociatedService,
            DopRef = DopRef,
            Description = Description,
            DefinedInLayer = string.IsNullOrEmpty(DefinedInLayer) ? originLayerName : DefinedInLayer,
            IsInherited = true
        };
    }
}
