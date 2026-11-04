using System.Xml.Linq;

namespace OdxDiagnosticParser.Parsers;

/// <summary>
/// XML helper extensions that allow namespace-agnostic querying of ODX elements.
/// </summary>
public static class OdxXmlExtensions
{
    public static XElement? El(this XContainer? container, string localName)
    {
        if (container == null) return null;
        return container.Elements().FirstOrDefault(e =>
            string.Equals(e.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<XElement> Els(this XContainer? container, string localName)
    {
        if (container == null) return Enumerable.Empty<XElement>();
        return container.Elements().Where(e =>
            string.Equals(e.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<XElement> Desc(this XContainer? container, string localName)
    {
        if (container == null) return Enumerable.Empty<XElement>();
        return container.Descendants().Where(e =>
            string.Equals(e.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));
    }

    public static string Val(this XElement? element)
    {
        return element?.Value?.Trim() ?? string.Empty;
    }

    public static string Attr(this XElement? element, string attributeName)
    {
        if (element == null) return string.Empty;
        var attr = element.Attributes().FirstOrDefault(a =>
            string.Equals(a.Name.LocalName, attributeName, StringComparison.OrdinalIgnoreCase));
        return attr?.Value?.Trim() ?? string.Empty;
    }
}

