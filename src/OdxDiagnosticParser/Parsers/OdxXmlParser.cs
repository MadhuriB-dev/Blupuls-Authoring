using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using OdxDiagnosticParser.Models;

namespace OdxDiagnosticParser.Parsers;

/// <summary>
/// High-performance, schema-resilient parser for ODX XML (ODX 2.0.1, 2.2.0, ISO 22901-1).
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
        var pkg = ParseStream(stream, Path.GetFileName(filePath));
        pkg.SourcePath = filePath;
        return pkg;
    }

    public DiagnosticPackage ParseXml(string xmlContent, string sourceName = "InlineXml")
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentException("XML content cannot be null or empty.", nameof(xmlContent));

        // Strip any leading non-XML characters, BOM artifacts, or zero-width spaces before '<'
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
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        // Read all stream bytes to inspect magic header and safely decode encoding
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var bytes = ms.ToArray();

        if (bytes.Length == 0)
            throw new ArgumentException("The stream is empty.", nameof(stream));

        // Check if stream is actually a ZIP/PDX archive (PK\x03\x04 or PK\x05\x06)
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

        // Decode XML bytes handling BOM, UTF-16, ISO-8859-1, Windows-1252
        string xmlContent = DecodeXmlBytes(bytes);
        return ParseXml(xmlContent, sourceName);
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

        // Detect encoding from BOM
        var encoding = System.Text.Encoding.UTF8;
        if (bytes.Length >= 2)
        {
            if (bytes[0] == 0xFF && bytes[1] == 0xFE)
                encoding = System.Text.Encoding.Unicode; // UTF-16 LE
            else if (bytes[0] == 0xFE && bytes[1] == 0xFF)
                encoding = System.Text.Encoding.BigEndianUnicode; // UTF-16 BE
            else if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                encoding = System.Text.Encoding.UTF8;
        }

        string text = encoding.GetString(bytes);

        // Check if XML declaration specifies a legacy OEM encoding (e.g. ISO-8859-1, Windows-1252)
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

        // Strip any leading non-XML characters, BOM artifacts, or zero-width spaces before '<'
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

        // Check model version
        pkg.Version = root.Attr("MODEL-VERSION");
        if (string.IsNullOrWhiteSpace(pkg.Version))
            pkg.Version = "2.2.0";

        // Global dictionaries for cross-referencing ID-REFs
        var globalRequests = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
        var globalPosResponses = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
        var globalNegResponses = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
        var globalDops = new Dictionary<string, DataObjectProperty>(StringComparer.OrdinalIgnoreCase);

        // Index all global requests, responses, and DOPs in the document
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

        // Global DOP specs (e.g. in shared data or protocols)
        foreach (var dopEl in root.Desc("DATA-OBJECT-PROP"))
        {
            var dop = ParseDataObjectProperty(dopEl);
            if (!string.IsNullOrEmpty(dop.Id) && !globalDops.ContainsKey(dop.Id))
                globalDops[dop.Id] = dop;
        }

        // Parse layers: BASE-VARIANTS, ECU-VARIANTS, ECU-SHARED-DATAS, PROTOCOLS
        var layerNodes = new List<(XElement Element, DiagnosticLayerType Type)>();

        foreach (var el in root.Desc("BASE-VARIANT"))
            layerNodes.Add((el, DiagnosticLayerType.BaseVariant));

        foreach (var el in root.Desc("ECU-VARIANT"))
            layerNodes.Add((el, DiagnosticLayerType.EcuVariant));

        foreach (var el in root.Desc("ECU-SHARED-DATA"))
            layerNodes.Add((el, DiagnosticLayerType.EcuSharedData));

        foreach (var el in root.Desc("PROTOCOL"))
            layerNodes.Add((el, DiagnosticLayerType.Protocol));

        foreach (var el in root.Desc("FUNCTIONAL-GROUP"))
            layerNodes.Add((el, DiagnosticLayerType.FunctionalGroup));

        // If no container was found but there are DIAG-SERVICES or root is DIAG-LAYER
        if (layerNodes.Count == 0)
        {
            var diagLayer = root.Desc("DIAG-LAYER").FirstOrDefault() ?? root;
            layerNodes.Add((diagLayer, DiagnosticLayerType.EcuVariant));
        }

        foreach (var (layerEl, layerType) in layerNodes)
        {
            var layer = ParseDiagnosticLayer(layerEl, layerType, sourceName, pkg.Version, globalRequests, globalPosResponses, globalNegResponses, globalDops);
            pkg.Layers.Add(layer);
        }

        if (pkg.Layers.Count > 0)
        {
            var primaryLayer = pkg.Layers.FirstOrDefault(l => l.LayerType == DiagnosticLayerType.EcuVariant)
                               ?? pkg.Layers[0];
            pkg.Name = !string.IsNullOrWhiteSpace(primaryLayer.ShortName) ? primaryLayer.ShortName : pkg.Name;
            pkg.Description = primaryLayer.Description;
        }

        return pkg;
    }

    private DiagnosticLayer ParseDiagnosticLayer(
        XElement layerEl,
        DiagnosticLayerType layerType,
        string sourceFileName,
        string defaultModelVersion,
        Dictionary<string, XElement> globalRequests,
        Dictionary<string, XElement> globalPosResponses,
        Dictionary<string, XElement> globalNegResponses,
        Dictionary<string, DataObjectProperty> globalDops)
    {
        var layer = new DiagnosticLayer
        {
            LayerType = layerType,
            Id = layerEl.Attr("ID"),
            ShortName = layerEl.El("SHORT-NAME").Val(),
            LongName = layerEl.El("LONG-NAME").Val(),
            Description = ExtractDescription(layerEl),
            ModelVersion = layerEl.Attr("MODEL-VERSION") is { Length: > 0 } v ? v : defaultModelVersion,
            SourceFileName = sourceFileName
        };

        // Fallback shortname if empty
        if (string.IsNullOrWhiteSpace(layer.ShortName))
            layer.ShortName = layerEl.Attr("ID");

        // Local DOPs
        var localDops = new Dictionary<string, DataObjectProperty>(globalDops, StringComparer.OrdinalIgnoreCase);
        foreach (var dopEl in layerEl.Desc("DATA-OBJECT-PROP"))
        {
            var dop = ParseDataObjectProperty(dopEl);
            if (!string.IsNullOrEmpty(dop.Id))
                localDops[dop.Id] = dop;
            layer.DataObjectProperties.Add(dop);
        }

        // DTCs
        ParseDiagnosticTroubleCodes(layerEl, layer);

        // Services
        ParseDiagnosticServices(layerEl, layer, localDops, globalRequests, globalPosResponses, globalNegResponses);

        // DIDs (synthesized from 0x22 / 0x2E services and parameters)
        ResolveDataIdentifiers(layer, localDops);

        // Communication Parameters
        ParseCommunicationParameters(layerEl, layer);

        return layer;
    }

    private void ParseDiagnosticServices(
        XElement layerEl,
        DiagnosticLayer layer,
        Dictionary<string, DataObjectProperty> dops,
        Dictionary<string, XElement> globalRequests,
        Dictionary<string, XElement> globalPosResponses,
        Dictionary<string, XElement> globalNegResponses)
    {
        var serviceElements = layerEl.Desc("DIAG-SERVICE");
        foreach (var sEl in serviceElements)
        {
            var service = new DiagnosticService
            {
                Id = sEl.Attr("ID"),
                ShortName = sEl.El("SHORT-NAME").Val(),
                LongName = sEl.El("LONG-NAME").Val(),
                Description = ExtractDescription(sEl),
                Semantic = sEl.Attr("SEMANTIC")
            };

            if (string.IsNullOrWhiteSpace(service.Semantic))
                service.Semantic = sEl.El("SEMANTIC").Val();

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

                // Service ID
                var sidParam = service.RequestParameters.FirstOrDefault(p =>
                    string.Equals(p.Semantic, "SERVICE-ID", StringComparison.OrdinalIgnoreCase) ||
                    p.BytePosition == 0 && !string.IsNullOrEmpty(p.DefaultValueHex));

                if (sidParam != null)
                {
                    service.ServiceIdHex = NormalizeHex(sidParam.DefaultValueHex);
                }

                // Sub-function
                var subParam = service.RequestParameters.FirstOrDefault(p =>
                    string.Equals(p.Semantic, "SUBFUNCTION", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Semantic, "SUB-FUNCTION", StringComparison.OrdinalIgnoreCase) ||
                    p.BytePosition == 1 && IsSubFunctionService(service.ServiceIdHex));

                if (subParam != null)
                {
                    service.SubFunctionHex = NormalizeHex(subParam.DefaultValueHex);
                }
            }

            // Fallback for Service ID from short name or semantic if not in parameters
            if (string.IsNullOrEmpty(service.ServiceIdHex))
            {
                service.ServiceIdHex = InferServiceId(service.ShortName, service.Semantic);
            }

            // Resolve Positive Response
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

            // Resolve Negative Responses
            var negRefs = sEl.Desc("NEG-RESPONSE-REF").Select(r => r.Attr("ID-REF")).Where(r => !string.IsNullOrEmpty(r));
            foreach (var nRef in negRefs)
            {
                if (globalNegResponses.TryGetValue(nRef, out var negEl))
                {
                    var nrcName = negEl.El("SHORT-NAME").Val();
                    var nrcParam = negEl.Desc("PARAM").FirstOrDefault(p => string.Equals(p.Attr("SEMANTIC"), "NRC", StringComparison.OrdinalIgnoreCase));
                    var nrcCode = nrcParam?.El("CODED-VALUE").Val() ?? nrcParam?.El("DEFAULT-VALUE").Val() ?? "";
                    if (!string.IsNullOrEmpty(nrcCode))
                        service.NegativeResponses.Add($"{NormalizeHex(nrcCode)} {nrcName}".Trim());
                    else if (!string.IsNullOrEmpty(nrcName))
                        service.NegativeResponses.Add(nrcName);
                }
                else
                {
                    service.NegativeResponses.Add(nRef);
                }
            }

            layer.Services.Add(service);
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

            // Coded / default value
            var codedVal = pEl.El("CODED-VALUE").Val();
            if (string.IsNullOrEmpty(codedVal))
                codedVal = pEl.El("DEFAULT-VALUE").Val();
            if (string.IsNullOrEmpty(codedVal))
                codedVal = pEl.El("BYTE-VALUE").Val();
            if (string.IsNullOrEmpty(codedVal))
                codedVal = pEl.El("PHYSICAL-DEFAULT-VALUE").Val();

            param.DefaultValueHex = codedVal;

            // DOP reference
            var dopRef = pEl.El("DOP-REF")?.Attr("ID-REF");
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
                Severity = dEl.El("SEVERITY").Val()
            };

            if (string.IsNullOrWhiteSpace(dtc.Description))
                dtc.Description = ExtractDescription(dEl);

            if (string.IsNullOrWhiteSpace(dtc.Description))
                dtc.Description = dEl.El("SHORT-NAME").Val();

            // Trouble code integer or hex
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

            // Standard OBD/UDS alphanumeric display code derivation if missing (Pxxxx, Cxxxx, Bxxxx, Uxxxx)
            if (string.IsNullOrWhiteSpace(dtc.DisplayCode) && dtc.TroubleCode > 0)
            {
                dtc.DisplayCode = FormatUdsDtcCode(dtc.TroubleCode);
            }

            // Failure Type Byte (FTB) - lowest byte of 3-byte DTC
            if (dtc.TroubleCode > 0)
            {
                var ftb = (byte)(dtc.TroubleCode & 0xFF);
                dtc.FailureTypeByte = $"0x{ftb:X2}";
            }

            layer.DiagnosticTroubleCodes.Add(dtc);
        }
    }

    private DataObjectProperty ParseDataObjectProperty(XElement dopEl)
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

        // Coded type
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

        // Unit
        var unitEl = dopEl.Desc("UNIT-SPEC").FirstOrDefault() ?? dopEl.Desc("UNIT-REF").FirstOrDefault();
        dop.PhysicalUnit = unitEl?.Attr("ID-REF") ?? unitEl?.El("SHORT-NAME").Val() ?? string.Empty;

        // Compu-Method scaling (Factor & Offset)
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

            // Text table entries
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

        // Scan all services in this layer for ReadDataByIdentifier (0x22) or WriteDataByIdentifier (0x2E)
        foreach (var svc in layer.Services)
        {
            bool isReadDid = svc.ServiceIdHex.Equals("0x22", StringComparison.OrdinalIgnoreCase);
            bool isWriteDid = svc.ServiceIdHex.Equals("0x2E", StringComparison.OrdinalIgnoreCase);

            if (!isReadDid && !isWriteDid)
            {
                // Check semantic
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

            // Find DID parameter from request
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
                // Try extracting hex 0xFxxx or 0x0xxx from service name (e.g., "ReadDataByIdentifier_F190")
                var match = Regex.Match(svc.ShortName, @"(?<did>[0-9A-Fa-f]{4})");
                if (match.Success)
                {
                    didHex = "0x" + match.Groups["did"].Value.ToUpperInvariant();
                }
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
                    Access = isWriteDid ? "Write" : "Read"
                };

                if (int.TryParse(didHex.Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var didDec))
                {
                    did.DidDecimal = didDec;
                }

                // Find payload parameter in positive response to extract data type, scaling, units
                var payloadParam = svc.ResponseParameters.FirstOrDefault(p =>
                    p.BytePosition >= 3 || (!string.IsNullOrEmpty(p.DopRef) && !p.Name.Contains("DID", StringComparison.OrdinalIgnoreCase)));

                if (payloadParam != null)
                {
                    did.ByteLength = payloadParam.BitLength > 0 ? (payloadParam.BitLength + 7) / 8 : 1;
                    did.BitLength = payloadParam.BitLength;

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
                // Merge access
                if (isWriteDid && did.Access == "Read")
                    did.Access = "Read/Write";
                else if (isReadDid && did.Access == "Write")
                    did.Access = "Read/Write";
            }
        }

        layer.DataIdentifiers.AddRange(didMap.Values.OrderBy(d => d.DidHex));
    }

    private void ParseCommunicationParameters(XElement layerEl, DiagnosticLayer layer)
    {
        var compRefs = layerEl.Desc("COMPARAM-REF");
        foreach (var cRef in compRefs)
        {
            var cp = new CommunicationParameter
            {
                Name = cRef.Attr("ID-REF"),
                Value = cRef.El("VALUE")?.Val() ?? cRef.Attr("VALUE"),
                Unit = cRef.El("UNIT")?.Val() ?? string.Empty,
                Description = ExtractDescription(cRef)
            };

            if (string.IsNullOrWhiteSpace(cp.Name))
                cp.Name = cRef.El("SHORT-NAME").Val();

            if (!string.IsNullOrEmpty(cp.Name))
                layer.CommunicationParameters.Add(cp);
        }
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

    /// <summary>
    /// Formats a 3-byte trouble code according to SAE J2012 / ISO 14229 standard UDS format (Pxxxx-xx).
    /// </summary>
    private static string FormatUdsDtcCode(long code)
    {
        // 3-byte DTC: Byte 2 = High, Byte 1 = Low, Byte 0 = Failure Type Byte (FTB)
        var highByte = (byte)((code >> 16) & 0xFF);
        var lowByte = (byte)((code >> 8) & 0xFF);
        var ftb = (byte)(code & 0xFF);

        char prefix = (highByte >> 6) switch
        {
            0 => 'P', // Powertrain
            1 => 'C', // Chassis
            2 => 'B', // Body
            3 => 'U', // Network
            _ => 'P'
        };

        var digit1 = (highByte >> 4) & 0x03;
        var digit2 = highByte & 0x0F;
        var digit3 = (lowByte >> 4) & 0x0F;
        var digit4 = lowByte & 0x0F;

        var baseCode = $"{prefix}{digit1:X1}{digit2:X1}{digit3:X1}{digit4:X1}";
        return ftb != 0 ? $"{baseCode}-{ftb:X2}" : baseCode;
    }
}

