namespace OdxDiagnosticParser.Models;

/// <summary>
/// Represents a communication parameter (ComParam) defining bus timing or addressing.
/// </summary>
public class CommunicationParameter
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

