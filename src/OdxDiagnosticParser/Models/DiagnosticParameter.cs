namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents an input/output parameter in a diagnostic request or response.
/// </summary>
public class DiagnosticParameter
{
    public string Name { get; set; } = string.Empty;
    public string LongName { get; set; } = string.Empty;
    public string Semantic { get; set; } = string.Empty;
    public string ParameterType { get; set; } = "VALUE"; // VALUE, CODED-CONST, MATCHING-REQUEST-PARAM, TABLE-KEY, etc.
    public int BytePosition { get; set; }
    public int BitPosition { get; set; }
    public int BitLength { get; set; }
    public string DefaultValueHex { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string DopRef { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

