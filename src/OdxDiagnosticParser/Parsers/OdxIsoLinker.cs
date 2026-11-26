using OdxDiagnosticParser.Models;

namespace OdxDiagnosticParser.Parsers;

/// <summary>
/// Resolves ISO 22901-1 (ODX) &amp; ISO 22901-2 (PDX) cross-file / cross-layer references,
/// PARENT-REFS diagnostic layer inheritance, ODX-C ComParam-Spec attachments, ODX-V Logical Links,
/// and validates package consistency.
/// </summary>
public static class OdxIsoLinker
{
    /// <summary>
    /// Strips previously computed inherited items and re-evaluates all ISO 22901-1 PARENT-REF
    /// and ODX-C / ODX-V links from scratch after live authoring changes.
    /// </summary>
    public static void RelinkPackage(DiagnosticPackage package)
    {
        if (package == null) return;

        foreach (var layer in package.Layers)
        {
            layer.Services.RemoveAll(s => s.IsInherited);
            layer.DataIdentifiers.RemoveAll(d => d.IsInherited);
            layer.DiagnosticTroubleCodes.RemoveAll(t => t.IsInherited);
            layer.DataObjectProperties.RemoveAll(d => d.IsInherited);
            layer.CommunicationParameters.RemoveAll(c => c.IsInherited);
            layer.InheritanceResolved = false;
        }

        LinkPackage(package);
    }

    public static void LinkPackage(DiagnosticPackage package)
    {
        if (package == null) return;

        package.ConsistencyIssues.Clear();

        // Build global indices
        var layersById = new Dictionary<string, DiagnosticLayer>(StringComparer.OrdinalIgnoreCase);
        var layersByShortName = new Dictionary<string, DiagnosticLayer>(StringComparer.OrdinalIgnoreCase);
        var layersByDocAndId = new Dictionary<(string DocRef, string Id), DiagnosticLayer>(TupleIgnoreCaseComparer.Instance);
        var globalUnits = new Dictionary<string, PhysicalUnitInfo>(StringComparer.OrdinalIgnoreCase);
        var globalDops = new Dictionary<string, DataObjectProperty>(StringComparer.OrdinalIgnoreCase);

        foreach (var layer in package.Layers)
        {
            if (!string.IsNullOrEmpty(layer.Id) && !layersById.ContainsKey(layer.Id))
                layersById[layer.Id] = layer;

            if (!string.IsNullOrEmpty(layer.ShortName) && !layersByShortName.ContainsKey(layer.ShortName))
                layersByShortName[layer.ShortName] = layer;

            if (!string.IsNullOrEmpty(layer.ContainerShortName) && !string.IsNullOrEmpty(layer.Id))
                layersByDocAndId[(layer.ContainerShortName, layer.Id)] = layer;

            foreach (var u in layer.Units)
            {
                if (!string.IsNullOrEmpty(u.Id) && !globalUnits.ContainsKey(u.Id))
                    globalUnits[u.Id] = u;
            }

            foreach (var dop in layer.DataObjectProperties)
            {
                if (!string.IsNullOrEmpty(dop.Id) && !globalDops.ContainsKey(dop.Id))
                    globalDops[dop.Id] = dop;
            }
        }

        // 1. Resolve global unit references & cross-layer DOP enrichments
        ResolveUnitsAndCrossLayerDops(package, globalUnits, globalDops);

        // 2. Connect ODX-C ComParamSpecs to PROTOCOL layers
        ConnectComParamSpecsToProtocols(package);

        // 3. Resolve PARENT-REF targets across all layers (including cross-file DOCREF resolution)
        foreach (var layer in package.Layers)
        {
            foreach (var pref in layer.ParentRefs)
            {
                var target = FindTargetLayer(pref, layersByDocAndId, layersById, layersByShortName);
                if (target != null && !ReferenceEquals(target, layer))
                {
                    pref.IsResolved = true;
                    pref.ResolvedLayerId = target.Id;
                    pref.ResolvedLayerShortName = target.ShortName;
                    pref.ResolvedLayerType = target.LayerType.ToString();
                }
                else
                {
                    pref.IsResolved = false;
                    package.ConsistencyIssues.Add(new OdxConsistencyIssue
                    {
                        Severity = "Warning",
                        StandardRule = "ISO 22901-1 Section 7.2 (PARENT-REF)",
                        SourceObject = layer.ShortName,
                        Message = $"Unresolved PARENT-REF target '{pref.DisplayTarget}' (DOCREF='{pref.DocRef}', ID-REF='{pref.IdRef}')."
                    });
                }
            }
        }

        // 4. Stamp origin layer name on own entities before inheritance copy
        foreach (var layer in package.Layers)
        {
            foreach (var svc in layer.Services.Where(s => !s.IsInherited))
            {
                if (string.IsNullOrEmpty(svc.DefinedInLayer))
                    svc.DefinedInLayer = layer.ShortName;
                if (string.IsNullOrEmpty(svc.DefinedInLayerType))
                    svc.DefinedInLayerType = layer.LayerType.ToString();
            }

            foreach (var did in layer.DataIdentifiers.Where(d => !d.IsInherited))
            {
                if (string.IsNullOrEmpty(did.DefinedInLayer))
                    did.DefinedInLayer = layer.ShortName;
            }

            foreach (var dtc in layer.DiagnosticTroubleCodes.Where(t => !t.IsInherited))
            {
                if (string.IsNullOrEmpty(dtc.DefinedInLayer))
                    dtc.DefinedInLayer = layer.ShortName;
            }

            foreach (var cp in layer.CommunicationParameters.Where(c => !c.IsInherited))
            {
                if (string.IsNullOrEmpty(cp.DefinedInLayer))
                    cp.DefinedInLayer = layer.ShortName;
            }
        }

        // 5. Propagate ISO 22901-1 inheritance recursively in topological parent-first order
        var resolvedSet = new HashSet<DiagnosticLayer>();
        var visitingSet = new HashSet<DiagnosticLayer>();

        foreach (var layer in package.Layers)
        {
            ResolveLayerInheritanceRecursive(layer, layersById, layersByDocAndId, layersByShortName, resolvedSet, visitingSet, package.ConsistencyIssues);
        }

        // 6. Connect ODX-V VehicleInfos & Logical Links to Protocol / BaseVariant / FunctionalGroup layers
        ConnectVehicleTopology(package, layersByDocAndId, layersById, layersByShortName);

        // 7. Run final ISO consistency checks
        ValidateConsistency(package);
    }

    private static void ResolveUnitsAndCrossLayerDops(
        DiagnosticPackage package,
        Dictionary<string, PhysicalUnitInfo> globalUnits,
        Dictionary<string, DataObjectProperty> globalDops)
    {
        foreach (var dop in package.Layers.SelectMany(l => l.DataObjectProperties))
        {
            if (!string.IsNullOrEmpty(dop.PhysicalUnit) && globalUnits.TryGetValue(dop.PhysicalUnit, out var unitInfo))
            {
                dop.PhysicalUnit = !string.IsNullOrEmpty(unitInfo.DisplayName) ? unitInfo.DisplayName : unitInfo.ShortName;
            }
        }

        foreach (var layer in package.Layers)
        {
            foreach (var svc in layer.Services)
            {
                foreach (var param in svc.RequestParameters.Concat(svc.ResponseParameters))
                {
                    if (!string.IsNullOrEmpty(param.DopRef) && globalDops.TryGetValue(param.DopRef, out var dop))
                    {
                        if (string.IsNullOrEmpty(param.DataType))
                            param.DataType = dop.BaseDataType;
                        if (param.BitLength == 0)
                            param.BitLength = dop.BitLength;
                    }
                }
            }

            var servicesByShortName = new Dictionary<string, DiagnosticService>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in layer.Services)
            {
                if (!string.IsNullOrEmpty(s.ShortName) && !servicesByShortName.ContainsKey(s.ShortName))
                    servicesByShortName[s.ShortName] = s;
            }

            foreach (var did in layer.DataIdentifiers)
            {
                if (!string.IsNullOrEmpty(did.Unit) && globalUnits.TryGetValue(did.Unit, out var u))
                {
                    did.Unit = !string.IsNullOrEmpty(u.DisplayName) ? u.DisplayName : u.ShortName;
                }

                // If DID DataType/Unit wasn't resolved locally, check DopRef directly or global DOPs via AssociatedService params
                if (string.IsNullOrEmpty(did.Unit) || string.IsNullOrEmpty(did.DataType))
                {
                    DataObjectProperty? dop = null;
                    string resolvedDopRef = did.DopRef;

                    if (!string.IsNullOrEmpty(did.DopRef))
                        globalDops.TryGetValue(did.DopRef, out dop);

                    if (dop == null && !string.IsNullOrEmpty(did.AssociatedService) &&
                        servicesByShortName.TryGetValue(did.AssociatedService, out var assocSvc))
                    {
                        var payloadParam = assocSvc.ResponseParameters.FirstOrDefault(p => !string.IsNullOrEmpty(p.DopRef))
                                           ?? assocSvc.RequestParameters.FirstOrDefault(p => !string.IsNullOrEmpty(p.DopRef));
                        if (payloadParam != null && globalDops.TryGetValue(payloadParam.DopRef, out dop))
                            resolvedDopRef = payloadParam.DopRef;
                    }

                    if (dop != null)
                    {
                        if (string.IsNullOrEmpty(did.DopRef)) did.DopRef = resolvedDopRef;
                        if (string.IsNullOrEmpty(did.DataType)) did.DataType = dop.BaseDataType;
                        if (string.IsNullOrEmpty(did.Unit)) did.Unit = dop.PhysicalUnit;
                        if (did.ScalingFactor == 1.0 && dop.Factor != 1.0) did.ScalingFactor = dop.Factor;
                        if (did.Offset == 0.0 && dop.Offset != 0.0) did.Offset = dop.Offset;
                        if (did.BitLength == 0 && dop.BitLength > 0)
                        {
                            did.BitLength = dop.BitLength;
                            did.ByteLength = (dop.BitLength + 7) / 8;
                        }
                    }
                }
            }
        }
    }

    private static void ConnectComParamSpecsToProtocols(DiagnosticPackage package)
    {
        if (package.ComParamSpecs.Count == 0) return;

        foreach (var proto in package.Layers.Where(l => l.LayerType == DiagnosticLayerType.Protocol))
        {
            ComParamSpecInfo? matchedSpec = null;
            if (!string.IsNullOrEmpty(proto.ComParamSpecRef))
            {
                matchedSpec = package.ComParamSpecs.FirstOrDefault(c =>
                    c.Id.Equals(proto.ComParamSpecRef, StringComparison.OrdinalIgnoreCase) ||
                    c.ShortName.Equals(proto.ComParamSpecRef, StringComparison.OrdinalIgnoreCase));
            }
            if (matchedSpec == null && !string.IsNullOrEmpty(proto.ComParamSpecDocRef))
            {
                matchedSpec = package.ComParamSpecs.FirstOrDefault(c =>
                    c.ShortName.Equals(proto.ComParamSpecDocRef, StringComparison.OrdinalIgnoreCase));
            }
            if (matchedSpec == null && package.ComParamSpecs.Count == 1)
            {
                matchedSpec = package.ComParamSpecs[0];
            }

            if (matchedSpec != null)
            {
                foreach (var cp in matchedSpec.DefaultComParams)
                {
                    if (!proto.CommunicationParameters.Any(existing =>
                            existing.Name.Equals(cp.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        proto.CommunicationParameters.Add(cp.CloneAsInherited(matchedSpec.ShortName, proto.ShortName));
                    }
                }
            }
        }
    }

    private static DiagnosticLayer? FindTargetLayer(
        LayerParentRef pref,
        Dictionary<(string DocRef, string Id), DiagnosticLayer> layersByDocAndId,
        Dictionary<string, DiagnosticLayer> layersById,
        Dictionary<string, DiagnosticLayer> layersByShortName)
    {
        if (!string.IsNullOrEmpty(pref.DocRef) && !string.IsNullOrEmpty(pref.IdRef) &&
            layersByDocAndId.TryGetValue((pref.DocRef, pref.IdRef), out var byDocAndId))
        {
            return byDocAndId;
        }

        if (!string.IsNullOrEmpty(pref.IdRef) && layersById.TryGetValue(pref.IdRef, out var byId))
            return byId;

        if (!string.IsNullOrEmpty(pref.ShortNameRef) && layersByShortName.TryGetValue(pref.ShortNameRef, out var bySn))
            return bySn;

        if (!string.IsNullOrEmpty(pref.IdRef) && layersByShortName.TryGetValue(pref.IdRef, out var byIdAsSn))
            return byIdAsSn;

        return null;
    }

    private static void ResolveLayerInheritanceRecursive(
        DiagnosticLayer layer,
        Dictionary<string, DiagnosticLayer> layersById,
        Dictionary<(string DocRef, string Id), DiagnosticLayer> layersByDocAndId,
        Dictionary<string, DiagnosticLayer> layersByShortName,
        HashSet<DiagnosticLayer> resolvedSet,
        HashSet<DiagnosticLayer> visitingSet,
        List<OdxConsistencyIssue> issues)
    {
        if (resolvedSet.Contains(layer))
            return;

        if (!visitingSet.Add(layer))
        {
            issues.Add(new OdxConsistencyIssue
            {
                Severity = "Error",
                StandardRule = "ISO 22901-1 Cyclic Inheritance",
                SourceObject = layer.ShortName,
                Message = $"Circular PARENT-REF inheritance detected involving layer '{layer.ShortName}'."
            });
            return;
        }

        var existingServices = new HashSet<string>(layer.Services.Select(s => s.ShortName), StringComparer.OrdinalIgnoreCase);
        var existingDidsByHex = new Dictionary<string, DiagnosticDataIdentifier>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in layer.DataIdentifiers)
        {
            if (!string.IsNullOrEmpty(d.DidHex) && !existingDidsByHex.ContainsKey(d.DidHex))
                existingDidsByHex[d.DidHex] = d;
        }
        var existingDtcDisplay = new HashSet<string>(
            layer.DiagnosticTroubleCodes.Where(t => !string.IsNullOrEmpty(t.DisplayCode)).Select(t => t.DisplayCode),
            StringComparer.OrdinalIgnoreCase);
        var existingDtcHex = new HashSet<string>(
            layer.DiagnosticTroubleCodes.Where(t => !string.IsNullOrEmpty(t.TroubleCodeHex)).Select(t => t.TroubleCodeHex),
            StringComparer.OrdinalIgnoreCase);
        var existingDopIds = new HashSet<string>(
            layer.DataObjectProperties.Where(d => !string.IsNullOrEmpty(d.Id)).Select(d => d.Id),
            StringComparer.OrdinalIgnoreCase);
        var existingCpNames = new HashSet<string>(
            layer.CommunicationParameters.Where(c => !string.IsNullOrEmpty(c.Name)).Select(c => c.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (var pref in layer.ParentRefs)
        {
            var parentLayer = FindTargetLayer(pref, layersByDocAndId, layersById, layersByShortName);
            if (parentLayer == null || ReferenceEquals(parentLayer, layer))
                continue;

            // Resolve ancestors first
            ResolveLayerInheritanceRecursive(parentLayer, layersById, layersByDocAndId, layersByShortName, resolvedSet, visitingSet, issues);

            var excludedComms = pref.NotInheritedDiagComms.Count > 0
                ? new HashSet<string>(pref.NotInheritedDiagComms, StringComparer.OrdinalIgnoreCase)
                : null;

            // Inherit services (child overrides if same ShortName already defined)
            foreach (var parentSvc in parentLayer.Services)
            {
                if (excludedComms != null && excludedComms.Contains(parentSvc.ShortName))
                    continue;

                if (existingServices.Add(parentSvc.ShortName))
                {
                    layer.Services.Add(parentSvc.CloneAsInherited(parentLayer.ShortName, parentLayer.LayerType.ToString()));
                }
            }

            // Inherit DIDs
            foreach (var parentDid in parentLayer.DataIdentifiers)
            {
                if (excludedComms != null && !string.IsNullOrEmpty(parentDid.AssociatedService) && excludedComms.Contains(parentDid.AssociatedService))
                    continue;

                if (!string.IsNullOrEmpty(parentDid.DidHex) && existingDidsByHex.TryGetValue(parentDid.DidHex, out var existingDid))
                {
                    // Merge Read + Write if split between BaseVariant and EcuVariant
                    if (existingDid.Access != parentDid.Access && !existingDid.Access.Contains('/'))
                    {
                        existingDid.Access = "Read/Write";
                    }
                }
                else
                {
                    var clonedDid = parentDid.CloneAsInherited(parentLayer.ShortName);
                    layer.DataIdentifiers.Add(clonedDid);
                    if (!string.IsNullOrEmpty(clonedDid.DidHex))
                        existingDidsByHex[clonedDid.DidHex] = clonedDid;
                }
            }

            // Inherit DTCs
            foreach (var parentDtc in parentLayer.DiagnosticTroubleCodes)
            {
                bool exists = (!string.IsNullOrEmpty(parentDtc.DisplayCode) && existingDtcDisplay.Contains(parentDtc.DisplayCode)) ||
                              (!string.IsNullOrEmpty(parentDtc.TroubleCodeHex) && existingDtcHex.Contains(parentDtc.TroubleCodeHex));

                if (!exists)
                {
                    layer.DiagnosticTroubleCodes.Add(parentDtc.CloneAsInherited(parentLayer.ShortName));
                    if (!string.IsNullOrEmpty(parentDtc.DisplayCode)) existingDtcDisplay.Add(parentDtc.DisplayCode);
                    if (!string.IsNullOrEmpty(parentDtc.TroubleCodeHex)) existingDtcHex.Add(parentDtc.TroubleCodeHex);
                }
            }

            // Inherit DOPs
            var excludedDops = pref.NotInheritedDops.Count > 0
                ? new HashSet<string>(pref.NotInheritedDops, StringComparer.OrdinalIgnoreCase)
                : null;
            foreach (var parentDop in parentLayer.DataObjectProperties)
            {
                if (excludedDops != null && excludedDops.Contains(parentDop.ShortName))
                    continue;

                if (string.IsNullOrEmpty(parentDop.Id) || existingDopIds.Add(parentDop.Id))
                {
                    layer.DataObjectProperties.Add(parentDop.CloneAsInherited(parentLayer.ShortName));
                }
            }

            // Inherit Communication Parameters (most specific layer overrides)
            foreach (var parentCp in parentLayer.CommunicationParameters)
            {
                if (string.IsNullOrEmpty(parentCp.Name) || existingCpNames.Add(parentCp.Name))
                {
                    string protoName = parentLayer.LayerType == DiagnosticLayerType.Protocol ? parentLayer.ShortName : parentCp.Protocol;
                    layer.CommunicationParameters.Add(parentCp.CloneAsInherited(parentLayer.ShortName, protoName));
                }
            }
        }

        visitingSet.Remove(layer);
        layer.InheritanceResolved = true;
        resolvedSet.Add(layer);
    }

    private static void ConnectVehicleTopology(
        DiagnosticPackage package,
        Dictionary<(string DocRef, string Id), DiagnosticLayer> layersByDocAndId,
        Dictionary<string, DiagnosticLayer> layersById,
        Dictionary<string, DiagnosticLayer> layersByShortName)
    {
        foreach (var vi in package.VehicleInfos)
        {
            var pvlById = vi.PhysicalVehicleLinks.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var link in vi.LogicalLinks)
            {
                if (!string.IsNullOrEmpty(link.PhysicalVehicleLinkRef) && pvlById.TryGetValue(link.PhysicalVehicleLinkRef, out var pvl))
                {
                    link.PhysicalVehicleLinkShortName = pvl.ShortName;
                }

                // Resolve Protocol
                var proto = ResolveLinkLayer(link.ProtocolDocRef, link.ProtocolRef, layersByDocAndId, layersById, layersByShortName);
                if (proto != null)
                {
                    link.ResolvedProtocolName = proto.ShortName;
                }
                else if (!string.IsNullOrEmpty(link.ProtocolRef))
                {
                    package.ConsistencyIssues.Add(new OdxConsistencyIssue
                    {
                        Severity = "Warning",
                        StandardRule = "ISO 22901-1 ODX-V LOGICAL-LINK",
                        SourceObject = link.ShortName,
                        Message = $"Logical Link '{link.ShortName}' references missing PROTOCOL '{link.ProtocolRef}'."
                    });
                }

                // Resolve BaseVariant
                var bv = ResolveLinkLayer(link.BaseVariantDocRef, link.BaseVariantRef, layersByDocAndId, layersById, layersByShortName);
                if (bv != null)
                {
                    link.ResolvedBaseVariantName = bv.ShortName;

                    // Propagate LogicalLink ComParams (like TX/RX CAN ID) to BaseVariant and its child ECU Variants if missing
                    if (link.LinkComParams.Count > 0)
                    {
                        foreach (var lcp in link.LinkComParams)
                        {
                            if (!bv.CommunicationParameters.Any(c => c.Name.Equals(lcp.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                bv.CommunicationParameters.Add(lcp.CloneAsInherited(link.ShortName, link.ResolvedProtocolName));
                            }

                            foreach (var childEv in package.EcuVariants.Where(ev => ev.ParentRefs.Any(p => p.ResolvedLayerShortName == bv.ShortName)))
                            {
                                if (!childEv.CommunicationParameters.Any(c => c.Name.Equals(lcp.Name, StringComparison.OrdinalIgnoreCase)))
                                {
                                    childEv.CommunicationParameters.Add(lcp.CloneAsInherited(link.ShortName, link.ResolvedProtocolName));
                                }
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(link.BaseVariantRef))
                {
                    package.ConsistencyIssues.Add(new OdxConsistencyIssue
                    {
                        Severity = "Warning",
                        StandardRule = "ISO 22901-1 ODX-V LOGICAL-LINK",
                        SourceObject = link.ShortName,
                        Message = $"Logical Link '{link.ShortName}' references missing BASE-VARIANT '{link.BaseVariantRef}'."
                    });
                }

                // Resolve FunctionalGroup
                var fg = ResolveLinkLayer(link.FunctionalGroupDocRef, link.FunctionalGroupRef, layersByDocAndId, layersById, layersByShortName);
                if (fg != null)
                {
                    link.ResolvedFunctionalGroupName = fg.ShortName;
                }
            }

            // Enrich PdxCatalog Vehicles/ECUs if ODX-V provides data missing in index.xml
            if (package.Catalog != null)
            {
                if (!package.Catalog.Vehicles.Any(v => v.Model.Equals(vi.Model, StringComparison.OrdinalIgnoreCase)) &&
                    !string.IsNullOrEmpty(vi.Model))
                {
                    package.Catalog.Vehicles.Add(new PdxVehicleInfo
                    {
                        ShortName = vi.ShortName,
                        Model = vi.Model,
                        Company = vi.Company,
                        ModelYear = vi.ModelYear
                    });
                }

                foreach (var ll in vi.LogicalLinks)
                {
                    var ecuName = !string.IsNullOrEmpty(ll.ResolvedBaseVariantName) ? ll.ResolvedBaseVariantName : ll.ShortName;
                    if (!package.Catalog.Ecus.Any(e => e.EcuName.Equals(ecuName, StringComparison.OrdinalIgnoreCase) || e.ShortName.Equals(ll.ShortName, StringComparison.OrdinalIgnoreCase)))
                    {
                        package.Catalog.Ecus.Add(new PdxEcuInfo
                        {
                            ShortName = ll.ShortName,
                            EcuName = ecuName,
                            Supplier = vi.Company,
                            DiagnosticAddress = !string.IsNullOrEmpty(ll.PhysicalRequestId) ? $"{ll.PhysicalRequestId} -> {ll.PhysicalResponseId}".Trim() : "Logical Link"
                        });
                    }
                }
            }
        }
    }

    private static DiagnosticLayer? ResolveLinkLayer(
        string docRef,
        string refId,
        Dictionary<(string DocRef, string Id), DiagnosticLayer> layersByDocAndId,
        Dictionary<string, DiagnosticLayer> layersById,
        Dictionary<string, DiagnosticLayer> layersByShortName)
    {
        if (string.IsNullOrWhiteSpace(refId)) return null;
        if (!string.IsNullOrEmpty(docRef) && layersByDocAndId.TryGetValue((docRef, refId), out var byDoc))
            return byDoc;
        if (layersById.TryGetValue(refId, out var byId))
            return byId;
        if (layersByShortName.TryGetValue(refId, out var bySn))
            return bySn;
        return null;
    }

    private static void ValidateConsistency(DiagnosticPackage package)
    {
        // Check PDX Catalog ABLOCK referenced files exist on disk
        if (package.Catalog != null && !string.IsNullOrEmpty(package.ExtractedDirectory) && Directory.Exists(package.ExtractedDirectory))
        {
            foreach (var file in package.Catalog.Files)
            {
                if (!string.IsNullOrWhiteSpace(file.RelativePath))
                {
                    var normalizedRel = file.RelativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                    var fullPath = Path.Combine(package.ExtractedDirectory, normalizedRel);
                    if (!File.Exists(fullPath))
                    {
                        package.ConsistencyIssues.Add(new OdxConsistencyIssue
                        {
                            Severity = "Error",
                            StandardRule = "ISO 22901-2 PDX ABLOCK File Check",
                            SourceObject = file.ShortName,
                            Message = $"Catalog references missing archive file: '{file.RelativePath}'."
                        });
                    }
                }
            }
        }

        // Check missing SIDs on services
        foreach (var layer in package.Layers)
        {
            foreach (var svc in layer.Services.Where(s => !s.IsInherited))
            {
                if (string.IsNullOrWhiteSpace(svc.ServiceIdHex))
                {
                    package.ConsistencyIssues.Add(new OdxConsistencyIssue
                    {
                        Severity = "Warning",
                        StandardRule = "ISO 14229 / ODX-D Service SID",
                        SourceObject = $"{layer.ShortName}.{svc.ShortName}",
                        Message = $"Service '{svc.ShortName}' does not specify an explicit SERVICE-ID coded value."
                    });
                }
            }
        }
    }

    private sealed class TupleIgnoreCaseComparer : IEqualityComparer<(string DocRef, string Id)>
    {
        public static readonly TupleIgnoreCaseComparer Instance = new();

        public bool Equals((string DocRef, string Id) x, (string DocRef, string Id) y) =>
            string.Equals(x.DocRef, y.DocRef, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Id, y.Id, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string DocRef, string Id) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.DocRef ?? string.Empty),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Id ?? string.Empty));
    }
}
