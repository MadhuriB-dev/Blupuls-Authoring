# OdxDiagnosticParser - Complete API Reference & Developer Guide

This document provides a comprehensive, developer-ready API reference for the **`OdxDiagnosticParser`** Class Library and demonstrates how every API is invoked inside the **`OdxDiagnosticParser.Tester`** console application.

---

## 1. Solution Architecture & Calling Topology

The solution consists of **2 interconnected projects** under one Visual Studio Solution (`OdxDiagnosticParser.sln`):

```
                        ┌────────────────────────────────────────────────────────┐
                        │              OdxDiagnosticParser.Tester                │
                        │                 (Console Application)                  │
                        └──────┬──────────────────┬──────────────────┬───────────┘
                               │                  │                  │
                Calls          │                  │                  │
                               ▼                  ▼                  ▼
┌─────────────────────────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│   IPdxExtractor / PdxPackageExtractor│ │ DiagnosticSession│ │SampleOdxGenerator│
└──────────────────┬──────────────────┘ └─────────┬────────┘ └──────────────────┘
                   │                              │
                   │ Uses                         │ Uses
                   ▼                              ▼
┌─────────────────────────────────────┐ ┌──────────────────────────────────────┐
│       IOdxParser / OdxXmlParser     │ │  IOdxExcelExporter / OdxExcelExporter│
│   (Namespace-Agnostic XML Engine)   │ │       (ClosedXML Excel Engine)       │
└──────────────────┬──────────────────┘ └──────────────────┬───────────────────┘
                   │                                       │
                   │ Produces                              │ Exports
                   ▼                                       ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                             Diagnostic Domain Models                         │
│   (DiagnosticPackage, DiagnosticLayer, DiagnosticService, DID, DTC, ComParam) │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Console Tester Mapping: Menu & CLI Commands to APIs

The table below maps every user action in `OdxDiagnosticParser.Tester` (`Program.cs`) to the underlying Class Library APIs:

| Tester Action / Menu Option | CLI Command | Primary APIs Invoked | Input Requirements | Output Produced |
|---|---|---|---|---|
| **Option 1: Temporary Session** | `dotnet run -- session [pdx-path]` | `DiagnosticSession.CreateFromPdx()`<br>`session.OpenExcelInDefaultViewer()`<br>`session.SavePermanentCopy()`<br>`session.Dispose()` | `.pdx` file path (or Enter for generated sample) | Temporary folder in `%TEMP%`, temp Excel launched in viewer, **auto-deleted upon exit**. |
| **Option 2: Test PDX & Export Excel** | `dotnet run -- pdx <pdx-path> [out.xlsx]` | `PdxExtractor.ExtractAndParse()`<br>`ExcelExporter.ExportToExcel()` | Path to `.pdx` / `.zip` package | Extracted files, detailed categorized console report, permanent `.xlsx` file. |
| **Option 3: Test Single ODX XML** | `dotnet run -- parse-odx <odx-path> [out.xlsx]` | `OdxParser.ParseFile()`<br>`ExcelExporter.ExportToExcel()` | Path to `.odx` / `.odx-d` / `.xml` file | Categorized console diagnostic report, permanent `.xlsx` workbook. |
| **Option 4: Run Test Suite** | `dotnet run -- test` | `ParserPartTests.RunAll()`<br>`OdxParser.ParseXml()`<br>`PdxExtractor.ExtractAndParse()`<br>`ExcelExporter.ExportToBytes()` | Built-in sample generator fixtures | 15 automated unit test results with pass/fail and elapsed time in ms. |
| **Option 5: Test Specific Part** | Interactive sub-menu (`a` to `i`) | Direct part tests: `TestOdxDiagnosticLayers`, `TestUdsServicesParsing`, `TestStoredAndCurrentDataCategorization`, etc. | Unit-level inputs & mocks | Isolated unit test logs and diagnostics. |
| **Option 6: End-to-End Demo** | `dotnet run -- demo` | `SampleOdxGenerator.CreateSamplePdxFile()`<br>`PdxExtractor.ExtractAndParse()`<br>`ExcelExporter.ExportToExcel()` | None (auto-generates realistic EMS ECU PDX) | `OdxDemoOutput/Powertrain_EMS_2026.pdx`, full console report, `Powertrain_EMS_Diagnostics.xlsx`. |
| **Direct File Invocation** | `dotnet run -- <file.pdx>` or `<file.odx-d>` | Routes to `TestAndExportPdx` or `ConvertOdxToExcel` based on file extension | File path as 1st CLI argument | Console report and matching `.xlsx` workbook. |

---

## 3. Detailed API Reference by Component

---

### 3.1. PDX Extraction API (`IPdxExtractor` & `PdxPackageExtractor`)

**Namespace**: `OdxDiagnosticParser.Parsers`  
**Purpose**: Safely unpacks compressed `.pdx` (PKZIP) archives, defends against Zip-Slip path-traversal attacks, parses the `index.xml` catalog manifest, discovers diagnostic layers (`.odx-d`, `.odx-c`), and returns an aggregated `DiagnosticPackage`.

#### Interface Definition
```csharp
namespace OdxDiagnosticParser.Parsers;

public interface IPdxExtractor
{
    DiagnosticPackage ExtractAndParse(string pdxFilePath, string? extractionTargetDirectory = null);
    DiagnosticPackage ExtractAndParse(Stream pdxStream, string packageName = "Package", string? extractionTargetDirectory = null);
    string UnzipPdx(string pdxFilePath, string? extractionDirectory = null);
    PdxCatalog? ParseCatalogManifest(string indexXmlPath);
}
```

#### Methods

##### 1. `ExtractAndParse(string pdxFilePath, string? extractionTargetDirectory = null)`
- **Description**: Extracts the PDX package to a destination directory, parses `index.xml`, parses all referenced `.odx-d` and `.odx-c` files, and aggregates all layers into a `DiagnosticPackage`.
- **Parameters**:
  - `pdxFilePath` (`string`): Absolute or relative path to the `.pdx` or `.zip` file on disk.
  - `extractionTargetDirectory` (`string?`, optional): Directory where contents are extracted. If `null`, a directory named `<pdxName>_extracted` is created next to the PDX file.
- **Returns**: `DiagnosticPackage` populated with catalog info, layers, services, DIDs, DTCs, and ComParams.
- **Exceptions**: `FileNotFoundException` (if PDX file doesn't exist), `SecurityException` (if Zip-Slip directory traversal is detected), `XmlException` (if XML markup is invalid).
- **Called in Tester**: `Program.cs` line 344 inside `TestAndExportPdx()`.

##### 2. `ExtractAndParse(Stream pdxStream, string packageName = "Package", string? extractionTargetDirectory = null)`
- **Description**: Same as above, but operates directly on a memory/file `Stream`. Ideal for web APIs, microservices, or file upload handlers without requiring local files.

##### 3. `UnzipPdx(string pdxFilePath, string? extractionDirectory = null)`
- **Description**: Low-level archive extraction with canonicalized path validation to prevent Zip-Slip vulnerabilities.
- **Returns**: `string` path to the folder containing the extracted ODX files.
- **Called in Tester**: `Program.cs` line 727 inside `HandleCommandLineArguments("unpack-pdx")`.

##### 4. `ParseCatalogManifest(string indexXmlPath)`
- **Description**: Parses the PDX root `index.xml` manifest file.
- **Returns**: `PdxCatalog` containing package name, creation timestamp, creator tool/company, vehicle model references, ECU names/addresses, and file inventory with MD5 checksums.

#### C# Usage Example
```csharp
using OdxDiagnosticParser.Parsers;
using OdxDiagnosticParser.Models;

// 1. Instantiate the parser and extractor
IOdxParser odxParser = new OdxXmlParser();
IPdxExtractor pdxExtractor = new PdxPackageExtractor(odxParser);

// 2. Unpack and parse the PDX container
string pdxPath = @"C:\Diagnostics\Powertrain_EMS.pdx";
DiagnosticPackage package = pdxExtractor.ExtractAndParse(pdxPath);

// 3. Inspect extracted data
Console.WriteLine($"Package: {package.Name}, Layers: {package.Layers.Count}");
Console.WriteLine($"Total Services: {package.TotalServicesCount}, DIDs: {package.TotalDidsCount}");
```

---

### 3.2. ODX XML Parsing API (`IOdxParser` & `OdxXmlParser`)

**Namespace**: `OdxDiagnosticParser.Parsers`  
**Purpose**: Resilient, namespace-agnostic LINQ to XML engine that parses ODX 2.0.1 and ODX 2.2.0 diagnostic files (`.odx`, `.odx-d`, `.odx-c`), resolves internal `ID-REF` links across parameters and Data Object Properties (DOPs), and synthesizes UDS services, DIDs, DTCs, and bus parameters.

#### Interface Definition
```csharp
namespace OdxDiagnosticParser.Parsers;

public interface IOdxParser
{
    DiagnosticPackage ParseFile(string filePath);
    DiagnosticPackage ParseXml(string xmlContent, string sourceName = "InlineXml");
    DiagnosticPackage ParseStream(Stream stream, string sourceName = "Stream");
}
```

#### Methods

##### 1. `ParseFile(string filePath)`
- **Description**: Reads an ODX file from disk and parses it. Automatically detects if the file is actually a PDX/ZIP archive (via magic header `PK\x03\x04` or extension) and routes it through `PdxPackageExtractor`.
- **Parameters**: `filePath` (`string`): Path to `.odx`, `.odx-d`, `.odx-c`, or `.xml` file.
- **Returns**: `DiagnosticPackage`.
- **Called in Tester**: `Program.cs` line 597 inside `ConvertOdxToExcel()`.

##### 2. `ParseXml(string xmlContent, string sourceName = "InlineXml")`
- **Description**: Parses an in-memory ODX XML string. Hardened with `SafeReaderSettings` (disables DTD/XXE) and auto-strips UTF-8 BOM, zero-width characters, and non-printable prefixes.
- **Parameters**:
  - `xmlContent` (`string`): Raw ODX XML string.
  - `sourceName` (`string`): Display label or pseudo-filename for logging.
- **Returns**: `DiagnosticPackage`.
- **Called in Tester**: `ParserPartTests.cs` lines 52, 65, 87, 114 for fast unit tests without disk I/O.

##### 3. `ParseStream(Stream stream, string sourceName = "Stream")`
- **Description**: Parses ODX content from an open `Stream`.

##### 4. Static Helpers: Categorization Rules
```csharp
// Categorizes a service into "Current Data", "Stored Data", or "System Control"
public static string CategorizeService(string serviceIdHex, string semantic, string shortName, string description = "");

// Categorizes a DID into "Current Data" (sensors) or "Stored Data" (identification / snapshots)
public static string CategorizeDataIdentifier(string didHex, string shortName, string longName = "", string description = "");
```
- **Rules applied**:
  - `Current Data`: Live sensors (`0x22` / `0x2A` / `0x2C` / `0x01` / `0x06`), DIDs `0x0100-0x01FF` (OBD PIDs) or names with RPM, Speed, Temp, Voltage, Pressure.
  - `Stored Data`: Fault memory (`0x19`, `0x14`, `0x85`), identification DIDs (`0xF180-0xF1FF` ISO 14229-1, `0xF0xx`, `0xF8xx`), VIN, Serial, Part Number, SW Version.
  - `System Control`: Sessions (`0x10`), Security (`0x27`), Tester Present (`0x3E`), Routine (`0x31`), Reset (`0x11`).

#### C# Usage Example
```csharp
using OdxDiagnosticParser.Parsers;
using OdxDiagnosticParser.Models;

IOdxParser parser = new OdxXmlParser();

// Parse single ECU ODX file
DiagnosticPackage package = parser.ParseFile(@"C:\Diagnostics\EngineControlUnit.odx-d");

foreach (var layer in package.Layers)
{
    Console.WriteLine($"Layer: {layer.ShortName} ({layer.LayerType})");
    foreach (var svc in layer.CurrentDataServices)
    {
        Console.WriteLine($"  [Current Data] {svc.ServiceIdHex} - {svc.ShortName}");
    }
    foreach (var svc in layer.StoredDataServices)
    {
        Console.WriteLine($"  [Stored Data]  {svc.ServiceIdHex} - {svc.ShortName}");
    }
}
```

---

### 3.3. Ephemeral Diagnostic Session API (`DiagnosticSession`)

**Namespace**: `OdxDiagnosticParser.Sessions`  
**Purpose**: Manages a temporary diagnostic session. It unzips the PDX into an isolated directory in `%TEMP%`, generates a temporary Excel report, launches it in the user's viewer, and **automatically deletes every temporary file and directory** upon exit (`Dispose()`).

#### Class Definition
```csharp
namespace OdxDiagnosticParser.Sessions;

public sealed class DiagnosticSession : IDisposable
{
    public string SessionId { get; }
    public string SessionDirectory { get; }
    public string ExtractedDirectory { get; }
    public string TemporaryExcelFilePath { get; }
    public DiagnosticPackage Package { get; }

    public static DiagnosticSession CreateFromPdx(
        string pdxFilePath, 
        string? baseDirectory = null, 
        IOdxParser? parser = null, 
        IOdxExcelExporter? exporter = null);

    public void SavePermanentCopy(string destinationExcelFilePath);
    public void OpenExcelInDefaultViewer();
    public void Dispose();
}
```

#### Properties
- `SessionId` (`string`): Unique GUID identifying the session.
- `SessionDirectory` (`string`): Absolute path to the isolated temporary folder (e.g. `%TEMP%\OdxDiagSession_a1b2c3d4`).
- `ExtractedDirectory` (`string`): Path to the extracted ODX files within the session folder.
- `TemporaryExcelFilePath` (`string`): Path to the temporary `.xlsx` workbook generated for the session.
- `Package` (`DiagnosticPackage`): The parsed in-memory diagnostic data structure.

#### Methods
- `CreateFromPdx(...)`: Factory method that initializes the session, unpacks the PDX, parses the data, and generates the temporary Excel file.
- `OpenExcelInDefaultViewer()`: Opens the temporary Excel file with the default Windows application (`cmd.exe /c start`).
- `SavePermanentCopy(string destinationExcelFilePath)`: Copies the temporary Excel file to a permanent path chosen by the user before the session ends.
- `Dispose()`: Deletes `SessionDirectory` and all contents. Registered with `AppDomain.CurrentDomain.ProcessExit` to ensure cleanup even on unhandled terminations.

#### Called in Tester
`Program.cs` lines 160–240 in `RunTemporarySession()`:
```csharp
using (var session = DiagnosticSession.CreateFromPdx(pdxPath))
{
    DisplayDetailedDiagnosticReport(session.Package);
    session.OpenExcelInDefaultViewer();
    // User inspects data...
    // Optionally: session.SavePermanentCopy("C:\\Reports\\MySavedReport.xlsx");
} // <- Automatically deletes session directory and temporary Excel file here
```

---

### 3.4. Excel Report Exporter API (`IOdxExcelExporter` & `OdxExcelExporter`)

**Namespace**: `OdxDiagnosticParser.Exporters`  
**Purpose**: Generates multi-worksheet automotive Excel (`.xlsx`) workbooks using ClosedXML (no Microsoft Office or COM Interop required).

#### Interface Definition
```csharp
namespace OdxDiagnosticParser.Exporters;

public interface IOdxExcelExporter
{
    void ExportToExcel(DiagnosticPackage package, string outputFilePath, ExcelExportOptions? options = null);
    byte[] ExportToBytes(DiagnosticPackage package, ExcelExportOptions? options = null);
}
```

#### Options (`ExcelExportOptions`)
```csharp
public class ExcelExportOptions
{
    public string HeaderBackgroundColorHex { get; set; } = "#1B365D"; // Automotive Dark Navy
    public string AlternateRowColorHex { get; set; } = "#F8FAFC";      // Subtle Zebra Striping
    public int MaxColumnWidth { get; set; } = 60;
    public bool AutoFitColumns { get; set; } = true;
    public bool IncludePdxManifestSheet { get; set; } = true;
    public bool IncludeComParamsSheet { get; set; } = true;
}
```

#### Worksheets Generated

1. **Overview**: Package metadata, creator, vehicle model, ECU diagnostic address, and summary counts (Current Data services, Stored Data services, DIDs, DTCs).
2. **Diagnostic Services**:
   - Column 1: `ECU / Layer`
   - Column 2: `Data Category` (**Current Data** [Green badge], **Stored Data** [Amber badge], **System Control** [Purple badge])
   - Column 3: `Service ID (Hex)` (`0x10`, `0x22`, `0x19`, etc.)
   - Column 4: `Service Name`
   - Column 5: `Semantic`
   - Column 6: `Sub-Function`
   - Column 7: `Pos Resp SID` (`0x50`, `0x62`, `0x59`, etc.)
   - Column 8: `Request Parameters Count`
   - Column 9: `Response Parameters Count`
   - Column 10: `Supported NRCs`
   - Column 11: `Description`
3. **Data Identifiers (DIDs)**:
   - Column 1: `ECU / Layer`
   - Column 2: `Data Category` (**Current Data** vs **Stored Data**)
   - Column 3: `DID (Hex)` (`0xF190`, `0x010C`)
   - Column 4: `DID (Dec)`
   - Column 5: `DID Name`
   - Column 6: `Access` (`Read` [Green], `Write`/`Read-Write` [Red/Blue])
   - Column 7: `Length (Bytes)`
   - Column 8: `Bit Length`
   - Column 9: `Data Type` (`A_UINT32`, `A_ASCIISTRING`)
   - Column 10: `Unit` (`rpm`, `degC`, `V`)
   - Column 11: `Scaling Factor`
   - Column 12: `Scaling Offset`
   - Column 13: `Min Physical`
   - Column 14: `Max Physical`
   - Column 15: `Associated Service`
   - Column 16: `Description`
4. **DTCs (Trouble Codes)**: Alphanumeric UDS Display Code (`P0300-92`), Trouble Code Hex (`0x030092`), Failure Type Byte (`0x92`), Severity (`Fatal` / `Warning`), Description.
5. **Communication Parameters**: Parameter names (`CP_CanId`, `CP_Baudrate`), values, engineering units (`bps`, `ms`).
6. **PDX Manifest** (for PDX packages): Catalog file list, file types (`ODX-D`, `ODX-C`), relative paths, sizes, and MD5 hashes.

#### C# Usage Example
```csharp
using OdxDiagnosticParser.Exporters;

IOdxExcelExporter exporter = new OdxExcelExporter();

// Export to file
exporter.ExportToExcel(package, @"C:\Diagnostics\Report.xlsx");

// Or export to byte array (for ASP.NET file download responses)
byte[] excelBytes = exporter.ExportToBytes(package);
```

---

### 3.5. Sample ODX & PDX Generator API (`SampleOdxGenerator`)

**Namespace**: `OdxDiagnosticParser.Samples`  
**Purpose**: Programmatically generates realistic, standard-compliant ODX XML strings and compressed PDX packages for automated testing and demonstrations without requiring external diagnostic files.

#### Static Methods
```csharp
namespace OdxDiagnosticParser.Samples;

public static class SampleOdxGenerator
{
    public static string CreateSampleOdxXml(string ecuName = "EngineControlUnit", string modelVersion = "2.2.0");
    public static void CreateSamplePdxFile(string targetPdxPath, string ecuName = "EngineControlUnit");
    public static byte[] CreateSamplePdxBytes(string ecuName = "EngineControlUnit");
}
```

- `CreateSampleOdxXml(...)`: Returns XML with UDS services (0x10, 0x14, 0x19, 0x22, 0x27, 0x2E, 0x3E), DIDs (0xF190 VIN, 0x010C Engine Speed, 0x0105 Coolant Temp, 0x0142 Battery Voltage), DTCs (`P0100-13`, `P0300-92`, `U0100-87`, `B1000-16`), and ComParams (`CP_Baudrate`, `CP_P2Max`).
- `CreateSamplePdxFile(...)`: Assembles a complete `.pdx` archive on disk containing `index.xml`, `odx-d/EngineControlUnit.odx-d`, and `odx-c/Protocols_UDS.odx-c`.
- `CreateSamplePdxBytes(...)`: Returns the `.pdx` archive as a `byte[]` in memory.

---

## 4. Diagnostic Data Models Reference

All domain models reside in namespace `OdxDiagnosticParser.Models`:

```
DiagnosticPackage
├── Catalog: PdxCatalog
│   ├── Vehicles: List<PdxVehicleRef>
│   ├── Ecus: List<PdxEcuRef>
│   └── Files: List<PdxFileEntry>
└── Layers: List<DiagnosticLayer>
    ├── Services: List<DiagnosticService>
    │   ├── RequestParameters: List<DiagnosticParameter>
    │   ├── ResponseParameters: List<DiagnosticParameter>
    │   └── NegativeResponses: List<string>
    ├── DataIdentifiers: List<DiagnosticDataIdentifier>
    ├── DiagnosticTroubleCodes: List<DiagnosticTroubleCode>
    ├── DataObjectProperties: List<DataObjectProperty>
    └── CommunicationParameters: List<CommunicationParameter>
```

### Key Query Properties on `DiagnosticPackage`
- `package.TotalServicesCount` -> Total UDS/KWP services.
- `package.TotalCurrentDataServicesCount` -> Live sensor/telemetry services.
- `package.TotalStoredDataServicesCount` -> Fault memory and stored identification services.
- `package.TotalSystemControlServicesCount` -> Session, security, and routine control services.
- `package.TotalDidsCount` -> Total Data Identifiers.
- `package.TotalCurrentDataDidsCount` -> Live sensor DIDs (RPM, temperature, voltage).
- `package.TotalStoredDataDidsCount` -> Non-volatile identification DIDs (VIN, serial number).
- `package.TotalDtcsCount` -> Total Diagnostic Trouble Codes.
- `package.TotalComParamsCount` -> Total Communication Parameters.

### Key Query Properties on `DiagnosticLayer`
- `layer.CurrentDataServices` -> Filtered enumerable of services where `DataCategory == "Current Data"`.
- `layer.StoredDataServices` -> Filtered enumerable of services where `DataCategory == "Stored Data"`.
- `layer.SystemControlServices` -> Filtered enumerable of services where `DataCategory == "System Control"`.
- `layer.CurrentDataDids` -> Filtered enumerable of DIDs where `DataCategory == "Current Data"`.
- `layer.StoredDataDids` -> Filtered enumerable of DIDs where `DataCategory == "Stored Data"`.

---

## 5. End-to-End Code Integration Examples

### Example 1: Full Pipeline (PDX -> Extracted Data -> Categorized Console Output -> Excel)
```csharp
using System;
using OdxDiagnosticParser.Exporters;
using OdxDiagnosticParser.Models;
using OdxDiagnosticParser.Parsers;

class Program
{
    static void Main()
    {
        // 1. Create instances
        IOdxParser parser = new OdxXmlParser();
        IPdxExtractor extractor = new PdxPackageExtractor(parser);
        IOdxExcelExporter exporter = new OdxExcelExporter();

        // 2. Extract and parse PDX archive
        string pdxPath = @"C:\Diagnostics\EngineControlUnit.pdx";
        DiagnosticPackage package = extractor.ExtractAndParse(pdxPath);

        // 3. Print categorized diagnostic summary
        Console.WriteLine($"Package: {package.Name} (ODX v{package.Version})");
        Console.WriteLine($"Current Data Services: {package.TotalCurrentDataServicesCount}");
        Console.WriteLine($"Stored Data Services:  {package.TotalStoredDataServicesCount}");
        Console.WriteLine($"Stored DTCs:           {package.TotalDtcsCount}");

        foreach (var layer in package.Layers)
        {
            Console.WriteLine($"\n--- Layer: {layer.ShortName} ---");
            
            // Live sensor measurements
            Console.WriteLine("[Current Data DIDs]");
            foreach (var did in layer.CurrentDataDids)
            {
                Console.WriteLine($"  [{did.DidHex}] {did.ShortName,-20} = Scaled {did.ScalingFactor}x + {did.Offset} {did.Unit}");
            }

            // Stored fault memory
            Console.WriteLine("[Stored Fault Codes]");
            foreach (var dtc in layer.DiagnosticTroubleCodes)
            {
                Console.WriteLine($"  [{dtc.DisplayCode}] {dtc.TroubleCodeHex} (FTB: {dtc.FailureTypeByte}): {dtc.Description}");
            }
        }

        // 4. Export multi-sheet Excel
        string excelPath = @"C:\Diagnostics\EngineControlUnit_Report.xlsx";
        exporter.ExportToExcel(package, excelPath);
        Console.WriteLine($"\nReport exported: {excelPath}");
    }
}
```

### Example 2: Temporary Diagnostic Session with Auto-Cleanup
```csharp
using OdxDiagnosticParser.Sessions;

// Starts an isolated session, generates temp Excel, auto-deletes on exit
using (var session = DiagnosticSession.CreateFromPdx(@"C:\Diagnostics\Vehicle_EMS.pdx"))
{
    Console.WriteLine($"Temp Directory: {session.SessionDirectory}");
    Console.WriteLine($"Temp Excel:     {session.TemporaryExcelFilePath}");

    // Launch Excel viewer
    session.OpenExcelInDefaultViewer();

    // User inspects report...
    Console.WriteLine("Press Enter when done inspecting (temp files will be deleted)...");
    Console.ReadLine();

    // If user wants to save permanently:
    // session.SavePermanentCopy(@"C:\PermanentReports\SavedReport.xlsx");
} 
// <- Cleaned up: session folder, unzipped ODX files, and temp Excel file are deleted!
```

---

## 6. Error Handling & Security Guidelines

| Issue / Hazard | Defense Implemented in API | Recommended Caller Handling |
|---|---|---|
| **Zip-Slip Vulnerability** | `PdxPackageExtractor` validates canonical path before extraction. | None needed; `SecurityException` thrown if hostile path detected. |
| **XXE Injection** | `OdxXmlParser` configures `XmlReaderSettings.DtdProcessing = DtdProcessing.Prohibit` and sets `XmlResolver = null`. | Safe to parse untrusted ODX XML from 3rd-party OEMs. |
| **Non-XML Data in Parser** | `OdxXmlParser.StripInvalidXmlPreamble()` strips BOM and leading junk characters; auto-detects ZIP archives. | Pass raw strings, streams, or files directly. |
| **Orphaned Temp Files** | `DiagnosticSession` implements `IDisposable` and hooks `AppDomain.ProcessExit`. | Always wrap `DiagnosticSession` in a C# `using` block. |
