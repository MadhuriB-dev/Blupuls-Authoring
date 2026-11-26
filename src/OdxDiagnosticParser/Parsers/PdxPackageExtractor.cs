using System.IO.Compression;
using System.Security;
using System.Xml.Linq;
using OdxDiagnosticParser.Models;

namespace OdxDiagnosticParser.Parsers;

/// <summary>
/// Handles safe extraction of ASAM MCD-2 D / ISO 22901-2 PDX packages, catalog parsing (ABLOCKs &amp; manifests),
/// and ISO 22901-1 cross-file diagnostic data aggregation.
/// </summary>
public class PdxPackageExtractor : IPdxExtractor
{
    private readonly IOdxParser _odxParser;

    public PdxPackageExtractor(IOdxParser? odxParser = null)
    {
        _odxParser = odxParser ?? new OdxXmlParser();
    }

    /// <summary>
    /// Unzips a PDX package safely, checking for Zip-Slip vulnerabilities.
    /// </summary>
    public string UnzipPdx(string pdxFilePath, string? targetDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(pdxFilePath))
            throw new ArgumentException("PDX file path cannot be null or empty.", nameof(pdxFilePath));

        if (!File.Exists(pdxFilePath))
            throw new FileNotFoundException($"PDX archive file not found: {pdxFilePath}", pdxFilePath);

        var destDir = string.IsNullOrWhiteSpace(targetDirectory)
            ? Path.Combine(Path.GetTempPath(), "OdxExtractor", Path.GetFileNameWithoutExtension(pdxFilePath) + "_" + Guid.NewGuid().ToString("N")[..8])
            : Path.GetFullPath(targetDirectory);

        Directory.CreateDirectory(destDir);
        var canonicalDestDir = Path.GetFullPath(destDir);
        if (!canonicalDestDir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            canonicalDestDir += Path.DirectorySeparatorChar;

        using var archive = ZipFile.OpenRead(pdxFilePath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name) && (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\')))
            {
                var dirPath = Path.Combine(canonicalDestDir, entry.FullName);
                Directory.CreateDirectory(dirPath);
                continue;
            }

            var destinationPath = Path.GetFullPath(Path.Combine(canonicalDestDir, entry.FullName));

            // Prevent Zip-Slip vulnerability
            if (!destinationPath.StartsWith(canonicalDestDir, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException($"Malicious zip entry detected attempting path traversal: {entry.FullName}");
            }

            var parentDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            entry.ExtractToFile(destinationPath, overwrite: true);
        }

        return destDir;
    }

    /// <summary>
    /// Parses the ISO 22901-2 index.xml catalog file (supporting standard ABLOCKS/ABLOCK and CATALOG-ELEMENTS).
    /// </summary>
    public PdxCatalog? ParseCatalog(string extractedDirectory)
    {
        if (!Directory.Exists(extractedDirectory))
            return null;

        var indexFiles = Directory.GetFiles(extractedDirectory, "index.xml", SearchOption.AllDirectories);
        if (indexFiles.Length == 0)
            return null;

        var indexPath = indexFiles[0];
        var indexRootDir = Path.GetDirectoryName(indexPath) ?? extractedDirectory;

        try
        {
            var doc = XDocument.Load(indexPath);
            var root = doc.Root;
            if (root == null) return null;

            var fVersion = root.Attr("F-VERSION");
            var mVersion = root.Attr("MODEL-VERSION");
            var revisionEl = root.Desc("DOC-REVISION").FirstOrDefault();
            var companyEl = root.Desc("COMPANY-DATA").FirstOrDefault();

            var catalog = new PdxCatalog
            {
                ShortName = root.El("SHORT-NAME").Val(),
                StandardVersion = !string.IsNullOrEmpty(fVersion) ? fVersion : (!string.IsNullOrEmpty(mVersion) ? mVersion : "2.2.0"),
                Version = root.El("VERSION")?.Val() is { Length: > 0 } ver
                    ? ver
                    : (revisionEl?.El("REVISION-LABEL")?.Val() is { Length: > 0 } rev ? rev : (!string.IsNullOrEmpty(fVersion) ? fVersion : mVersion)),
                CompanyName = companyEl?.El("LONG-NAME")?.Val() is { Length: > 0 } cn
                    ? cn
                    : (companyEl?.El("SHORT-NAME")?.Val() ?? root.El("COMPANY")?.Val() ?? string.Empty),
                Creator = root.El("CREATOR")?.Val() is { Length: > 0 } cr
                    ? cr
                    : (companyEl?.El("LONG-NAME")?.Val() ?? companyEl?.El("SHORT-NAME")?.Val() ?? root.El("COMPANY")?.Val() ?? string.Empty),
                CreationDate = root.El("DATE")?.Val() is { Length: > 0 } dt
                    ? dt
                    : (revisionEl?.El("DATE")?.Val() ?? root.El("CREATION-DATE")?.Val() ?? string.Empty),
                Description = root.El("DESC")?.Val() ?? root.El("LONG-NAME")?.Val() ?? string.Empty
            };

            // Standard ISO 22901-2 ABLOCKS / ABLOCK
            foreach (var abEl in root.Desc("ABLOCK"))
            {
                var abCategory = abEl.El("CATEGORY")?.Val() is { Length: > 0 } c ? c : abEl.Attr("CATEGORY");
                var abRev = abEl.Desc("DOC-REVISION").FirstOrDefault()?.El("REVISION-LABEL")?.Val() ?? string.Empty;

                var ablock = new PdxAblock
                {
                    ShortName = abEl.El("SHORT-NAME").Val(),
                    LongName = abEl.El("LONG-NAME").Val(),
                    Category = abCategory,
                    RevisionLabel = abRev
                };

                foreach (var fileEl in abEl.Desc("FILE"))
                {
                    var relPath = fileEl.Val();
                    if (string.IsNullOrWhiteSpace(relPath))
                        relPath = fileEl.Attr("FILE-PATH");
                    if (string.IsNullOrWhiteSpace(relPath))
                        relPath = fileEl.Attr("PATH");

                    if (string.IsNullOrWhiteSpace(relPath)) continue;

                    long actualSize = 0;
                    var diskPath = Path.Combine(indexRootDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(diskPath))
                        actualSize = new FileInfo(diskPath).Length;

                    var fileInfo = new PdxFileInfo
                    {
                        ShortName = !string.IsNullOrEmpty(ablock.ShortName) ? ablock.ShortName : Path.GetFileNameWithoutExtension(relPath),
                        AblockShortName = ablock.ShortName,
                        FileType = !string.IsNullOrEmpty(ablock.Category) ? ablock.Category : InferFileTypeFromExtension(relPath),
                        MimeType = fileEl.Attr("MIME-TYPE"),
                        CreationDate = fileEl.Attr("CREATION-DATE"),
                        RelativePath = relPath.Replace('\\', '/'),
                        FileSizeBytes = actualSize
                    };

                    ablock.Files.Add(fileInfo);
                    AddUniqueFileInfo(catalog.Files, fileInfo);
                }

                // Fallback: some ABLOCK variants place path in FILE-PATH
                if (ablock.Files.Count == 0)
                {
                    var relPath = abEl.El("FILE-PATH")?.Val();
                    if (!string.IsNullOrWhiteSpace(relPath))
                    {
                        var fileInfo = new PdxFileInfo
                        {
                            ShortName = ablock.ShortName,
                            AblockShortName = ablock.ShortName,
                            FileType = !string.IsNullOrEmpty(ablock.Category) ? ablock.Category : InferFileTypeFromExtension(relPath),
                            RelativePath = relPath.Replace('\\', '/')
                        };
                        ablock.Files.Add(fileInfo);
                        AddUniqueFileInfo(catalog.Files, fileInfo);
                    }
                }

                catalog.Ablocks.Add(ablock);
            }

            // Vehicles (extended catalog section)
            foreach (var vEl in root.Desc("VEHICLE-INFO"))
            {
                catalog.Vehicles.Add(new PdxVehicleInfo
                {
                    ShortName = vEl.El("SHORT-NAME").Val(),
                    Model = vEl.El("MODEL")?.Val() ?? vEl.El("VEHICLE-MODEL")?.Val() ?? string.Empty,
                    Company = vEl.El("COMPANY")?.Val() ?? string.Empty,
                    ModelYear = vEl.El("MODEL-YEAR")?.Val() ?? string.Empty
                });
            }

            // ECUs (extended catalog section)
            foreach (var eEl in root.Desc("ECU-INFO"))
            {
                catalog.Ecus.Add(new PdxEcuInfo
                {
                    ShortName = eEl.El("SHORT-NAME").Val(),
                    EcuName = eEl.El("ECU-NAME")?.Val() ?? eEl.El("NAME")?.Val() ?? string.Empty,
                    Supplier = eEl.El("SUPPLIER")?.Val() ?? string.Empty,
                    DiagnosticAddress = eEl.El("DIAG-ADDRESS")?.Val() ?? string.Empty
                });
            }

            // Legacy / Custom CATALOG-ELEMENT manifest entries
            foreach (var elem in root.Desc("CATALOG-ELEMENT"))
            {
                var relPath = (elem.El("FILE-PATH")?.Val() ?? elem.El("NAME")?.Val() ?? string.Empty).Replace('\\', '/');
                var fileInfo = new PdxFileInfo
                {
                    ShortName = elem.El("SHORT-NAME").Val(),
                    FileType = elem.Attr("TYPE"),
                    RelativePath = relPath,
                    Checksum = elem.El("CHECKSUM")?.Val() ?? string.Empty
                };

                if (long.TryParse(elem.El("FILE-SIZE")?.Val(), out var size))
                    fileInfo.FileSizeBytes = size;

                AddUniqueFileInfo(catalog.Files, fileInfo);
            }

            return catalog;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Unzips a PDX package, locates all diagnostic ODX files per index.xml &amp; directory scan,
    /// parses ODX-C / ODX-E / ODX-D / ODX-V, and connects all ISO 22901-1 layers and cross-references.
    /// </summary>
    public DiagnosticPackage ExtractAndParse(string pdxFilePath, string? targetDirectory = null)
    {
        var extractedDir = UnzipPdx(pdxFilePath, targetDirectory);
        var catalog = ParseCatalog(extractedDir);

        var package = new DiagnosticPackage
        {
            Name = Path.GetFileNameWithoutExtension(pdxFilePath),
            SourcePath = pdxFilePath,
            ExtractedDirectory = extractedDir,
            IsFromPdx = true,
            Catalog = catalog,
            ExtractedAt = DateTime.UtcNow
        };

        if (catalog != null && !string.IsNullOrWhiteSpace(catalog.ShortName))
        {
            package.Name = catalog.ShortName;
            package.Description = catalog.Description;
            if (!string.IsNullOrWhiteSpace(catalog.StandardVersion))
                package.Version = catalog.StandardVersion;
        }

        // Discover ODX files (extensions: .odx-c, .odx-e, .odx-d, .odx-v, .odx-f, .odx-fd, .odx-m, .odx, .xml)
        var odxExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".odx-d", ".odx-c", ".odx-v", ".odx-e", ".odx-f", ".odx-fd", ".odx-m", ".odx", ".xml"
        };

        var discoveredFiles = new List<string>();
        var seenFullPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Priority 1: Resolve paths listed in index.xml ABLOCKs / Catalog files
        if (catalog != null)
        {
            var indexDir = Path.GetDirectoryName(Directory.GetFiles(extractedDir, "index.xml", SearchOption.AllDirectories).FirstOrDefault() ?? extractedDir) ?? extractedDir;
            foreach (var cf in catalog.Files.OrderBy(f => GetCategoryPriority(f.FileType, f.RelativePath)))
            {
                if (string.IsNullOrWhiteSpace(cf.RelativePath)) continue;
                var candidate = Path.GetFullPath(Path.Combine(indexDir, cf.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
                if (File.Exists(candidate) && seenFullPaths.Add(candidate))
                {
                    discoveredFiles.Add(candidate);
                }
            }
        }

        // Priority 2: Supplement with any remaining ODX files found on disk
        var diskFiles = Directory.GetFiles(extractedDir, "*.*", SearchOption.AllDirectories)
            .Where(f => odxExtensions.Contains(Path.GetExtension(f)) &&
                        !Path.GetFileName(f).Equals("index.xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(GetFileProcessingPriority);

        foreach (var file in diskFiles)
        {
            var full = Path.GetFullPath(file);
            if (seenFullPaths.Add(full))
            {
                discoveredFiles.Add(full);
            }
        }

        // Ordered parse pass (without pre-mature cross-file inheritance until all files are loaded)
        var rawXmlParser = _odxParser as OdxXmlParser;
        foreach (var file in discoveredFiles.OrderBy(GetFileProcessingPriority))
        {
            try
            {
                var subPackage = rawXmlParser != null
                    ? rawXmlParser.ParseFileWithoutLinking(file)
                    : _odxParser.ParseFile(file);

                var fileNameOnly = Path.GetFileName(file);

                foreach (var spec in subPackage.ComParamSpecs)
                {
                    spec.SourceFileName = fileNameOnly;
                    if (!package.ComParamSpecs.Any(c => c.Id == spec.Id && c.ShortName == spec.ShortName))
                        package.ComParamSpecs.Add(spec);
                }

                foreach (var vi in subPackage.VehicleInfos)
                {
                    vi.SourceFileName = fileNameOnly;
                    if (!package.VehicleInfos.Any(v => v.Id == vi.Id && v.ShortName == vi.ShortName))
                        package.VehicleInfos.Add(vi);
                }

                foreach (var layer in subPackage.Layers)
                {
                    layer.SourceFileName = fileNameOnly;
                    if (!package.Layers.Any(l =>
                            !string.IsNullOrEmpty(l.Id) && l.Id == layer.Id &&
                            l.ShortName == layer.ShortName && l.LayerType == layer.LayerType))
                    {
                        package.Layers.Add(layer);
                    }
                }
            }
            catch (Exception ex)
            {
                package.ConsistencyIssues.Add(new OdxConsistencyIssue
                {
                    Severity = "Error",
                    StandardRule = "ISO 22901 XML Schema Load",
                    SourceObject = Path.GetFileName(file),
                    Message = $"Failed to parse packaged file: {ex.Message}"
                });
            }
        }

        // Resolve all ISO 22901-1 & ISO 22901-2 cross-file references, PARENT-REF inheritance, ODX-C ComParams & ODX-V Logical Links!
        OdxIsoLinker.LinkPackage(package);

        return package;
    }

    private static void AddUniqueFileInfo(List<PdxFileInfo> list, PdxFileInfo item)
    {
        var existing = list.FirstOrDefault(f =>
            !string.IsNullOrEmpty(item.RelativePath) &&
            f.RelativePath.Equals(item.RelativePath, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            list.Add(item);
        }
        else
        {
            if (string.IsNullOrEmpty(existing.Checksum) && !string.IsNullOrEmpty(item.Checksum))
                existing.Checksum = item.Checksum;
            if (existing.FileSizeBytes == 0 && item.FileSizeBytes > 0)
                existing.FileSizeBytes = item.FileSizeBytes;
            if (string.IsNullOrEmpty(existing.AblockShortName) && !string.IsNullOrEmpty(item.AblockShortName))
                existing.AblockShortName = item.AblockShortName;
            if (string.IsNullOrEmpty(existing.MimeType) && !string.IsNullOrEmpty(item.MimeType))
                existing.MimeType = item.MimeType;
        }
    }

    private static string InferFileTypeFromExtension(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".odx-c" => "ODX-COMPARAM-SPEC",
            ".odx-d" => "ODX-DATA",
            ".odx-e" => "ODX-ECU-CONFIG",
            ".odx-v" => "ODX-VEHICLE-INFO",
            ".odx-f" => "ODX-FLASH",
            ".odx-fd" => "ODX-FUNCTION-DICTIONARY",
            _ => "ODX-DATA"
        };
    }

    private static int GetCategoryPriority(string category, string relPath)
    {
        var cat = (category ?? "").ToUpperInvariant();
        if (cat.Contains("COMPARAM") || cat == "ODX-C") return 1;
        if (cat.Contains("ECU-CONFIG") || cat == "ODX-E") return 2;
        if (cat.Contains("DATA") || cat == "ODX-D") return 3;
        if (cat.Contains("VEHICLE") || cat == "ODX-V") return 4;
        return GetFileProcessingPriority(relPath);
    }

    private static int GetFileProcessingPriority(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".odx-c" => 1, // Protocols & ComParam specs first
            ".odx-e" => 2, // ECU shared data
            ".odx-d" => 3, // BaseVariants & ECU Variants
            ".odx-v" => 4, // Vehicle topology & logical links
            _ => 5
        };
    }
}
