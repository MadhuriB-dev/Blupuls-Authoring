using System.IO.Compression;
using System.Security;
using System.Xml.Linq;
using OdxDiagnosticParser.Models;

namespace OdxDiagnosticParser.Parsers;

/// <summary>
/// Handles safe extraction of ASAM MCD-2 D / ISO 22901-2 PDX packages, catalog parsing, and data aggregation.
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
            // Empty directory entries
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
    /// Parses the index.xml catalog file inside the extracted PDX directory.
    /// </summary>
    public PdxCatalog? ParseCatalog(string extractedDirectory)
    {
        if (!Directory.Exists(extractedDirectory))
            return null;

        var indexFiles = Directory.GetFiles(extractedDirectory, "index.xml", SearchOption.AllDirectories);
        if (indexFiles.Length == 0)
            return null;

        var indexPath = indexFiles[0];
        try
        {
            var doc = XDocument.Load(indexPath);
            var root = doc.Root;
            if (root == null) return null;

            var catalog = new PdxCatalog
            {
                ShortName = root.El("SHORT-NAME").Val(),
                Version = root.Attr("MODEL-VERSION"),
                Creator = root.El("CREATOR")?.Val() ?? root.El("COMPANY")?.Val() ?? string.Empty,
                CreationDate = root.El("DATE")?.Val() ?? root.El("CREATION-DATE")?.Val() ?? string.Empty,
                Description = root.El("DESC")?.Val() ?? string.Empty
            };

            // Vehicles
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

            // ECUs
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

            // Catalog Elements / Files
            foreach (var elem in root.Desc("CATALOG-ELEMENT"))
            {
                var fileInfo = new PdxFileInfo
                {
                    ShortName = elem.El("SHORT-NAME").Val(),
                    FileType = elem.Attr("TYPE"),
                    RelativePath = elem.El("FILE-PATH")?.Val() ?? elem.El("NAME")?.Val() ?? string.Empty,
                    Checksum = elem.El("CHECKSUM")?.Val() ?? string.Empty
                };

                if (long.TryParse(elem.El("FILE-SIZE")?.Val(), out var size))
                    fileInfo.FileSizeBytes = size;

                catalog.Files.Add(fileInfo);
            }

            return catalog;
        }
        catch
        {
            // Return empty or partial catalog if index.xml malformed
            return null;
        }
    }

    /// <summary>
    /// Unzips a PDX package, locates all diagnostic ODX files, and parses them into a unified DiagnosticPackage.
    /// </summary>
    public DiagnosticPackage ExtractAndParse(string pdxFilePath, string? targetDirectory = null)
    {
        var extractedDir = UnzipPdx(pdxFilePath, targetDirectory);
        var catalog = ParseCatalog(extractedDir);

        var package = new DiagnosticPackage
        {
            Name = Path.GetFileNameWithoutExtension(pdxFilePath),
            SourcePath = pdxFilePath,
            IsFromPdx = true,
            Catalog = catalog,
            ExtractedAt = DateTime.UtcNow
        };

        if (catalog != null && !string.IsNullOrWhiteSpace(catalog.ShortName))
        {
            package.Name = catalog.ShortName;
            package.Description = catalog.Description;
        }

        // Find all ODX files (.odx-d, .odx-c, .odx-v, .odx-e, .odx, .xml)
        var odxExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".odx-d", ".odx-c", ".odx-v", ".odx-e", ".odx-f", ".odx", ".xml"
        };

        var allFiles = Directory.GetFiles(extractedDir, "*.*", SearchOption.AllDirectories)
            .Where(f => odxExtensions.Contains(Path.GetExtension(f)) &&
                        !Path.GetFileName(f).Equals("index.xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(GetFileProcessingPriority)
            .ToList();

        // If catalog specified files, we can also supplement file metadata
        foreach (var file in allFiles)
        {
            try
            {
                var subPackage = _odxParser.ParseFile(file);
                foreach (var layer in subPackage.Layers)
                {
                    layer.SourceFileName = Path.GetFileName(file);
                    // Avoid duplicate layer Ids
                    if (!package.Layers.Any(l => l.Id == layer.Id && l.ShortName == layer.ShortName))
                    {
                        package.Layers.Add(layer);
                    }
                }
            }
            catch (Exception ex)
            {
                // Record parsing error or continue with other files
                Console.WriteLine($"[Warning] Failed to parse file {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        return package;
    }

    private static int GetFileProcessingPriority(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".odx-c" => 1, // Protocols first
            ".odx-e" => 2, // ECU shared data
            ".odx-d" => 3, // ECU diagnostic layer
            ".odx-v" => 4, // Vehicle topology
            _ => 5
        };
    }
}

