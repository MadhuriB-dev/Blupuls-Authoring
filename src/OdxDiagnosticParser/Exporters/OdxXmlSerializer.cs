using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using OdxDiagnosticParser.Models;

namespace OdxDiagnosticParser.Exporters;

/// <summary>
/// Serializes DiagnosticPackage models (including newly authored or edited DIDs, DOPs, Services, Parameters,
/// DTCs, ComParams, PARENT-REFS, and ODX-V topology) into ISO 22901-1 (ODX XML) and ISO 22901-2 (PDX ZIP) archives.
/// </summary>
public static class OdxXmlSerializer
{
    private static readonly XNamespace XsiNs = "http://www.w3.org/2001/XMLSchema-instance";

    /// <summary>
    /// Exports the entire DiagnosticPackage as a standalone ISO 22901-1 ODX XML document.
    /// </summary>
    public static string SerializePackageToOdxXml(DiagnosticPackage package)
    {
        if (package == null) throw new ArgumentNullException(nameof(package));

        var root = new XElement("ODX",
            new XAttribute("MODEL-VERSION", string.IsNullOrWhiteSpace(package.Version) ? "2.2.0" : package.Version),
            new XAttribute(XNamespace.Xmlns + "xsi", XsiNs));

        foreach (var spec in package.ComParamSpecs)
        {
            root.Add(BuildComParamSpecElement(spec));
        }

        var containerGroups = package.Layers
            .GroupBy(l => string.IsNullOrWhiteSpace(l.ContainerShortName) ? $"DLC_{package.Name}" : l.ContainerShortName,
                     StringComparer.OrdinalIgnoreCase);

        foreach (var grp in containerGroups)
        {
            root.Add(BuildDiagLayerContainerElement(grp.Key, grp.ToList()));
        }

        foreach (var vi in package.VehicleInfos)
        {
            root.Add(BuildVehicleInfoSpecElement(vi));
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
        return doc.ToString();
    }

    /// <summary>
    /// Saves the complete DiagnosticPackage as an ISO 22901-2 packaged .pdx archive on disk.
    /// </summary>
    public static string ExportToPdx(DiagnosticPackage package, string targetPdxPath)
    {
        if (package == null) throw new ArgumentNullException(nameof(package));
        if (string.IsNullOrWhiteSpace(targetPdxPath)) throw new ArgumentException("Target PDX path cannot be empty.", nameof(targetPdxPath));

        var dir = Path.GetDirectoryName(targetPdxPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(targetPdxPath))
            File.Delete(targetPdxPath);

        var safePkgName = SanitizeId(string.IsNullOrWhiteSpace(package.Name) ? "DiagnosticPackage" : package.Name);
        var modelVer = string.IsNullOrWhiteSpace(package.Version) ? "2.2.0" : package.Version;
        var isoNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

        var ablocksEl = new XElement("ABLOCKS");
        var filesToWrite = new List<(string EntryPath, string XmlContent)>();

        // 1. ODX-C (Protocols & ComParamSpecs)
        var protoLayers = package.Layers.Where(l => l.LayerType == DiagnosticLayerType.Protocol).ToList();
        if (package.ComParamSpecs.Any() || protoLayers.Any())
        {
            var odxCRoot = new XElement("ODX", new XAttribute("MODEL-VERSION", modelVer), new XAttribute(XNamespace.Xmlns + "xsi", XsiNs));
            foreach (var spec in package.ComParamSpecs)
                odxCRoot.Add(BuildComParamSpecElement(spec));
            foreach (var grp in protoLayers.GroupBy(l => string.IsNullOrWhiteSpace(l.ContainerShortName) ? "DLC_Protocols" : l.ContainerShortName))
                odxCRoot.Add(BuildDiagLayerContainerElement(grp.Key, grp.ToList()));

            var relPath = "odx-c/Protocols_UDS.odx-c";
            filesToWrite.Add((relPath, new XDocument(new XDeclaration("1.0", "utf-8", "yes"), odxCRoot).ToString()));
            ablocksEl.Add(BuildAblockElement("Protocols_UDS", "ODX-COMPARAM-SPEC", relPath, "application/x-asam.odx.c", isoNow));
        }

        // 2. ODX-E (Shared Data)
        var sharedLayers = package.Layers.Where(l => l.LayerType == DiagnosticLayerType.EcuSharedData).ToList();
        if (sharedLayers.Any())
        {
            var odxERoot = new XElement("ODX", new XAttribute("MODEL-VERSION", modelVer), new XAttribute(XNamespace.Xmlns + "xsi", XsiNs));
            foreach (var grp in sharedLayers.GroupBy(l => string.IsNullOrWhiteSpace(l.ContainerShortName) ? "DLC_SharedData" : l.ContainerShortName))
                odxERoot.Add(BuildDiagLayerContainerElement(grp.Key, grp.ToList()));

            var relPath = "odx-e/Shared_UDS_Services.odx-e";
            filesToWrite.Add((relPath, new XDocument(new XDeclaration("1.0", "utf-8", "yes"), odxERoot).ToString()));
            ablocksEl.Add(BuildAblockElement("Shared_UDS_Data", "ODX-ECU-CONFIG", relPath, "application/x-asam.odx.e", isoNow));
        }

        // 3. ODX-D (FunctionalGroups, BaseVariants, ECU Variants)
        var diagLayers = package.Layers.Where(l =>
            l.LayerType is DiagnosticLayerType.FunctionalGroup or DiagnosticLayerType.BaseVariant or DiagnosticLayerType.EcuVariant).ToList();
        if (diagLayers.Any() || filesToWrite.Count == 0)
        {
            var odxDRoot = new XElement("ODX", new XAttribute("MODEL-VERSION", modelVer), new XAttribute(XNamespace.Xmlns + "xsi", XsiNs));
            foreach (var grp in diagLayers.GroupBy(l => string.IsNullOrWhiteSpace(l.ContainerShortName) ? $"DLC_{safePkgName}" : l.ContainerShortName))
                odxDRoot.Add(BuildDiagLayerContainerElement(grp.Key, grp.ToList()));

            var relPath = $"odx-d/{safePkgName}.odx-d";
            filesToWrite.Add((relPath, new XDocument(new XDeclaration("1.0", "utf-8", "yes"), odxDRoot).ToString()));
            ablocksEl.Add(BuildAblockElement($"DLC_{safePkgName}", "ODX-DATA", relPath, "application/x-asam.odx.d", isoNow));
        }

        // 4. ODX-V (Vehicle Topology)
        if (package.VehicleInfos.Any())
        {
            var odxVRoot = new XElement("ODX", new XAttribute("MODEL-VERSION", modelVer), new XAttribute(XNamespace.Xmlns + "xsi", XsiNs));
            foreach (var vi in package.VehicleInfos)
                odxVRoot.Add(BuildVehicleInfoSpecElement(vi));

            var relPath = "odx-v/VehicleTopology.odx-v";
            filesToWrite.Add((relPath, new XDocument(new XDeclaration("1.0", "utf-8", "yes"), odxVRoot).ToString()));
            ablocksEl.Add(BuildAblockElement("VehicleTopology", "ODX-VEHICLE-INFO", relPath, "application/x-asam.odx.v", isoNow));
        }

        // 5. index.xml Catalog
        var catalogRoot = new XElement("CATALOG",
            new XAttribute("F-VERSION", modelVer),
            new XAttribute("MODEL-VERSION", modelVer),
            new XAttribute(XNamespace.Xmlns + "xsi", XsiNs),
            new XElement("SHORT-NAME", string.IsNullOrWhiteSpace(package.Catalog?.ShortName) ? $"PACK_{safePkgName}" : package.Catalog.ShortName),
            new XElement("VERSION", package.Catalog?.Version ?? "1.0.0"),
            new XElement("CREATOR", package.Catalog?.Creator ?? "Bluepuls Authoring Tool"),
            new XElement("DATE", DateTime.UtcNow.ToString("yyyy-MM-dd")),
            new XElement("DESC", package.Description ?? $"Exported ISO 22901-2 PDX package for {package.Name}"),
            ablocksEl);

        if (package.Catalog?.Vehicles.Any() == true)
        {
            var vehiclesEl = new XElement("VEHICLES");
            foreach (var v in package.Catalog.Vehicles)
            {
                vehiclesEl.Add(new XElement("VEHICLE-INFO",
                    new XElement("SHORT-NAME", v.ShortName),
                    new XElement("MODEL", v.Model),
                    new XElement("COMPANY", v.Company),
                    new XElement("MODEL-YEAR", v.ModelYear)));
            }
            catalogRoot.Add(vehiclesEl);
        }

        var indexXml = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), catalogRoot).ToString();

        using var zipStream = new FileStream(targetPdxPath, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

        WriteZipEntry(archive, "index.xml", indexXml);
        foreach (var (entryPath, xmlContent) in filesToWrite)
        {
            WriteZipEntry(archive, entryPath, xmlContent);
        }

        return targetPdxPath;
    }

    private static XElement BuildAblockElement(string shortName, string category, string relPath, string mimeType, string createdAt)
    {
        return new XElement("ABLOCK",
            new XElement("SHORT-NAME", shortName),
            new XElement("CATEGORY", category),
            new XElement("FILES",
                new XElement("FILE",
                    new XAttribute("MIME-TYPE", mimeType),
                    new XAttribute("CREATION-DATE", createdAt),
                    relPath)));
    }

    private static XElement BuildDiagLayerContainerElement(string containerName, List<DiagnosticLayer> layers)
    {
        var containerEl = new XElement("DIAG-LAYER-CONTAINER",
            new XAttribute("ID", SanitizeId(containerName)),
            new XElement("SHORT-NAME", containerName));

        void AppendGroup(string groupElementName, string layerElementName, DiagnosticLayerType type)
        {
            var matched = layers.Where(l => l.LayerType == type).ToList();
            if (matched.Count == 0) return;
            var groupEl = new XElement(groupElementName);
            foreach (var layer in matched)
                groupEl.Add(BuildDiagnosticLayerElement(layer, layerElementName));
            containerEl.Add(groupEl);
        }

        AppendGroup("PROTOCOLS", "PROTOCOL", DiagnosticLayerType.Protocol);
        AppendGroup("FUNCTIONAL-GROUPS", "FUNCTIONAL-GROUP", DiagnosticLayerType.FunctionalGroup);
        AppendGroup("ECU-SHARED-DATAS", "ECU-SHARED-DATA", DiagnosticLayerType.EcuSharedData);
        AppendGroup("BASE-VARIANTS", "BASE-VARIANT", DiagnosticLayerType.BaseVariant);
        AppendGroup("ECU-VARIANTS", "ECU-VARIANT", DiagnosticLayerType.EcuVariant);

        return containerEl;
    }

    private static XElement BuildDiagnosticLayerElement(DiagnosticLayer layer, string elementName)
    {
        var layerEl = new XElement(elementName,
            new XAttribute("ID", string.IsNullOrWhiteSpace(layer.Id) ? SanitizeId(layer.ShortName) : layer.Id),
            new XElement("SHORT-NAME", layer.ShortName));

        if (!string.IsNullOrWhiteSpace(layer.LongName))
            layerEl.Add(new XElement("LONG-NAME", layer.LongName));
        if (!string.IsNullOrWhiteSpace(layer.Description))
            layerEl.Add(new XElement("DESC", new XElement("P", layer.Description)));

        if (layer.LayerType == DiagnosticLayerType.Protocol && !string.IsNullOrWhiteSpace(layer.ComParamSpecRef))
        {
            layerEl.Add(new XElement("COMPARAM-SPEC-REF",
                new XAttribute("ID-REF", layer.ComParamSpecRef),
                new XAttribute("DOCREF", string.IsNullOrWhiteSpace(layer.ComParamSpecDocRef) ? layer.ComParamSpecRef : layer.ComParamSpecDocRef),
                new XAttribute("DOCTYPE", "COMPARAM-SPEC")));
        }

        // PARENT-REFS
        if (layer.ParentRefs.Any())
        {
            var parentRefsEl = new XElement("PARENT-REFS");
            foreach (var pref in layer.ParentRefs)
            {
                var pEl = new XElement("PARENT-REF",
                    new XAttribute(XsiNs + "type", string.IsNullOrWhiteSpace(pref.ParentRefType) ? "BASE-VARIANT-REF" : pref.ParentRefType),
                    new XAttribute("ID-REF", string.IsNullOrWhiteSpace(pref.IdRef) ? pref.ResolvedLayerId : pref.IdRef));

                if (!string.IsNullOrWhiteSpace(pref.DocRef))
                    pEl.Add(new XAttribute("DOCREF", pref.DocRef));
                if (!string.IsNullOrWhiteSpace(pref.DocType))
                    pEl.Add(new XAttribute("DOCTYPE", pref.DocType));

                if (pref.NotInheritedDiagComms.Any())
                {
                    var niEl = new XElement("NOT-INHERITED-DIAG-COMMS");
                    foreach (var excluded in pref.NotInheritedDiagComms)
                        niEl.Add(new XElement("NOT-INHERITED-DIAG-COMM", new XElement("DIAG-COMM-SNREF", new XAttribute("SHORT-NAME", excluded))));
                    pEl.Add(niEl);
                }

                parentRefsEl.Add(pEl);
            }
            layerEl.Add(parentRefsEl);
        }

        // ECU-VARIANT-PATTERNS
        if (layer.VariantPatterns.Any())
        {
            var patsEl = new XElement("ECU-VARIANT-PATTERNS");
            foreach (var pat in layer.VariantPatterns)
            {
                var mpEl = new XElement("MATCHING-PARAMETERS");
                foreach (var m in pat.MatchingParameters)
                {
                    mpEl.Add(new XElement("MATCHING-PARAMETER",
                        new XElement("EXPECTED-VALUE", m.ExpectedValue),
                        new XElement("DIAG-COMM-SNREF", new XAttribute("SHORT-NAME", m.DiagCommShortName)),
                        new XElement("OUT-PARAM-IF-SNREF", new XAttribute("SHORT-NAME", m.OutParamShortName))));
                }
                patsEl.Add(new XElement("ECU-VARIANT-PATTERN",
                    new XElement("SHORT-NAME", pat.Name),
                    mpEl));
            }
            layerEl.Add(patsEl);
        }

        // DIAG-DATA-DICTIONARY-SPEC (Own Units, Own DOPs, Own DTCs)
        var ownDops = layer.DataObjectProperties.Where(d => !d.IsInherited).ToList();
        var ownDtcs = layer.DiagnosticTroubleCodes.Where(t => !t.IsInherited).ToList();
        if (layer.Units.Any() || ownDops.Any() || ownDtcs.Any())
        {
            var dictEl = new XElement("DIAG-DATA-DICTIONARY-SPEC");

            var allUnits = new List<PhysicalUnitInfo>(layer.Units);
            foreach (var dop in ownDops.Where(d => !string.IsNullOrWhiteSpace(d.PhysicalUnit)))
            {
                if (!allUnits.Any(u => u.DisplayName == dop.PhysicalUnit || u.ShortName == dop.PhysicalUnit || u.Id == dop.PhysicalUnit))
                {
                    allUnits.Add(new PhysicalUnitInfo
                    {
                        Id = $"UNIT_{SanitizeId(dop.PhysicalUnit)}",
                        ShortName = SanitizeId(dop.PhysicalUnit),
                        DisplayName = dop.PhysicalUnit
                    });
                }
            }

            if (allUnits.Any())
            {
                var unitsEl = new XElement("UNITS");
                foreach (var u in allUnits)
                {
                    unitsEl.Add(new XElement("UNIT",
                        new XAttribute("ID", string.IsNullOrWhiteSpace(u.Id) ? $"UNIT_{SanitizeId(u.ShortName)}" : u.Id),
                        new XElement("SHORT-NAME", u.ShortName),
                        new XElement("DISPLAY-NAME", string.IsNullOrWhiteSpace(u.DisplayName) ? u.ShortName : u.DisplayName)));
                }
                dictEl.Add(new XElement("UNIT-SPEC", unitsEl));
            }

            if (ownDops.Any())
            {
                var dopsEl = new XElement("DATA-OBJECT-PROPS");
                foreach (var dop in ownDops)
                    dopsEl.Add(BuildDopElement(dop, allUnits));
                dictEl.Add(dopsEl);
            }

            if (ownDtcs.Any())
            {
                var dtcsEl = new XElement("DTCS");
                foreach (var dtc in ownDtcs)
                {
                    var dEl = new XElement("DTC",
                        new XAttribute("ID", string.IsNullOrWhiteSpace(dtc.Id) ? $"DTC_{SanitizeId(dtc.DisplayCode)}" : dtc.Id),
                        new XElement("SHORT-NAME", $"DTC_{SanitizeId(dtc.DisplayCode)}"),
                        new XElement("TROUBLE-CODE", dtc.TroubleCode),
                        new XElement("DISPLAY-TROUBLE-CODE", dtc.DisplayCode),
                        new XElement("TEXT", dtc.Description));
                    if (!string.IsNullOrWhiteSpace(dtc.Severity))
                        dEl.Add(new XElement("SEVERITY", dtc.Severity));
                    dtcsEl.Add(dEl);
                }
                dictEl.Add(new XElement("DTC-DOPS",
                    new XElement("DTC-DOP",
                        new XAttribute("ID", $"DTC_DOP_{SanitizeId(layer.ShortName)}"),
                        new XElement("SHORT-NAME", $"DTC_DOP_{SanitizeId(layer.ShortName)}"),
                        dtcsEl)));
            }

            layerEl.Add(dictEl);
        }

        // DIAG-COMMS & NEG-RESPONSES (Own non-inherited Services + any unlinked Own DIDs)
        var ownServices = layer.Services.Where(s => !s.IsInherited).ToList();
        foreach (var did in layer.DataIdentifiers.Where(d => !d.IsInherited))
        {
            bool hasReadSvc = ownServices.Any(s => string.Equals(s.ServiceIdHex, "0x22", StringComparison.OrdinalIgnoreCase) &&
                s.RequestParameters.Any(p => string.Equals(p.DefaultValueHex, did.DidHex, StringComparison.OrdinalIgnoreCase)));
            bool hasWriteSvc = ownServices.Any(s => string.Equals(s.ServiceIdHex, "0x2E", StringComparison.OrdinalIgnoreCase) &&
                s.RequestParameters.Any(p => string.Equals(p.DefaultValueHex, did.DidHex, StringComparison.OrdinalIgnoreCase)));

            var access = did.Access ?? "Read";
            if (!hasReadSvc && access.Contains("Read", StringComparison.OrdinalIgnoreCase))
                ownServices.Add(BuildSyntheticDidService(did, isWrite: false, layer.ShortName));
            if (!hasWriteSvc && access.Contains("Write", StringComparison.OrdinalIgnoreCase))
                ownServices.Add(BuildSyntheticDidService(did, isWrite: true, layer.ShortName));
        }

        if (ownServices.Any())
        {
            var layerNegResponses = new List<XElement>();
            var commsEl = new XElement("DIAG-COMMS");
            foreach (var svc in ownServices)
            {
                commsEl.Add(BuildServiceElement(svc, layer.ShortName));
                var svcId = string.IsNullOrWhiteSpace(svc.Id) ? $"SVC_{SanitizeId(svc.ShortName)}" : svc.Id;
                for (int i = 0; i < svc.NegativeResponses.Count; i++)
                {
                    var nrId = $"NR_{SanitizeId(layer.ShortName)}_{SanitizeId(svcId)}_{i + 1}";
                    layerNegResponses.Add(BuildNegResponseElement(nrId, svc.NegativeResponses[i]));
                }
            }
            if (layerNegResponses.Any())
                layerEl.Add(new XElement("NEG-RESPONSES", layerNegResponses));
            layerEl.Add(commsEl);
        }

        // COMPARAM-REFS (Own ComParams)
        var ownComParams = layer.CommunicationParameters.Where(c => !c.IsInherited).ToList();
        if (ownComParams.Any())
        {
            var cpRefsEl = new XElement("COMPARAM-REFS");
            foreach (var cp in ownComParams)
            {
                var cpEl = new XElement("COMPARAM-REF",
                    new XAttribute("ID-REF", cp.Name),
                    new XElement("SHORT-NAME", cp.Name),
                    new XElement("SIMPLE-VALUE", cp.Value));
                if (!string.IsNullOrWhiteSpace(cp.Unit))
                    cpEl.Add(new XElement("UNIT", cp.Unit));
                if (!string.IsNullOrWhiteSpace(cp.Description))
                    cpEl.Add(new XElement("DESC", new XElement("P", cp.Description)));
                cpRefsEl.Add(cpEl);
            }
            layerEl.Add(cpRefsEl);
        }

        return layerEl;
    }

    private static XElement BuildDopElement(DataObjectProperty dop, List<PhysicalUnitInfo> availableUnits)
    {
        var category = string.IsNullOrWhiteSpace(dop.Category) ? (dop.Factor != 1.0 || dop.Offset != 0.0 ? "LINEAR" : "IDENTICAL") : dop.Category;
        var dopId = string.IsNullOrWhiteSpace(dop.Id) ? SanitizeId(dop.ShortName) : dop.Id;

        var compuMethodEl = new XElement("COMPU-METHOD", new XElement("CATEGORY", category));
        if (string.Equals(category, "LINEAR", StringComparison.OrdinalIgnoreCase))
        {
            compuMethodEl.Add(new XElement("COMPU-INTERNAL-TO-PHYS",
                new XElement("COMPU-SCALES",
                    new XElement("COMPU-SCALE",
                        new XElement("COMPU-RATIONAL-COEFFS",
                            new XElement("COMPU-NUMERATOR",
                                new XElement("V", dop.Offset.ToString(CultureInfo.InvariantCulture)),
                                new XElement("V", dop.Factor.ToString(CultureInfo.InvariantCulture))),
                            new XElement("COMPU-DENOMINATOR", new XElement("V", "1")))))));
        }

        var dopEl = new XElement("DATA-OBJECT-PROP",
            new XAttribute("ID", dopId),
            new XAttribute("CATEGORY", category),
            new XElement("SHORT-NAME", dop.ShortName),
            compuMethodEl,
            new XElement("DIAG-CODED-TYPE",
                new XAttribute("BASE-DATA-TYPE", string.IsNullOrWhiteSpace(dop.BaseDataType) ? "A_UINT32" : dop.BaseDataType),
                new XAttribute("BIT-LENGTH", dop.BitLength > 0 ? dop.BitLength : 8)));

        if (!string.IsNullOrWhiteSpace(dop.LongName))
            dopEl.Add(new XElement("LONG-NAME", dop.LongName));

        if (!string.IsNullOrWhiteSpace(dop.PhysicalUnit))
        {
            var matchedUnit = availableUnits.FirstOrDefault(u =>
                u.DisplayName == dop.PhysicalUnit || u.ShortName == dop.PhysicalUnit || u.Id == dop.PhysicalUnit);
            var unitRefId = matchedUnit?.Id ?? $"UNIT_{SanitizeId(dop.PhysicalUnit)}";
            dopEl.Add(new XElement("UNIT-REF", new XAttribute("ID-REF", unitRefId)));
        }

        return dopEl;
    }

    private static XElement BuildServiceElement(DiagnosticService svc, string layerShortName)
    {
        var svcId = string.IsNullOrWhiteSpace(svc.Id) ? $"SVC_{SanitizeId(svc.ShortName)}" : svc.Id;
        var svcEl = new XElement("DIAG-SERVICE",
            new XAttribute("ID", svcId),
            new XAttribute("SEMANTIC", string.IsNullOrWhiteSpace(svc.Semantic) ? "DATA-ID" : svc.Semantic),
            new XAttribute("ADDRESSING", (svc.Addressing ?? "PHYSICAL").ToUpperInvariant().Replace("/", "-AND-")),
            new XElement("SHORT-NAME", svc.ShortName));

        if (!string.IsNullOrWhiteSpace(svc.LongName))
            svcEl.Add(new XElement("LONG-NAME", svc.LongName));
        if (!string.IsNullOrWhiteSpace(svc.Description))
            svcEl.Add(new XElement("DESC", new XElement("P", svc.Description)));

        // Request
        var reqParamsEl = new XElement("PARAMS");
        foreach (var p in svc.RequestParameters)
            reqParamsEl.Add(BuildParamElement(p));

        svcEl.Add(new XElement("REQUEST",
            new XElement("SHORT-NAME", $"RQ_{SanitizeId(svc.ShortName)}"),
            reqParamsEl));

        // Positive Response
        if (svc.ResponseParameters.Any() || !string.IsNullOrWhiteSpace(svc.PositiveResponseSidHex))
        {
            var respParamsEl = new XElement("PARAMS");
            if (svc.ResponseParameters.Any())
            {
                foreach (var p in svc.ResponseParameters)
                    respParamsEl.Add(BuildParamElement(p));
            }
            else if (!string.IsNullOrWhiteSpace(svc.PositiveResponseSidHex))
            {
                respParamsEl.Add(BuildParamElement(new DiagnosticParameter
                {
                    Name = "ResponseSID",
                    Semantic = "SERVICE-ID",
                    ParameterType = "CODED-CONST",
                    BytePosition = 0,
                    DefaultValueHex = svc.PositiveResponseSidHex
                }));
            }

            svcEl.Add(new XElement("POS-RESPONSES",
                new XElement("POS-RESPONSE",
                    new XAttribute("ID", $"PR_{SanitizeId(svcId)}"),
                    new XElement("SHORT-NAME", $"PR_{SanitizeId(svc.ShortName)}"),
                    respParamsEl)));
        }

        // Negative Response Refs (ISO 22901-1 <NEG-RESPONSE-REFS>)
        if (svc.NegativeResponses.Any())
        {
            var nrRefsEl = new XElement("NEG-RESPONSE-REFS");
            for (int i = 0; i < svc.NegativeResponses.Count; i++)
            {
                var nrId = $"NR_{SanitizeId(layerShortName)}_{SanitizeId(svcId)}_{i + 1}";
                nrRefsEl.Add(new XElement("NEG-RESPONSE-REF", new XAttribute("ID-REF", nrId)));
            }
            svcEl.Add(nrRefsEl);
        }

        return svcEl;
    }

    private static XElement BuildNegResponseElement(string nrId, string rawNrc)
    {
        var trimmed = (rawNrc ?? string.Empty).Trim();
        var parts = trimmed.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        var first = parts.Length > 0 ? parts[0] : string.Empty;
        bool hasHexCode = first.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                          (first.Length == 2 && int.TryParse(first, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _));

        if (hasHexCode)
        {
            var code = first.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? first : $"0x{first.ToUpperInvariant()}";
            var shortName = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
                ? parts[1].Trim()
                : $"NRC_{code}";

            return new XElement("NEG-RESPONSE",
                new XAttribute("ID", nrId),
                new XElement("SHORT-NAME", shortName),
                new XElement("PARAMS",
                    new XElement("PARAM",
                        new XAttribute("SEMANTIC", "NRC"),
                        new XAttribute(XsiNs + "type", "CODED-CONST"),
                        new XAttribute("TYPE", "CODED-CONST"),
                        new XElement("SHORT-NAME", "NRC"),
                        new XElement("BYTE-POSITION", 2),
                        new XElement("CODED-VALUE", code))));
        }

        return new XElement("NEG-RESPONSE",
            new XAttribute("ID", nrId),
            new XElement("SHORT-NAME", string.IsNullOrWhiteSpace(trimmed) ? nrId : trimmed));
    }

    private static DiagnosticService BuildSyntheticDidService(DiagnosticDataIdentifier did, bool isWrite, string layerShortName)
    {
        string sid = isWrite ? "0x2E" : "0x22";
        string prSid = isWrite ? "0x6E" : "0x62";
        string prefix = isWrite ? "Write" : "Read";
        int bitLen = did.BitLength > 0 ? did.BitLength : Math.Max(1, did.ByteLength) * 8;

        var svc = new DiagnosticService
        {
            Id = $"SVC_{prefix}_{SanitizeId(did.ShortName)}_{SanitizeId(did.DidHex)}",
            ShortName = $"{prefix}_{did.ShortName}",
            LongName = string.IsNullOrWhiteSpace(did.LongName) ? $"{prefix} {did.ShortName}" : did.LongName,
            Description = did.Description,
            Semantic = did.DataCategory == "Stored Data" ? "IDENTIFICATION" : "CURRENT-DATA",
            DataCategory = did.DataCategory,
            ServiceIdHex = sid,
            PositiveResponseSidHex = prSid,
            Addressing = "Physical",
            DefinedInLayer = layerShortName,
            RequestParameters =
            {
                new DiagnosticParameter { Name = "SID", Semantic = "SERVICE-ID", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = sid },
                new DiagnosticParameter { Name = "DID", Semantic = "ID", ParameterType = "CODED-CONST", BytePosition = 1, BitLength = 16, DefaultValueHex = did.DidHex }
            },
            ResponseParameters =
            {
                new DiagnosticParameter { Name = "ResponseSID", Semantic = "SERVICE-ID", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = prSid },
                new DiagnosticParameter { Name = "DID_PR", Semantic = "ID", ParameterType = "CODED-CONST", BytePosition = 1, BitLength = 16, DefaultValueHex = did.DidHex }
            }
        };

        var payloadParam = new DiagnosticParameter
        {
            Name = did.ShortName,
            LongName = did.LongName,
            Semantic = "DATA",
            ParameterType = "VALUE",
            BytePosition = 3,
            BitLength = bitLen,
            DataType = did.DataType,
            DopRef = did.DopRef
        };

        if (isWrite)
            svc.RequestParameters.Add(payloadParam);
        else
            svc.ResponseParameters.Add(payloadParam);

        return svc;
    }

    private static XElement BuildParamElement(DiagnosticParameter p)
    {
        var pType = string.IsNullOrWhiteSpace(p.ParameterType) ? "VALUE" : p.ParameterType;
        var pEl = new XElement("PARAM",
            new XAttribute("SEMANTIC", string.IsNullOrWhiteSpace(p.Semantic) ? "DATA" : p.Semantic),
            new XAttribute(XsiNs + "type", pType),
            new XAttribute("TYPE", pType),
            new XElement("SHORT-NAME", string.IsNullOrWhiteSpace(p.Name) ? "Param" : p.Name),
            new XElement("BYTE-POSITION", p.BytePosition));

        if (p.BitPosition > 0)
            pEl.Add(new XElement("BIT-POSITION", p.BitPosition));
        if (p.BitLength > 0)
            pEl.Add(new XElement("BIT-LENGTH", p.BitLength));
        if (!string.IsNullOrWhiteSpace(p.DefaultValueHex))
            pEl.Add(new XElement("CODED-VALUE", p.DefaultValueHex));
        if (!string.IsNullOrWhiteSpace(p.DopRef))
            pEl.Add(new XElement("DOP-REF", new XAttribute("ID-REF", p.DopRef)));

        return pEl;
    }

    private static XElement BuildComParamSpecElement(ComParamSpecInfo spec)
    {
        var stacksEl = new XElement("PROT-STACKS");
        foreach (var st in spec.ProtStacks)
        {
            stacksEl.Add(new XElement("PROT-STACK",
                new XAttribute("ID", st.Id),
                new XElement("SHORT-NAME", st.ShortName),
                new XElement("PDU-PROTOCOL-TYPE", st.PduProtocolType),
                new XElement("PHYSICAL-LINK-TYPE", st.PhysicalLinkType)));
        }

        var cpSubsetEl = new XElement("COMPARAMS");
        foreach (var cp in spec.DefaultComParams)
        {
            cpSubsetEl.Add(new XElement("COMPARAM",
                new XAttribute("ID", SanitizeId(cp.Name)),
                new XAttribute("PARAM-CLASS", string.IsNullOrWhiteSpace(cp.ParamClass) ? "TIMING" : cp.ParamClass),
                new XElement("SHORT-NAME", cp.Name),
                new XElement("PHYSICAL-DEFAULT-VALUE", cp.Value)));
        }

        return new XElement("COMPARAM-SPEC",
            new XAttribute("ID", string.IsNullOrWhiteSpace(spec.Id) ? SanitizeId(spec.ShortName) : spec.Id),
            new XElement("SHORT-NAME", spec.ShortName),
            stacksEl,
            new XElement("COMPARAM-SUBSET",
                new XAttribute("ID", $"CPSS_{SanitizeId(spec.ShortName)}"),
                new XElement("SHORT-NAME", $"CPSS_{SanitizeId(spec.ShortName)}"),
                cpSubsetEl));
    }

    private static XElement BuildVehicleInfoSpecElement(VehicleInfoSpec vi)
    {
        var pvlsEl = new XElement("PHYSICAL-VEHICLE-LINKS");
        foreach (var pvl in vi.PhysicalVehicleLinks)
        {
            pvlsEl.Add(new XElement("PHYSICAL-VEHICLE-LINK",
                new XAttribute("ID", pvl.Id),
                new XAttribute("TYPE", pvl.LinkType),
                new XElement("SHORT-NAME", pvl.ShortName)));
        }

        var llsEl = new XElement("LOGICAL-LINKS");
        foreach (var ll in vi.LogicalLinks)
        {
            var roleTag = string.IsNullOrWhiteSpace(ll.LinkRole) ? "GATEWAY-LOGICAL-LINK" : ll.LinkRole;
            var llEl = new XElement(roleTag,
                new XAttribute("ID", ll.Id),
                new XElement("SHORT-NAME", ll.ShortName));

            if (!string.IsNullOrWhiteSpace(ll.PhysicalVehicleLinkRef))
                llEl.Add(new XElement("PHYSICAL-VEHICLE-LINK-REF", new XAttribute("ID-REF", ll.PhysicalVehicleLinkRef)));
            if (!string.IsNullOrWhiteSpace(ll.ProtocolRef))
                llEl.Add(new XElement("PROTOCOL-REF", new XAttribute("ID-REF", ll.ProtocolRef)));
            if (!string.IsNullOrWhiteSpace(ll.BaseVariantRef))
                llEl.Add(new XElement("BASE-VARIANT-REF", new XAttribute("ID-REF", ll.BaseVariantRef)));
            if (!string.IsNullOrWhiteSpace(ll.FunctionalGroupRef))
                llEl.Add(new XElement("FUNCTIONAL-GROUP-REF", new XAttribute("ID-REF", ll.FunctionalGroupRef)));

            if (ll.LinkComParams.Any())
            {
                var cpEl = new XElement("LINK-COMPARAM-REFS");
                foreach (var cp in ll.LinkComParams)
                {
                    cpEl.Add(new XElement("LINK-COMPARAM-REF",
                        new XAttribute("ID-REF", cp.Name),
                        new XElement("SHORT-NAME", cp.Name),
                        new XElement("SIMPLE-VALUE", cp.Value)));
                }
                llEl.Add(cpEl);
            }

            llsEl.Add(llEl);
        }

        return new XElement("VEHICLE-INFO-SPEC",
            new XAttribute("ID", $"VIS_{SanitizeId(vi.ShortName)}"),
            new XElement("SHORT-NAME", $"VIS_{SanitizeId(vi.ShortName)}"),
            new XElement("VEHICLE-INFORMATIONS",
                new XElement("VEHICLE-INFORMATION",
                    new XAttribute("ID", string.IsNullOrWhiteSpace(vi.Id) ? SanitizeId(vi.ShortName) : vi.Id),
                    new XElement("SHORT-NAME", vi.ShortName),
                    new XElement("MODEL", vi.Model),
                    new XElement("COMPANY", vi.Company),
                    new XElement("MODEL-YEAR", vi.ModelYear),
                    pvlsEl,
                    llsEl)));
    }

    private static string SanitizeId(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "ID_" + Guid.NewGuid().ToString("N")[..6];
        var sb = new StringBuilder();
        foreach (char c in input)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }
        return sb.ToString();
    }

    private static void WriteZipEntry(ZipArchive archive, string entryPath, string content)
    {
        var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
