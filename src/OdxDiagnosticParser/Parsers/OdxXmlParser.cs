using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using OdxDiagnosticParser.Models;

namespace OdxDiagnosticParser.Parsers;

/// <summary>
/// High-performance, schema-resilient parser for ODX XML (ODX 2.0.1, 2.2.0, ISO 22901-1).
/// Parses ODX-D, ODX-C, ODX-V, ODX-E, and resolves PARENT-REFS, COMPARAM-SPEC, UNIT-REF, DOP-REF, and TABLE-ROWs.
/// </summary>
public class OdxXmlParser : IOdxParser
{
    private static readonly XmlReaderSettings SafeReaderSettings = new()
    {
        DtdProcessing = DtdProcessing.Ignore,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true
    };

    public DiagnosticPackage ParseFile(string filePath)
    {
        var pkg = ParseFileWithoutLinking(filePath);
        OdxIsoLinker.LinkPackage(pkg);
        return pkg;
    }

    internal DiagnosticPackage ParseFileWithoutLinking(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"ODX file not found: {filePath}", filePath);

        // Auto-detect if user passed a PDX or ZIP archive instead of raw XML
        if (IsZipArchive(filePath))
        {
            var extractor = new PdxPackageExtractor(this);
            return extractor.ExtractAndParse(filePath);
        }

        using var stream = File.OpenRead(filePath);
        var pkg = ParseStreamWithoutLinking(stream, Path.GetFileName(filePath));
        pkg.SourcePath = filePath;
        return pkg;
    }

    public DiagnosticPackage ParseXml(string xmlContent, string sourceName = "InlineXml")
    {
        var pkg = ParseXmlWithoutLinking(xmlContent, sourceName);
        OdxIsoLinker.LinkPackage(pkg);
        return pkg;
    }

    internal DiagnosticPackage ParseXmlWithoutLinking(string xmlContent, string sourceName = "InlineXml")
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentException("XML content cannot be null or empty.", nameof(xmlContent));

        int xmlStart = xmlContent.IndexOf('<');
        if (xmlStart > 0)
        {
            xmlContent = xmlContent.Substring(xmlStart);
        }

        using var stringReader = new StringReader(xmlContent);
        using var xmlReader = XmlReader.Create(stringReader, SafeReaderSettings);
        var doc = XDocument.Load(xmlReader);
        return ParseXDocument(doc, sourceName);
    }

    public DiagnosticPackage ParseStream(Stream stream, string sourceName = "Stream")
    {
        var pkg = ParseStreamWithoutLinking(stream, sourceName);
        OdxIsoLinker.LinkPackage(pkg);
        return pkg;
    }

    private DiagnosticPackage ParseStreamWithoutLinking(Stream stream, string sourceName = "Stream")
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();

        if (bytes.Length == 0)
            throw new ArgumentException("The stream is empty.", nameof(stream));

        if (bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B &&
            (bytes[2] == 0x03 || bytes[2] == 0x05 || bytes[2] == 0x07))
        {
            var tempPdx = Path.Combine(Path.GetTempPath(), $"OdxAutoExtract_{Guid.NewGuid():N}.pdx");
            try
            {
                File.WriteAllBytes(tempPdx, bytes);
                var extractor = new PdxPackageExtractor(this);
                return extractor.ExtractAndParse(tempPdx);
            }
            finally
            {
                try { if (File.Exists(tempPdx)) File.Delete(tempPdx); } catch { }
            }
        }

        string xmlContent = DecodeXmlBytes(bytes);
        return ParseXmlWithoutLinking(xmlContent, sourceName);
    }

    private static bool IsZipArchive(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext is ".pdx" or ".zip") return true;

        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length >= 4)
            {
                var header = new byte[4];
                int read = fs.Read(header, 0, 4);
                return read >= 4 && header[0] == 0x50 && header[1] == 0x4B &&
                       (header[2] == 0x03 || header[2] == 0x05 || header[2] == 0x07);
            }
        }
        catch { }

        return false;
    }

    private static string DecodeXmlBytes(byte[] bytes)
    {
        if (bytes.Length == 0)
            throw new ArgumentException("XML content is empty.");

        var encoding = System.Text.Encoding.UTF8;
        if (bytes.Length >= 2)
        {
            if (bytes[0] == 0xFF && bytes[1] == 0xFE)
                encoding = System.Text.Encoding.Unicode;
            else if (bytes[0] == 0xFE && bytes[1] == 0xFF)
                encoding = System.Text.Encoding.BigEndianUnicode;
            else if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                encoding = System.Text.Encoding.UTF8;
        }

        string text = encoding.GetString(bytes);

        if (text.Contains("encoding=\"ISO-8859-1\"", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("encoding='ISO-8859-1'", StringComparison.OrdinalIgnoreCase))
        {
            try { text = System.Text.Encoding.GetEncoding("ISO-8859-1").GetString(bytes); } catch { }
        }
        else if (text.Contains("encoding=\"windows-1252\"", StringComparison.OrdinalIgnoreCase) ||
                 text.Contains("encoding='windows-1252'", StringComparison.OrdinalIgnoreCase))
        {
            try { text = System.Text.Encoding.GetEncoding(1252).GetString(bytes); } catch { }
        }

        int xmlStart = text.IndexOf('<');
        if (xmlStart > 0)
        {
            text = text.Substring(xmlStart);
        }
        else if (xmlStart < 0)
        {
            throw new XmlException($"The content does not contain valid XML markup. Leading characters: '{(text.Length > 30 ? text[..30] : text)}'");
        }

        return text.TrimEnd('\0', ' ', '\r', '\n', '\t');
    }

    private DiagnosticPackage ParseXDocument(XDocument doc, string sourceName)
    {
        var root = doc.Root;
        var pkg = new DiagnosticPackage
        {
            Name = Path.GetFileNameWithoutExtension(sourceName),
            SourcePath = sourceName,
            IsFromPdx = false,
            ExtractedAt = DateTime.UtcNow
        };

        if (root == null)
            return pkg;

        pkg.Version = root.Attr("MODEL-VERSION");
        if (string.IsNullOrWhiteSpace(pkg.Version))
            pkg.Version = "2.2.0";

        // 1. Parse ISO 22901-1 ODX-C COMPARAM-SPEC (if present)
        ParseComParamSpecs(root, sourceName, pkg);

        // 2. Parse ISO 22901-1 ODX-V VEHICLE-INFO-SPEC (if present)
        ParseVehicleInfoSpecs(root, sourceName, pkg);

        // Global dictionaries for cross-referencing ID-REFs within document
        var globalUnits = new Dictionary<string, PhysicalUnitInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var uEl in root.Desc("UNIT"))
        {
            var u = ParseUnitElement(uEl);
            if (!string.IsNullOrEmpty(u.Id) && !globalUnits.ContainsKey(u.Id))
                globalUnits[u.Id] = u;
        }

        var globalRequests = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
        var globalPosResponses = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
        var globalNegResponses = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
        var globalDops = new Dictionary<string, DataObjectProperty>(StringComparer.OrdinalIgnoreCase);
        var globalFunctClasses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fcEl in root.Desc("FUNCT-CLASS"))
        {
            var id = fcEl.Attr("ID");
            var sn = fcEl.El("SHORT-NAME").Val();
            if (!string.IsNullOrEmpty(id))
                globalFunctClasses[id] = !string.IsNullOrEmpty(sn) ? sn : id;
        }

        foreach (var reqEl in root.Desc("REQUEST"))
        {
            var id = reqEl.Attr("ID");
            if (!string.IsNullOrEmpty(id) && !globalRequests.ContainsKey(id))
                globalRequests[id] = reqEl;
        }

        foreach (var posEl in root.Desc("POS-RESPONSE"))
        {
            var id = posEl.Attr("ID");
            if (!string.IsNullOrEmpty(id) && !globalPosResponses.ContainsKey(id))
                globalPosResponses[id] = posEl;
        }

        foreach (var negEl in root.Desc("NEG-RESPONSE"))
        {
            var id = negEl.Attr("ID");
            if (!string.IsNullOrEmpty(id) && !globalNegResponses.ContainsKey(id))
                globalNegResponses[id] = negEl;
        }

        foreach (var dopEl in root.Desc("DATA-OBJECT-PROP"))
        {
            var dop = ParseDataObjectProperty(dopEl, globalUnits);
            if (!string.IsNullOrEmpty(dop.Id) && !globalDops.ContainsKey(dop.Id))
                globalDops[dop.Id] = dop;
        }

        // Collect layers and record their enclosing DIAG-LAYER-CONTAINER SHORT-NAME (ISO DOCREF)
        var layerNodes = new List<(XElement Element, DiagnosticLayerType Type, string ContainerShortName)>();

        void AddLayerType(string elementName, DiagnosticLayerType layerType)
        {
            foreach (var el in root.Desc(elementName))
            {
                var containerEl = el.Ancestors().FirstOrDefault(a =>
                    string.Equals(a.Name.LocalName, "DIAG-LAYER-CONTAINER", StringComparison.OrdinalIgnoreCase));
                var containerName = containerEl?.El("SHORT-NAME")?.Val() ?? Path.GetFileNameWithoutExtension(sourceName);
                layerNodes.Add((el, layerType, containerName));
            }
        }

        AddLayerType("PROTOCOL", DiagnosticLayerType.Protocol);
        AddLayerType("FUNCTIONAL-GROUP", DiagnosticLayerType.FunctionalGroup);
        AddLayerType("ECU-SHARED-DATA", DiagnosticLayerType.EcuSharedData);
        AddLayerType("BASE-VARIANT", DiagnosticLayerType.BaseVariant);
        AddLayerType("ECU-VARIANT", DiagnosticLayerType.EcuVariant);

        // Fallback only if XML contains DIAG-SERVICE or DIAG-LAYER without typed layer elements
        if (layerNodes.Count == 0 && !root.Desc("COMPARAM-SPEC").Any() && !root.Desc("VEHICLE-INFO-SPEC").Any())
        {
            var diagLayer = root.Desc("DIAG-LAYER").FirstOrDefault();
            if (diagLayer != null || root.Desc("DIAG-SERVICE").Any())
            {
                layerNodes.Add((diagLayer ?? root, DiagnosticLayerType.EcuVariant, Path.GetFileNameWithoutExtension(sourceName)));
            }
        }

        foreach (var (layerEl, layerType, containerName) in layerNodes)
        {
            var layer = ParseDiagnosticLayer(
                layerEl, layerType, containerName, sourceName, pkg.Version,
                globalRequests, globalPosResponses, globalNegResponses, globalDops, globalUnits, globalFunctClasses);
            pkg.Layers.Add(layer);
        }

        if (pkg.Layers.Count > 0)
        {
            var primaryLayer = pkg.Layers.FirstOrDefault(l => l.LayerType == DiagnosticLayerType.EcuVariant)
                               ?? pkg.Layers.FirstOrDefault(l => l.LayerType == DiagnosticLayerType.BaseVariant)
                               ?? pkg.Layers[0];
            pkg.Name = !string.IsNullOrWhiteSpace(primaryLayer.ShortName) ? primaryLayer.ShortName : pkg.Name;
            pkg.Description = primaryLayer.Description;
        }
        else if (pkg.VehicleInfos.Count > 0)
        {
            pkg.Name = pkg.VehicleInfos[0].ShortName;
        }
        else if (pkg.ComParamSpecs.Count > 0)
        {
            pkg.Name = pkg.ComParamSpecs[0].ShortName;
        }

        return pkg;
    }

    private void ParseComParamSpecs(XElement root, string sourceName, DiagnosticPackage pkg)
    {
        foreach (var cpsEl in root.Desc("COMPARAM-SPEC"))
        {
            var spec = new ComParamSpecInfo
            {
                Id = cpsEl.Attr("ID"),
                ShortName = cpsEl.El("SHORT-NAME").Val(),
                LongName = cpsEl.El("LONG-NAME").Val(),
                Description = ExtractDescription(cpsEl),
                SourceFileName = sourceName
            };

            if (string.IsNullOrWhiteSpace(spec.ShortName))
                spec.ShortName = spec.Id;

            foreach (var psEl in cpsEl.Desc("PROT-STACK"))
            {
                var stack = new ProtStackInfo
                {
                    Id = psEl.Attr("ID"),
                    ShortName = psEl.El("SHORT-NAME").Val(),
                    LongName = psEl.El("LONG-NAME").Val(),
                    PduProtocolType = psEl.El("PDU-PROTOCOL-TYPE").Val(),
                    PhysicalLinkType = psEl.El("PHYSICAL-LINK-TYPE").Val()
                };
                foreach (var ssRef in psEl.Desc("COMPARAM-SUBSET-REF"))
                {
                    var r = ssRef.Attr("ID-REF");
                    if (!string.IsNullOrEmpty(r))
                        stack.ComParamSubsetRefs.Add(r);
                }
                spec.ProtStacks.Add(stack);
            }

            // Parse COMPARAM definitions inside COMPARAM-SUBSET
            foreach (var cpEl in cpsEl.Desc("COMPARAM"))
            {
                var cpName = cpEl.El("SHORT-NAME").Val();
                var cpVal = cpEl.El("PHYSICAL-DEFAULT-VALUE")?.Val();
                if (string.IsNullOrEmpty(cpVal)) cpVal = cpEl.El("VALUE")?.Val();
                if (string.IsNullOrEmpty(cpVal)) cpVal = cpEl.El("SIMPLE-VALUE")?.Val();

                if (!string.IsNullOrEmpty(cpName))
                {
                    spec.DefaultComParams.Add(new CommunicationParameter
                    {
                        Name = cpName,
                        Value = cpVal ?? string.Empty,
                        ParamClass = cpEl.Attr("PARAM-CLASS"),
                        Protocol = spec.ShortName,
                        DefinedInLayer = spec.ShortName,
                        Description = ExtractDescription(cpEl)
                    });
                }
            }

            pkg.ComParamSpecs.Add(spec);
        }
    }

    private void ParseVehicleInfoSpecs(XElement root, string sourceName, DiagnosticPackage pkg)
    {
        foreach (var visEl in root.Desc("VEHICLE-INFO-SPEC"))
        {
            var infoNodes = visEl.Desc("VEHICLE-INFO").Concat(visEl.Desc("VEHICLE-INFORMATION")).ToList();
            if (infoNodes.Count == 0) infoNodes.Add(visEl);

            foreach (var vEl in infoNodes)
            {
                var vi = new VehicleInfoSpec
                {
                    Id = vEl.Attr("ID"),
                    ShortName = vEl.El("SHORT-NAME").Val(),
                    LongName = vEl.El("LONG-NAME").Val(),
                    Model = vEl.El("MODEL")?.Val() ?? vEl.El("VEHICLE-MODEL")?.Val() ?? vEl.El("INFO-COMPONENT")?.Val() ?? vEl.El("LONG-NAME").Val(),
                    Company = vEl.El("COMPANY")?.Val() is { Length: > 0 } compVal
                        ? compVal
                        : (root.Desc("COMPANY-DATA").FirstOrDefault()?.El("SHORT-NAME").Val() ?? string.Empty),
                    ModelYear = vEl.El("MODEL-YEAR")?.Val() ?? string.Empty,
                    SourceFileName = sourceName
                };

                if (string.IsNullOrEmpty(vi.ShortName))
                    vi.ShortName = visEl.El("SHORT-NAME").Val();

                foreach (var pvlEl in vEl.Desc("PHYSICAL-VEHICLE-LINK"))
                {
                    vi.PhysicalVehicleLinks.Add(new PhysicalVehicleLink
                    {
                        Id = pvlEl.Attr("ID"),
                        ShortName = pvlEl.El("SHORT-NAME").Val(),
                        LongName = pvlEl.El("LONG-NAME").Val(),
                        LinkType = !string.IsNullOrEmpty(pvlEl.Attr("TYPE")) ? pvlEl.Attr("TYPE") : pvlEl.El("LINK-TYPE").Val(),
                        Description = ExtractDescription(pvlEl)
                    });
                }

                // Collect GATEWAY-LOGICAL-LINK, MEMBER-LOGICAL-LINK, FUNCTIONAL-GROUP-LOGICAL-LINK, LOGICAL-LINK
                var linkElements = vEl.Desc("LOGICAL-LINKS").Elements();
                foreach (var llEl in linkElements)
                {
                    var localName = llEl.Name.LocalName.ToUpperInvariant();
                    if (!localName.Contains("LOGICAL-LINK")) continue;

                    var link = new LogicalLink
                    {
                        Id = llEl.Attr("ID"),
                        ShortName = llEl.El("SHORT-NAME").Val(),
                        LongName = llEl.El("LONG-NAME").Val(),
                        LinkRole = llEl.Name.LocalName,
                        PhysicalVehicleLinkRef = llEl.El("PHYSICAL-VEHICLE-LINK-REF")?.Attr("ID-REF") ?? string.Empty,
                        ProtocolRef = llEl.El("PROTOCOL-REF")?.Attr("ID-REF") ?? llEl.El("PROTOCOL-SNREF")?.Attr("SHORT-NAME") ?? string.Empty,
                        ProtocolDocRef = llEl.El("PROTOCOL-REF")?.Attr("DOCREF") ?? string.Empty,
                        BaseVariantRef = llEl.El("BASE-VARIANT-REF")?.Attr("ID-REF") ?? llEl.El("BASE-VARIANT-SNREF")?.Attr("SHORT-NAME") ?? string.Empty,
                        BaseVariantDocRef = llEl.El("BASE-VARIANT-REF")?.Attr("DOCREF") ?? string.Empty,
                        FunctionalGroupRef = llEl.El("FUNCTIONAL-GROUP-REF")?.Attr("ID-REF") ?? llEl.El("FUNCTIONAL-GROUP-SNREF")?.Attr("SHORT-NAME") ?? string.Empty,
                        FunctionalGroupDocRef = llEl.El("FUNCTIONAL-GROUP-REF")?.Attr("DOCREF") ?? string.Empty
                    };

                    var cpElements = llEl.Desc("LINK-COMPARAM-REF").Concat(llEl.Desc("COMPARAM-REF"));
                    foreach (var cpEl in cpElements)
                    {
                        var cp = ParseSingleComParamRef(cpEl, vi.ShortName);
                        if (!string.IsNullOrEmpty(cp.Name))
                        {
                            link.LinkComParams.Add(cp);

                            var upperName = cp.Name.ToUpperInvariant();
                            if (upperName.Contains("PHYSREQ") || upperName.Contains("CANIDTX") || upperName == "CP_CANID" || upperName.Contains("REQUESTCANID"))
                                link.PhysicalRequestId = cp.Value;
                            else if (upperName.Contains("PHYSRESP") || upperName.Contains("CANIDRX") || upperName.Contains("RESPONSECANID"))
                                link.PhysicalResponseId = cp.Value;
                            else if (upperName.Contains("FUNCREQ") || upperName.Contains("FUNCTIONALREQUEST"))
                                link.FunctionalRequestId = cp.Value;
                        }
                    }

                    vi.LogicalLinks.Add(link);
                }

                pkg.VehicleInfos.Add(vi);
            }
        }
    }

    private DiagnosticLayer ParseDiagnosticLayer(
        XElement layerEl,
        DiagnosticLayerType layerType,
        string containerShortName,
        string sourceFileName,
        string defaultModelVersion,
        Dictionary<string, XElement> globalRequests,
        Dictionary<string, XElement> globalPosResponses,
        Dictionary<string, XElement> globalNegResponses,
        Dictionary<string, DataObjectProperty> globalDops,
        Dictionary<string, PhysicalUnitInfo> globalUnits,
        Dictionary<string, string> globalFunctClasses)
    {
        var layer = new DiagnosticLayer
        {
            LayerType = layerType,
            Id = layerEl.Attr("ID"),
            ShortName = layerEl.El("SHORT-NAME").Val(),
            LongName = layerEl.El("LONG-NAME").Val(),
            Description = ExtractDescription(layerEl),
            ModelVersion = layerEl.Attr("MODEL-VERSION") is { Length: > 0 } v ? v : defaultModelVersion,
            SourceFileName = sourceFileName,
            ContainerShortName = containerShortName
        };

        if (string.IsNullOrWhiteSpace(layer.ShortName))
            layer.ShortName = layerEl.Attr("ID");

        // Parse local UNIT-SPEC
        var localUnits = new Dictionary<string, PhysicalUnitInfo>(globalUnits, StringComparer.OrdinalIgnoreCase);
        foreach (var uEl in layerEl.Desc("UNIT"))
        {
            var u = ParseUnitElement(uEl);
            if (!string.IsNullOrEmpty(u.Id))
                localUnits[u.Id] = u;
            layer.Units.Add(u);
        }

        // Protocol -> ODX-C ComParamSpec link
        if (layerType == DiagnosticLayerType.Protocol)
        {
            var cpsRef = layerEl.El("COMPARAM-SPEC-REF");
            if (cpsRef != null)
            {
                layer.ComParamSpecRef = cpsRef.Attr("ID-REF");
                layer.ComParamSpecDocRef = cpsRef.Attr("DOCREF");
            }
            layer.ProtStackShortName = layerEl.El("PROT-STACK-SNREF")?.Attr("SHORT-NAME") ?? string.Empty;
        }

        // ISO 22901-1 PARENT-REFS (Layer inheritance connections)
        ParseParentRefs(layerEl, layer);

        // ECU-VARIANT-PATTERNS (Variant matching identification)
        ParseVariantPatterns(layerEl, layer);

        // Local DOPs
        var localDops = new Dictionary<string, DataObjectProperty>(globalDops, StringComparer.OrdinalIgnoreCase);
        foreach (var dopEl in layerEl.Desc("DATA-OBJECT-PROP"))
        {
            var dop = ParseDataObjectProperty(dopEl, localUnits);
            dop.DefinedInLayer = layer.ShortName;
            dop.IsInherited = false;
            if (!string.IsNullOrEmpty(dop.Id))
                localDops[dop.Id] = dop;
            layer.DataObjectProperties.Add(dop);
        }

        // ODX Tables / TableRows (used by TABLE-KEY / DIDs)
        ParseTables(layerEl, layer);

        // DTCs
        ParseDiagnosticTroubleCodes(layerEl, layer);

        // Services (DIAG-SERVICES and DIAG-COMMS)
        ParseDiagnosticServices(layerEl, layer, localDops, globalRequests, globalPosResponses, globalNegResponses, globalFunctClasses);

        // DIDs (synthesized from 0x22 / 0x2E services and TABLE-ROW definitions)
        ResolveDataIdentifiers(layer, localDops);

        // Communication Parameters
        ParseCommunicationParameters(layerEl, layer);

        return layer;
    }

    private void ParseParentRefs(XElement layerEl, DiagnosticLayer layer)
    {
        var parentRefsEl = layerEl.El("PARENT-REFS");
        if (parentRefsEl == null) return;

        foreach (var pRefEl in parentRefsEl.Elements())
        {
            if (!pRefEl.Name.LocalName.Contains("PARENT-REF", StringComparison.OrdinalIgnoreCase) &&
                !pRefEl.Name.LocalName.EndsWith("-REF", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var xsiType = pRefEl.Attr("type");
            if (string.IsNullOrEmpty(xsiType))
                xsiType = pRefEl.Name.LocalName;

            var pref = new LayerParentRef
            {
                IdRef = pRefEl.Attr("ID-REF"),
                ShortNameRef = pRefEl.Attr("SHORT-NAME-REF") is { Length: > 0 } sn ? sn : pRefEl.El("SHORT-NAME").Val(),
                DocRef = pRefEl.Attr("DOCREF"),
                DocType = !string.IsNullOrEmpty(pRefEl.Attr("DOCTYPE")) ? pRefEl.Attr("DOCTYPE") : "CONTAINER",
                ParentRefType = xsiType
            };

            foreach (var excluded in pRefEl.Desc("NOT-INHERITED-DIAG-COMM"))
            {
                var target = excluded.El("DIAG-COMM-SNREF")?.Attr("SHORT-NAME") ?? excluded.El("SHORT-NAME").Val();
                if (!string.IsNullOrEmpty(target))
                    pref.NotInheritedDiagComms.Add(target);
            }

            foreach (var excludedDop in pRefEl.Desc("NOT-INHERITED-DOP"))
            {
                var target = excludedDop.El("DOP-BASE-SNREF")?.Attr("SHORT-NAME") ?? excludedDop.El("SHORT-NAME").Val();
                if (!string.IsNullOrEmpty(target))
                    pref.NotInheritedDops.Add(target);
            }

            foreach (var excludedTbl in pRefEl.Desc("NOT-INHERITED-TABLE"))
            {
                var target = excludedTbl.El("TABLE-SNREF")?.Attr("SHORT-NAME") ?? excludedTbl.El("SHORT-NAME").Val();
                if (!string.IsNullOrEmpty(target))
                    pref.NotInheritedTables.Add(target);
            }

            if (!string.IsNullOrEmpty(pref.IdRef) || !string.IsNullOrEmpty(pref.ShortNameRef))
                layer.ParentRefs.Add(pref);
        }
    }

    private static void ParseVariantPatterns(XElement layerEl, DiagnosticLayer layer)
    {
        foreach (var patEl in layerEl.Desc("ECU-VARIANT-PATTERN"))
        {
            var pattern = new EcuVariantPattern
            {
                Name = patEl.El("SHORT-NAME").Val() is { Length: > 0 } sn ? sn : $"Pattern_{layer.VariantPatterns.Count + 1}"
            };

            foreach (var mpEl in patEl.Desc("MATCHING-PARAMETER"))
            {
                pattern.MatchingParameters.Add(new MatchingParameterInfo
                {
                    DiagCommShortName = mpEl.El("DIAG-COMM-SNREF")?.Attr("SHORT-NAME") ?? string.Empty,
                    OutParamShortName = mpEl.El("OUT-PARAM-IF-SNREF")?.Attr("SHORT-NAME") ?? string.Empty,
                    ExpectedValue = mpEl.El("EXPECTED-VALUE").Val()
                });
            }

            layer.VariantPatterns.Add(pattern);
        }
    }

    private static void ParseTables(XElement layerEl, DiagnosticLayer layer)
    {
        foreach (var tableEl in layerEl.Desc("TABLE"))
        {
            var tableId = tableEl.Attr("ID");
            var tableName = tableEl.El("SHORT-NAME").Val();
            var semantic = tableEl.Attr("SEMANTIC");

            foreach (var rowEl in tableEl.Desc("TABLE-ROW"))
            {
                var keyVal = rowEl.El("KEY").Val();
                var dopRef = rowEl.El("DATA-OBJECT-PROP-REF")?.Attr("ID-REF")
                             ?? rowEl.El("STRUCTURE-REF")?.Attr("ID-REF")
                             ?? string.Empty;

                layer.TableRows.Add(new OdxTableRowInfo
                {
                    TableId = tableId,
                    TableShortName = tableName,
                    RowId = rowEl.Attr("ID"),
                    ShortName = rowEl.El("SHORT-NAME").Val(),
                    LongName = rowEl.El("LONG-NAME").Val(),
                    KeyHex = !string.IsNullOrEmpty(keyVal) ? NormalizeHex(keyVal) : string.Empty,
                    Semantic = !string.IsNullOrEmpty(rowEl.Attr("SEMANTIC")) ? rowEl.Attr("SEMANTIC") : semantic,
                    DopRef = dopRef,
                    Description = ExtractDescription(rowEl)
                });
            }
        }
    }

    private void ParseDiagnosticServices(
        XElement layerEl,
        DiagnosticLayer layer,
        Dictionary<string, DataObjectProperty> dops,
        Dictionary<string, XElement> globalRequests,
        Dictionary<string, XElement> globalPosResponses,
        Dictionary<string, XElement> globalNegResponses,
        Dictionary<string, string> globalFunctClasses)
    {
        var serviceElements = layerEl.Desc("DIAG-SERVICE");
        foreach (var sEl in serviceElements)
        {
            var addressing = sEl.Attr("ADDRESSING");
            var service = new DiagnosticService
            {
                Id = sEl.Attr("ID"),
                ShortName = sEl.El("SHORT-NAME").Val(),
                LongName = sEl.El("LONG-NAME").Val(),
                Description = ExtractDescription(sEl),
                Semantic = sEl.Attr("SEMANTIC"),
                Addressing = NormalizeAddressing(addressing),
                DefinedInLayer = layer.ShortName,
                DefinedInLayerType = layer.LayerType.ToString(),
                IsInherited = false
            };

            if (string.IsNullOrWhiteSpace(service.Semantic))
                service.Semantic = sEl.El("SEMANTIC").Val();

            foreach (var fcRef in sEl.Desc("FUNCT-CLASS-REF"))
            {
                var refId = fcRef.Attr("ID-REF");
                if (!string.IsNullOrEmpty(refId))
                    service.FunctionalClasses.Add(globalFunctClasses.TryGetValue(refId, out var fcName) ? fcName : refId);
            }

            // Resolve Request
            XElement? requestEl = sEl.El("REQUEST");
            if (requestEl == null)
            {
                var reqRef = sEl.El("REQUEST-REF")?.Attr("ID-REF");
                if (!string.IsNullOrEmpty(reqRef) && globalRequests.TryGetValue(reqRef, out var req))
                    requestEl = req;
            }

            if (requestEl != null)
            {
                service.RequestParameters = ParseParameters(requestEl, dops);

                var sidParam = service.RequestParameters.FirstOrDefault(p =>
                    string.Equals(p.Semantic, "SERVICE-ID", StringComparison.OrdinalIgnoreCase) ||
                    p.BytePosition == 0 && !string.IsNullOrEmpty(p.DefaultValueHex));

                if (sidParam != null)
                    service.ServiceIdHex = NormalizeHex(sidParam.DefaultValueHex);

                var subParam = service.RequestParameters.FirstOrDefault(p =>
                    string.Equals(p.Semantic, "SUBFUNCTION", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Semantic, "SUB-FUNCTION", StringComparison.OrdinalIgnoreCase) ||
                    p.BytePosition == 1 && IsSubFunctionService(service.ServiceIdHex));

                if (subParam != null)
                    service.SubFunctionHex = NormalizeHex(subParam.DefaultValueHex);
            }

            if (string.IsNullOrEmpty(service.ServiceIdHex))
            {
                service.ServiceIdHex = InferServiceId(service.ShortName, service.Semantic);
            }

            // Resolve Positive Response (supports <POS-RESPONSE>, <POS-RESPONSES>/<POS-RESPONSE-REF>, <POS-RESPONSE-REFS>/<POS-RESPONSE-REF>)
            var posRespEl = sEl.El("POS-RESPONSES")?.El("POS-RESPONSE") ?? sEl.El("POS-RESPONSE");
            if (posRespEl == null)
            {
                var posRef = sEl.Desc("POS-RESPONSE-REF").FirstOrDefault()?.Attr("ID-REF");
                if (!string.IsNullOrEmpty(posRef) && globalPosResponses.TryGetValue(posRef, out var pos))
                    posRespEl = pos;
            }

            if (posRespEl != null)
            {
                service.ResponseParameters = ParseParameters(posRespEl, dops);
                var respSidParam = service.ResponseParameters.FirstOrDefault(p =>
                    string.Equals(p.Semantic, "SERVICE-ID", StringComparison.OrdinalIgnoreCase) ||
                    p.BytePosition == 0 && !string.IsNullOrEmpty(p.DefaultValueHex));

                if (respSidParam != null)
                {
                    service.PositiveResponseSidHex = NormalizeHex(respSidParam.DefaultValueHex);
                }
                else if (!string.IsNullOrEmpty(service.ServiceIdHex) &&
                         int.TryParse(service.ServiceIdHex.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var sidNum))
                {
                    service.PositiveResponseSidHex = $"0x{(sidNum + 0x40):X2}";
                }
            }
            else if (!string.IsNullOrEmpty(service.ServiceIdHex) &&
                     int.TryParse(service.ServiceIdHex.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var sidNumFallback))
            {
                service.PositiveResponseSidHex = $"0x{(sidNumFallback + 0x40):X2}";
            }

            // Resolve Negative Responses
            var negRefs = sEl.Desc("NEG-RESPONSE-REF").Select(r => r.Attr("ID-REF")).Where(r => !string.IsNullOrEmpty(r)).ToList();
            foreach (var nRef in negRefs)
            {
                if (globalNegResponses.TryGetValue(nRef, out var negEl))
                {
                    AddParsedNegativeResponse(service, negEl);
                }
                else
                {
                    service.NegativeResponses.Add(nRef);
                }
            }
            if (negRefs.Count == 0)
            {
                var inlineNegs = sEl.El("NEG-RESPONSES")?.Elements().Where(e => e.Name.LocalName == "NEG-RESPONSE")
                                 ?? Enumerable.Empty<XElement>();
                foreach (var inlineNeg in inlineNegs)
                    AddParsedNegativeResponse(service, inlineNeg);
            }

            service.DataCategory = CategorizeService(service.ServiceIdHex, service.Semantic, service.ShortName, service.Description);
            layer.Services.Add(service);
        }
    }

    private void AddParsedNegativeResponse(DiagnosticService service, XElement negEl)
    {
        var nrcName = negEl.El("SHORT-NAME").Val();
        var nrcParam = negEl.Desc("PARAM").FirstOrDefault(p => string.Equals(p.Attr("SEMANTIC"), "NRC", StringComparison.OrdinalIgnoreCase));
        var nrcCode = nrcParam?.El("CODED-VALUE").Val() ?? nrcParam?.El("DEFAULT-VALUE").Val() ?? "";
        if (!string.IsNullOrEmpty(nrcCode))
        {
            var normalizedCode = NormalizeHex(nrcCode);
            var isAutoPlaceholder = string.Equals(nrcName, $"NRC_{normalizedCode}", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(nrcName, $"NRC_{normalizedCode.Replace("0x", "")}", StringComparison.OrdinalIgnoreCase);
            service.NegativeResponses.Add(isAutoPlaceholder || string.IsNullOrWhiteSpace(nrcName)
                ? normalizedCode
                : $"{normalizedCode} {nrcName}".Trim());
        }
        else if (!string.IsNullOrEmpty(nrcName))
        {
            service.NegativeResponses.Add(nrcName);
        }
    }

    private List<DiagnosticParameter> ParseParameters(XElement parentEl, Dictionary<string, DataObjectProperty> dops)
    {
        var list = new List<DiagnosticParameter>();
        var paramNodes = parentEl.El("PARAMS")?.Els("PARAM") ?? parentEl.Els("PARAM");

        foreach (var pEl in paramNodes)
        {
            var param = new DiagnosticParameter
            {
                Name = pEl.El("SHORT-NAME").Val(),
                LongName = pEl.El("LONG-NAME").Val(),
                Semantic = pEl.Attr("SEMANTIC"),
                ParameterType = pEl.Attr("TYPE"),
                Description = ExtractDescription(pEl)
            };

            if (string.IsNullOrWhiteSpace(param.Semantic))
                param.Semantic = pEl.El("SEMANTIC").Val();

            if (int.TryParse(pEl.El("BYTE-POSITION").Val(), out var bp))
                param.BytePosition = bp;

            if (int.TryParse(pEl.El("BIT-POSITION").Val(), out var bip))
                param.BitPosition = bip;

            if (int.TryParse(pEl.El("BIT-LENGTH").Val(), out var bl))
                param.BitLength = bl;

            var codedVal = pEl.El("CODED-VALUE").Val();
            if (string.IsNullOrEmpty(codedVal))
                codedVal = pEl.El("DEFAULT-VALUE").Val();
            if (string.IsNullOrEmpty(codedVal))
                codedVal = pEl.El("BYTE-VALUE").Val();
            if (string.IsNullOrEmpty(codedVal))
                codedVal = pEl.El("PHYSICAL-DEFAULT-VALUE").Val();

            param.DefaultValueHex = codedVal;

            var dopRef = pEl.El("DOP-REF")?.Attr("ID-REF");
            if (string.IsNullOrEmpty(dopRef))
                dopRef = pEl.El("DOP-SNREF")?.Attr("SHORT-NAME");
            if (string.IsNullOrEmpty(dopRef))
                dopRef = pEl.Attr("DOP-REF");

            param.DopRef = dopRef ?? string.Empty;

            if (!string.IsNullOrEmpty(param.DopRef) && dops.TryGetValue(param.DopRef, out var dop))
            {
                param.DataType = dop.BaseDataType;
                if (param.BitLength == 0)
                    param.BitLength = dop.BitLength;
            }

            list.Add(param);
        }

        return list;
    }

    private void ParseDiagnosticTroubleCodes(XElement layerEl, DiagnosticLayer layer)
    {
        var dtcNodes = layerEl.Desc("DTC");
        foreach (var dEl in dtcNodes)
        {
            var dtc = new DiagnosticTroubleCode
            {
                Id = dEl.Attr("ID"),
                Description = dEl.El("TEXT").Val(),
                DisplayCode = dEl.El("DISPLAY-TROUBLE-CODE").Val(),
                Severity = dEl.El("SEVERITY").Val(),
                DefinedInLayer = layer.ShortName,
                IsInherited = false
            };

            if (string.IsNullOrWhiteSpace(dtc.Description))
                dtc.Description = ExtractDescription(dEl);

            if (string.IsNullOrWhiteSpace(dtc.Description))
                dtc.Description = dEl.El("SHORT-NAME").Val();

            var codeVal = dEl.El("TROUBLE-CODE").Val();
            if (long.TryParse(codeVal, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tcNum))
            {
                dtc.TroubleCode = tcNum;
                dtc.TroubleCodeHex = $"0x{tcNum:X6}";
            }
            else if (codeVal.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                     long.TryParse(codeVal[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var tcHex))
            {
                dtc.TroubleCode = tcHex;
                dtc.TroubleCodeHex = $"0x{tcHex:X6}";
            }
            else
            {
                dtc.TroubleCodeHex = codeVal;
            }

            if (string.IsNullOrWhiteSpace(dtc.DisplayCode) && dtc.TroubleCode > 0)
                dtc.DisplayCode = FormatUdsDtcCode(dtc.TroubleCode);

            if (dtc.TroubleCode > 0)
            {
                var ftb = (byte)(dtc.TroubleCode & 0xFF);
                dtc.FailureTypeByte = $"0x{ftb:X2}";
            }

            layer.DiagnosticTroubleCodes.Add(dtc);
        }
    }

    private static PhysicalUnitInfo ParseUnitElement(XElement uEl)
    {
        var unit = new PhysicalUnitInfo
        {
            Id = uEl.Attr("ID"),
            ShortName = uEl.El("SHORT-NAME").Val(),
            DisplayName = uEl.El("DISPLAY-NAME").Val(),
            PhysicalDimensionRef = uEl.El("PHYSICAL-DIMENSION-REF")?.Attr("ID-REF") ?? string.Empty
        };

        if (double.TryParse(uEl.El("FACTOR-SI-TO-UNIT").Val(), NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            unit.FactorSiToUnit = f;
        if (double.TryParse(uEl.El("OFFSET-SI-TO-UNIT").Val(), NumberStyles.Float, CultureInfo.InvariantCulture, out var o))
            unit.OffsetSiToUnit = o;

        return unit;
    }

    private DataObjectProperty ParseDataObjectProperty(XElement dopEl, Dictionary<string, PhysicalUnitInfo> units)
    {
        var dop = new DataObjectProperty
        {
            Id = dopEl.Attr("ID"),
            ShortName = dopEl.El("SHORT-NAME").Val(),
            LongName = dopEl.El("LONG-NAME").Val(),
            Category = dopEl.Attr("CATEGORY")
        };

        if (string.IsNullOrWhiteSpace(dop.Category))
            dop.Category = dopEl.El("COMPU-METHOD")?.El("CATEGORY").Val() ?? "IDENTICAL";

        var diagTypeEl = dopEl.Desc("DIAG-CODED-TYPE").FirstOrDefault();
        if (diagTypeEl != null)
        {
            dop.BaseDataType = diagTypeEl.Attr("BASE-DATA-TYPE");
            if (string.IsNullOrEmpty(dop.BaseDataType))
                dop.BaseDataType = diagTypeEl.El("BASE-DATA-TYPE").Val();

            if (int.TryParse(diagTypeEl.Attr("BIT-LENGTH"), out var bl))
                dop.BitLength = bl;
            else if (int.TryParse(diagTypeEl.El("BIT-LENGTH").Val(), out var bl2))
                dop.BitLength = bl2;
        }

        // Resolve UNIT-REF against UNIT-SPEC dictionary first, fallback to UNIT-SPEC ID-REF
        var unitRefEl = dopEl.Desc("UNIT-REF").FirstOrDefault();
        var unitSpecEl = dopEl.Desc("UNIT-SPEC").FirstOrDefault();
        var rawUnitRef = unitRefEl?.Attr("ID-REF") ?? unitSpecEl?.Attr("ID-REF") ?? unitSpecEl?.El("SHORT-NAME").Val() ?? string.Empty;

        if (!string.IsNullOrEmpty(rawUnitRef) && units.TryGetValue(rawUnitRef, out var resolvedUnit))
        {
            dop.PhysicalUnit = !string.IsNullOrEmpty(resolvedUnit.DisplayName)
                ? resolvedUnit.DisplayName
                : resolvedUnit.ShortName;
        }
        else
        {
            dop.PhysicalUnit = rawUnitRef;
        }

        var compuMethod = dopEl.Desc("COMPU-METHOD").FirstOrDefault();
        if (compuMethod != null)
        {
            var numVals = compuMethod.Desc("COMPU-NUMERATOR").Elements().Select(v => v.Val()).ToList();
            var denVal = compuMethod.Desc("COMPU-DENOMINATOR").Elements().FirstOrDefault()?.Val();

            double divisor = 1.0;
            if (!string.IsNullOrEmpty(denVal) && double.TryParse(denVal, NumberStyles.Float, CultureInfo.InvariantCulture, out var den))
            {
                divisor = den != 0 ? den : 1.0;
            }

            if (numVals.Count >= 2)
            {
                if (double.TryParse(numVals[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var offset))
                    dop.Offset = offset / divisor;

                if (double.TryParse(numVals[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
                    dop.Factor = factor / divisor;
            }
            else if (numVals.Count == 1)
            {
                if (double.TryParse(numVals[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var factor))
                    dop.Factor = factor / divisor;
            }

            foreach (var scaleEl in compuMethod.Desc("COMPU-SCALE"))
            {
                var lowerLimit = scaleEl.El("LOWER-LIMIT")?.Val();
                var textConst = scaleEl.Desc("VT").FirstOrDefault()?.Val() ?? scaleEl.El("COMPU-CONST")?.Val();

                if (!string.IsNullOrEmpty(lowerLimit) &&
                    !string.IsNullOrEmpty(textConst) &&
                    long.TryParse(lowerLimit, NumberStyles.Integer, CultureInfo.InvariantCulture, out var key))
                {
                    dop.TextTableEntries[key] = textConst;
                }
            }
        }

        return dop;
    }

    private void ResolveDataIdentifiers(DiagnosticLayer layer, Dictionary<string, DataObjectProperty> dops)
    {
        var didMap = new Dictionary<string, DiagnosticDataIdentifier>(StringComparer.OrdinalIgnoreCase);

        foreach (var svc in layer.Services)
        {
            bool isReadDid = svc.ServiceIdHex.Equals("0x22", StringComparison.OrdinalIgnoreCase);
            bool isWriteDid = svc.ServiceIdHex.Equals("0x2E", StringComparison.OrdinalIgnoreCase);

            if (!isReadDid && !isWriteDid)
            {
                if (svc.Semantic.Contains("DATA-ID", StringComparison.OrdinalIgnoreCase) ||
                    svc.Semantic.Contains("IDENTIFICATION", StringComparison.OrdinalIgnoreCase))
                {
                    isReadDid = true;
                }
                else
                {
                    continue;
                }
            }

            var didParam = svc.RequestParameters.FirstOrDefault(p =>
                string.Equals(p.Semantic, "DATA-ID", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Semantic, "DATA-IDENTIFIER", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("DID", StringComparison.OrdinalIgnoreCase) ||
                (p.BytePosition == 1 && !string.IsNullOrEmpty(p.DefaultValueHex) && p.DefaultValueHex.Length >= 4));

            string didHex = string.Empty;
            if (didParam != null && !string.IsNullOrEmpty(didParam.DefaultValueHex))
            {
                didHex = NormalizeHex(didParam.DefaultValueHex);
            }
            else
            {
                var match = Regex.Match(svc.ShortName, @"(?<did>[0-9A-Fa-f]{4})");
                if (match.Success)
                    didHex = "0x" + match.Groups["did"].Value.ToUpperInvariant();
            }

            if (string.IsNullOrEmpty(didHex))
                continue;

            if (!didMap.TryGetValue(didHex, out var did))
            {
                did = new DiagnosticDataIdentifier
                {
                    DidHex = didHex,
                    ShortName = CleanServiceNameForDid(svc.ShortName),
                    LongName = svc.LongName,
                    AssociatedService = svc.ShortName,
                    Description = svc.Description,
                    Access = isWriteDid ? "Write" : "Read",
                    DataCategory = CategorizeDataIdentifier(didHex, CleanServiceNameForDid(svc.ShortName), svc.LongName, svc.Description),
                    DefinedInLayer = layer.ShortName,
                    IsInherited = false
                };

                if (int.TryParse(didHex.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var didDec))
                    did.DidDecimal = didDec;

                var payloadParam = svc.ResponseParameters.FirstOrDefault(p =>
                    p.BytePosition >= 3 || (!string.IsNullOrEmpty(p.DopRef) && !p.Name.Contains("DID", StringComparison.OrdinalIgnoreCase)));
                if (payloadParam == null && isWriteDid)
                {
                    payloadParam = svc.RequestParameters.FirstOrDefault(p =>
                        p.BytePosition >= 3 || (!string.IsNullOrEmpty(p.DopRef) && !p.Name.Contains("DID", StringComparison.OrdinalIgnoreCase)));
                }

                if (payloadParam != null)
                {
                    did.ByteLength = payloadParam.BitLength > 0 ? (payloadParam.BitLength + 7) / 8 : 1;
                    did.BitLength = payloadParam.BitLength;
                    did.DopRef = payloadParam.DopRef;

                    if (!string.IsNullOrEmpty(payloadParam.DopRef) && dops.TryGetValue(payloadParam.DopRef, out var dop))
                    {
                        did.DataType = dop.BaseDataType;
                        did.Unit = dop.PhysicalUnit;
                        did.ScalingFactor = dop.Factor;
                        did.Offset = dop.Offset;
                        did.MinPhysicalValue = dop.MinPhysicalValue;
                        did.MaxPhysicalValue = dop.MaxPhysicalValue;
                    }
                }

                didMap[didHex] = did;
            }
            else
            {
                if (isWriteDid && did.Access == "Read")
                    did.Access = "Read/Write";
                else if (isReadDid && did.Access == "Write")
                    did.Access = "Read/Write";
            }

            if (isReadDid || isWriteDid)
                svc.DataCategory = did.DataCategory;
        }

        // Supplement from ISO 22901-1 TABLE-ROW definitions (when DIDs are stored in a TABLE-KEY structure)
        foreach (var row in layer.TableRows.Where(r => !string.IsNullOrEmpty(r.KeyHex)))
        {
            if (didMap.ContainsKey(row.KeyHex)) continue;

            var did = new DiagnosticDataIdentifier
            {
                DidHex = row.KeyHex,
                ShortName = row.ShortName,
                LongName = row.LongName,
                AssociatedService = row.TableShortName,
                Description = row.Description,
                Access = "Read",
                DataCategory = CategorizeDataIdentifier(row.KeyHex, row.ShortName, row.LongName, row.Description),
                DefinedInLayer = layer.ShortName,
                IsInherited = false
            };

            if (int.TryParse(row.KeyHex.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var didDec))
                did.DidDecimal = didDec;

            if (!string.IsNullOrEmpty(row.DopRef) && dops.TryGetValue(row.DopRef, out var dop))
            {
                did.DopRef = row.DopRef;
                did.DataType = dop.BaseDataType;
                did.Unit = dop.PhysicalUnit;
                did.ScalingFactor = dop.Factor;
                did.Offset = dop.Offset;
                did.BitLength = dop.BitLength;
                did.ByteLength = dop.BitLength > 0 ? (dop.BitLength + 7) / 8 : 1;
            }

            didMap[row.KeyHex] = did;
        }

        layer.DataIdentifiers.AddRange(didMap.Values.OrderBy(d => d.DidHex));
    }

    private void ParseCommunicationParameters(XElement layerEl, DiagnosticLayer layer)
    {
        var compRefs = layerEl.Desc("COMPARAM-REF");
        foreach (var cRef in compRefs)
        {
            var cp = ParseSingleComParamRef(cRef, layer.ShortName);
            if (!string.IsNullOrEmpty(cp.Name))
            {
                if (layer.LayerType == DiagnosticLayerType.Protocol && string.IsNullOrEmpty(cp.Protocol))
                    cp.Protocol = layer.ShortName;
                layer.CommunicationParameters.Add(cp);
            }
        }
    }

    private static CommunicationParameter ParseSingleComParamRef(XElement cRef, string originLayer)
    {
        // ISO 22901-1 ODX 2.2 uses SIMPLE-VALUE, ODX 2.0 / custom uses VALUE or @VALUE
        var val = cRef.El("SIMPLE-VALUE")?.Val();
        if (string.IsNullOrEmpty(val)) val = cRef.El("VALUE")?.Val();
        if (string.IsNullOrEmpty(val)) val = cRef.Attr("VALUE");
        if (string.IsNullOrEmpty(val))
            val = string.Join(" ", cRef.Desc("SIMPLE-VALUE").Select(s => s.Val()));

        var name = cRef.El("SHORT-NAME").Val();
        if (string.IsNullOrWhiteSpace(name)) name = cRef.Attr("ID-REF");

        return new CommunicationParameter
        {
            Name = name,
            Value = val ?? string.Empty,
            Unit = cRef.El("UNIT")?.Val() ?? string.Empty,
            Protocol = cRef.El("PROTOCOL-SNREF")?.Attr("SHORT-NAME") ?? string.Empty,
            Description = ExtractDescription(cRef),
            DefinedInLayer = originLayer,
            IsInherited = false
        };
    }

    private static string NormalizeAddressing(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Physical";
        var u = raw.ToUpperInvariant();
        if (u.Contains("PHYSICAL") && u.Contains("FUNCTIONAL")) return "Physical/Functional";
        if (u.Contains("FUNCTIONAL")) return "Functional";
        return "Physical";
    }

    private static string ExtractDescription(XElement el)
    {
        var descEl = el.El("DESC");
        if (descEl == null) return string.Empty;

        var p = descEl.El("P");
        if (p != null) return p.Val();

        return descEl.Val();
    }

    private static string NormalizeHex(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var clean = input.Trim().Replace("0x", "", StringComparison.OrdinalIgnoreCase);
        if (clean.Length % 2 != 0) clean = "0" + clean;
        return "0x" + clean.ToUpperInvariant();
    }

    private static bool IsSubFunctionService(string sidHex)
    {
        if (string.IsNullOrEmpty(sidHex)) return false;
        var sid = sidHex.ToUpperInvariant();
        return sid is "0x10" or "0x11" or "0x19" or "0x27" or "0x28" or "0x31" or "0x85";
    }

    private static string InferServiceId(string shortName, string semantic)
    {
        var combined = $"{shortName} {semantic}".ToLowerInvariant();

        if (combined.Contains("session")) return "0x10";
        if (combined.Contains("ecu_reset") || combined.Contains("ecureset") || combined.Contains("reset")) return "0x11";
        if (combined.Contains("clear") || combined.Contains("cleardtcinformation")) return "0x14";
        if (combined.Contains("readdtc") || combined.Contains("read_dtc") || combined.Contains("fault")) return "0x19";
        if (combined.Contains("readdatabyidentifier") || combined.Contains("read_data") || combined.Contains("read_did")) return "0x22";
        if (combined.Contains("writedatabyidentifier") || combined.Contains("write_data") || combined.Contains("write_did")) return "0x2E";
        if (combined.Contains("security") || combined.Contains("securityaccess")) return "0x27";
        if (combined.Contains("communicationcontrol")) return "0x28";
        if (combined.Contains("testerpresent") || combined.Contains("tester_present")) return "0x3E";
        if (combined.Contains("routine") || combined.Contains("routinecontrol")) return "0x31";
        if (combined.Contains("requestdownload")) return "0x34";
        if (combined.Contains("requestupload")) return "0x35";
        if (combined.Contains("transferdata")) return "0x36";
        if (combined.Contains("requestfalshexit") || combined.Contains("requesttransferexit")) return "0x37";
        if (combined.Contains("controldtcsetting")) return "0x85";

        return string.Empty;
    }

    private static string CleanServiceNameForDid(string serviceShortName)
    {
        var cleaned = serviceShortName
            .Replace("ReadDataByIdentifier_", "", StringComparison.OrdinalIgnoreCase)
            .Replace("ReadDataByIdentifier", "", StringComparison.OrdinalIgnoreCase)
            .Replace("WriteDataByIdentifier_", "", StringComparison.OrdinalIgnoreCase)
            .Replace("WriteDataByIdentifier", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Read_", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Write_", "", StringComparison.OrdinalIgnoreCase);

        return string.IsNullOrWhiteSpace(cleaned) ? serviceShortName : cleaned;
    }

    private static string FormatUdsDtcCode(long code)
    {
        var highByte = (byte)((code >> 16) & 0xFF);
        var lowByte = (byte)((code >> 8) & 0xFF);
        var ftb = (byte)(code & 0xFF);

        char prefix = (highByte >> 6) switch
        {
            0 => 'P',
            1 => 'C',
            2 => 'B',
            3 => 'U',
            _ => 'P'
        };

        var digit1 = (highByte >> 4) & 0x03;
        var digit2 = highByte & 0x0F;
        var digit3 = (lowByte >> 4) & 0x0F;
        var digit4 = lowByte & 0x0F;

        var baseCode = $"{prefix}{digit1:X1}{digit2:X1}{digit3:X1}{digit4:X1}";
        return ftb != 0 ? $"{baseCode}-{ftb:X2}" : baseCode;
    }

    public static string CategorizeService(string serviceIdHex, string semantic, string shortName, string description = "")
    {
        var semUpper = (semantic ?? "").ToUpperInvariant().Trim();
        var sidNorm = (serviceIdHex ?? "").Trim().ToLowerInvariant();
        var nameUpper = (shortName ?? "").ToUpperInvariant().Trim();
        var descUpper = (description ?? "").ToUpperInvariant().Trim();
        var combinedText = $"{nameUpper} {descUpper} {semUpper}";

        if (semUpper.Contains("SESSION") || semUpper.Contains("SECURITY") ||
            semUpper.Contains("TESTER-PRESENT") || semUpper.Contains("TESTER_PRESENT") ||
            semUpper.Contains("ROUTINE") || semUpper.Contains("RESET") ||
            semUpper.Contains("COMMUNICATION-CONTROL") || semUpper.Contains("COMMUNICATION_CONTROL") ||
            semUpper.Contains("AUTHENTICATION") || semUpper.Contains("FLASH"))
        {
            return "System Control";
        }

        if (semUpper.Contains("CURRENT-DATA") || semUpper.Contains("CURRENTDATA") ||
            semUpper.Contains("DYNAMIC-DATA") || semUpper.Contains("DYNAMIC_DATA") ||
            semUpper.Contains("MEASURED") || semUpper.Contains("LIVE-DATA") ||
            semUpper.Contains("LIVEDATA") || semUpper.Contains("ACTUAL-VALUE"))
        {
            return "Current Data";
        }

        if (semUpper.Contains("STORED-DATA") || semUpper.Contains("STOREDDATA") ||
            semUpper.Contains("READ-DTC") || semUpper.Contains("READ_DTC") ||
            semUpper.Contains("CLEAR-DTC") || semUpper.Contains("CLEAR_DTC") ||
            semUpper.Contains("FREEZE-FRAME") || semUpper.Contains("FREEZEFRAME") ||
            semUpper.Contains("SNAPSHOT") || semUpper.Contains("FAULT-MEMORY") ||
            semUpper.Contains("FAULT_MEMORY") || semUpper.Contains("IDENTIFICATION") ||
            semUpper.Contains("IDENT") || semUpper.Contains("CODING") ||
            semUpper.Contains("CALIBRATION") || semUpper.Contains("ADAPTATION"))
        {
            return "Stored Data";
        }

        switch (sidNorm)
        {
            case "0x19":
            case "0x14":
            case "0x85":
            case "0x02":
            case "0x03":
            case "0x04":
            case "0x07":
            case "0x0a":
                return "Stored Data";

            case "0x2a":
            case "0x2c":
            case "0x01":
            case "0x06":
                return "Current Data";

            case "0x10":
            case "0x11":
            case "0x27":
            case "0x28":
            case "0x29":
            case "0x31":
            case "0x3e":
            case "0x86":
            case "0x87":
            case "0x34":
            case "0x35":
            case "0x36":
            case "0x37":
            case "0x38":
                return "System Control";
        }

        if (combinedText.Contains("VIN") ||
            combinedText.Contains("IDENTIF") ||
            combinedText.Contains("PARTNUMBER") ||
            combinedText.Contains("PART_NUMBER") ||
            combinedText.Contains("PART NUMBER") ||
            combinedText.Contains("SERIALNUMBER") ||
            combinedText.Contains("SERIAL_NUMBER") ||
            combinedText.Contains("SERIAL NUMBER") ||
            combinedText.Contains("SOFTWAREVERSION") ||
            combinedText.Contains("SOFTWARE_VERSION") ||
            combinedText.Contains("HARDWAREVERSION") ||
            combinedText.Contains("HARDWARE_VERSION") ||
            combinedText.Contains("BOOT_LOADER") ||
            combinedText.Contains("BOOTLOADER") ||
            combinedText.Contains("CALIBRATION") ||
            combinedText.Contains("FINGERPRINT") ||
            combinedText.Contains("CONFIG") ||
            combinedText.Contains("CODING") ||
            combinedText.Contains("FREEZE") ||
            combinedText.Contains("SNAPSHOT") ||
            combinedText.Contains("STORED") ||
            combinedText.Contains("FAULT") ||
            combinedText.Contains("ISOLATION") ||
            combinedText.Contains("DTC"))
        {
            return "Stored Data";
        }

        var didMatch = Regex.Match(nameUpper, @"0?X?(F1[0-9A-F]{2}|F0[0-9A-F]{2}|F8[0-9A-F]{2})");
        if (didMatch.Success)
        {
            return "Stored Data";
        }

        if (combinedText.Contains("CURRENT") ||
            combinedText.Contains("SPEED") ||
            combinedText.Contains("RPM") ||
            combinedText.Contains("TEMP") ||
            combinedText.Contains("TEMPERATURE") ||
            combinedText.Contains("VOLT") ||
            combinedText.Contains("VOLTAGE") ||
            combinedText.Contains("PRESSURE") ||
            combinedText.Contains("SENSOR") ||
            combinedText.Contains("ANGLE") ||
            combinedText.Contains("TORQUE") ||
            combinedText.Contains("LEVEL") ||
            combinedText.Contains("FLOW") ||
            combinedText.Contains("PEDAL") ||
            combinedText.Contains("THROTTLE") ||
            combinedText.Contains("DYNAMIC") ||
            combinedText.Contains("MEASUREMENT") ||
            combinedText.Contains("LIVE") ||
            combinedText.Contains("VALUE") ||
            combinedText.Contains("STATUS"))
        {
            return "Current Data";
        }

        if (sidNorm == "0x22" || sidNorm == "0x2e")
            return "Current Data";

        return "Current Data";
    }

    public static string CategorizeDataIdentifier(string didHex, string shortName, string longName = "", string description = "")
    {
        var hexUpper = (didHex ?? "").ToUpperInvariant().Trim();
        var nameUpper = (shortName ?? "").ToUpperInvariant().Trim();
        var longUpper = (longName ?? "").ToUpperInvariant().Trim();
        var descUpper = (description ?? "").ToUpperInvariant().Trim();
        var combined = $"{nameUpper} {longUpper} {descUpper}";

        if (combined.Contains("VIN") ||
            combined.Contains("IDENTIF") ||
            combined.Contains("PART NUMBER") ||
            combined.Contains("PARTNUMBER") ||
            combined.Contains("PART_NUMBER") ||
            combined.Contains("SERIAL") ||
            combined.Contains("VERSION") ||
            combined.Contains("CALIBRATION") ||
            combined.Contains("CODING") ||
            combined.Contains("FINGERPRINT") ||
            combined.Contains("MANUFACTURER") ||
            combined.Contains("SUPPLIER") ||
            combined.Contains("ECU NAME") ||
            combined.Contains("ODX FILE") ||
            combined.Contains("SNAPSHOT") ||
            combined.Contains("FREEZE") ||
            combined.Contains("STORED") ||
            combined.Contains("FAULT") ||
            combined.Contains("ISOLATION") ||
            combined.Contains("HISTORY"))
        {
            return "Stored Data";
        }

        var cleanHex = hexUpper.StartsWith("0X", StringComparison.OrdinalIgnoreCase) ? hexUpper[2..] : hexUpper;
        if (int.TryParse(cleanHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var didNum))
        {
            if (didNum >= 0xF100 && didNum <= 0xF1FF) return "Stored Data";
            if (didNum >= 0xF000 && didNum <= 0xF0FF) return "Stored Data";
            if (didNum >= 0xF800 && didNum <= 0xF8FF) return "Stored Data";
            if (didNum < 0xF000) return "Current Data";
        }

        return "Current Data";
    }
}
