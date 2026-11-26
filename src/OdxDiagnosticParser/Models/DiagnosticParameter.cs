namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents an input/output parameter in a diagnostic request or response.
/// </summary>
public class DiagnosticParameter : ObservableModelBase
{
    private string _name = string.Empty;
    private string _longName = string.Empty;
    private string _semantic = string.Empty;
    private string _parameterType = "VALUE";
    private int _bytePosition;
    private int _bitPosition;
    private int _bitLength;
    private string _defaultValueHex = string.Empty;
    private string _dataType = string.Empty;
    private string _dopRef = string.Empty;
    private string _description = string.Empty;

    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string LongName { get => _longName; set => SetProperty(ref _longName, value); }
    public string Semantic { get => _semantic; set => SetProperty(ref _semantic, value); }
    public string ParameterType { get => _parameterType; set => SetProperty(ref _parameterType, value); }
    public int BytePosition { get => _bytePosition; set => SetProperty(ref _bytePosition, value); }
    public int BitPosition { get => _bitPosition; set => SetProperty(ref _bitPosition, value); }
    public int BitLength { get => _bitLength; set => SetProperty(ref _bitLength, value); }
    public string DefaultValueHex { get => _defaultValueHex; set => SetProperty(ref _defaultValueHex, value); }
    public string DataType { get => _dataType; set => SetProperty(ref _dataType, value); }
    public string DopRef { get => _dopRef; set => SetProperty(ref _dopRef, value); }
    public string Description { get => _description; set => SetProperty(ref _description, value); }
}
