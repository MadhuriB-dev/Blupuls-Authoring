# OdxDiagnosticParser - ODX / PDX Automotive Diagnostic Parser & Excel Generator

A high-performance C# / .NET library and console utility to parse **ODX** (Open Diagnostic Data Exchange / ISO 22901-1) XML files and **PDX** (Packaged Diagnostic Exchange / ISO 22901-2) archives, extracting comprehensive UDS/KWP automotive diagnostic specifications and exporting them into beautifully formatted, multi-worksheet Excel (`.xlsx`) workbooks.

---

## 🌟 Key Features

* **Automotive Diagnostic Data Categorization (Stored Data vs Current Data)**:
  - **Categorized Services & DIDs**: Automatically analyzes ODX semantic tags, UDS service IDs (`0x19`, `0x14`, `0x22`, `0x2A`), DID hex ranges (e.g. `0xF180-0xF1FF` for ISO 14229-1 identification records), and telemetry keywords to classify data into:
    - **`Current Data`**: Real-time telemetry, dynamic measurements, periodic sensor values (Engine Speed, Coolant Temp, Battery Voltage).
    - **`Stored Data`**: Non-volatile memory, DTC fault records, freeze frames, snapshots, identification (VIN, ECU Part Number, Serial Number, SW/HW Versions).
    - **`System Control`**: Diagnostic sessions (`0x10`), Security access (`0x27`), Tester present (`0x3E`), Routine control (`0x31`).
  - **Color-Coded Excel Badges**: Adds a dedicated **"Data Category"** column in the **Diagnostic Services** and **Data Identifiers (DIDs)** sheets with custom styling (Teal badge for Current Data, Amber badge for Stored Data, Purple badge for System Control).
  - **Structured Console Report**: Displays extracted diagnostic data neatly categorized into dedicated sections for `[1] CURRENT DATA`, `[2] STORED DATA`, `[3] SYSTEM & DIAGNOSTIC CONTROL`, and `[5] COMMUNICATION PARAMETERS`.
* **Temporary Ephemeral Diagnostic Sessions (Auto-Cleanup on Exit)**:
  - Supports running isolated diagnostic sessions (`DiagnosticSession`): extracts PDX files and generates the Excel report into a dedicated temporary folder during runtime.
  - Automatically launches the temporary Excel report in the default viewer.
  - **Zero Leftover Footprint**: When the session ends, the console closes, or the process terminates, all temporary extracted files, directories, and the Excel workbook are **automatically deleted from disk**.
  - Provides an optional command to save a permanent copy before closing.
* **Complete PDX Package Decompression**:
  - Safely extracts `.pdx` / `.zip` packages with built-in **Zip-Slip path-traversal vulnerability prevention**.
  - Parses the `index.xml` catalog manifest (vehicles, ECUs, file inventory, MD5 checksums).
  - Automatically discovers and links packaged `.odx-d`, `.odx-c`, `.odx-v`, and `.odx-e` diagnostic files.
* **Resilient ODX XML Parser**:
  - Fully namespace-agnostic: seamlessly processes ODX 2.0.1, ODX 2.2.0, and ISO 22901-1 files with or without XML namespaces.
  - XXE (XML External Entity) hardened reader.
  - Resolves internal `ID-REF` links across Data Object Properties (DOPs), Units, Requests, and Responses.
* **Semantic Automotive Diagnostic Extraction**:
  - **Diagnostic Services (UDS / KWP)**: Service IDs (`0x10`, `0x22`, `0x2E`, `0x19`, `0x14`, `0x27`, `0x31`, `0x3E`), Sub-functions, Semantics, Positive Response SIDs, and Negative Response Codes (NRCs).
  - **Data Identifiers (DIDs)**: 2-byte DID hex & decimal codes, Read / Write / Read-Write access resolution, payload byte/bit lengths, data types, physical engineering units (`rpm`, `degC`, `km/h`, `V`, `mA`), and linear scaling factors/offsets ($y = a \cdot x + b$).
  - **Diagnostic Trouble Codes (DTCs)**: Standard UDS / SAE J2012 alphanumeric display codes (`P0100-13`, `U0100-87`, `B1000-16`), 3-byte trouble code hex and integer values, Failure Type Bytes (FTB), and severity levels.
  - **Communication Parameters (ComParams)**: Physical/Functional CAN IDs, Baud rates, $P2_{max}$, $P2^*_{max}$, and $S3_{client}$ timings.
* **Professional Multi-Sheet Excel Exporter**:
  - Powered by `ClosedXML` — **zero dependency on Microsoft Office or Excel COM Interop**.
  - Modern automotive dark navy header styling (`#1B365D`), zebra striping (`#F8FAFC`), frozen header rows, and native Excel auto-filtering.
  - Separate worksheets for **Overview**, **Diagnostic Services**, **Data Identifiers (DIDs)**, **DTCs (Trouble Codes)**, **Communication Parameters**, and **PDX Manifest**.
* **Self-Contained Sample Generator**:
  - Includes a built-in generator (`SampleOdxGenerator`) that produces realistic ECU diagnostic ODX files and PDX packages out of the box for immediate demonstration and testing.

---

## 📁 Solution Architecture (3 Projects in 1 Solution)

```
Blupuls-Authoring/
├── OdxDiagnosticParser.sln                # Single Visual Studio 2022 Solution (3 Projects)
├── API_DOCUMENTATION.md                   # Complete developer API reference & guide
├── PDX_ODX_PARSING_APPROACH.md            # In-depth technical architecture & specification
├── README.md                              # Project documentation
└── src/
    ├── OdxDiagnosticParser/               # [PROJECT 1] Core Parser Library (net8.0)
    │   ├── Models/                        # Diagnostic domain models
    │   │   ├── DiagnosticPackage.cs       # Root diagnostic package & aggregated layers
    │   │   ├── DiagnosticLayer.cs         # ECU Variants, Base Variants, Protocols
    │   │   ├── DiagnosticService.cs       # UDS Services (0x10, 0x22, 0x19, etc.)
    │   │   ├── DiagnosticParameter.cs     # Request & Response parameters
    │   │   ├── DiagnosticDataIdentifier.cs# DIDs with scaling, units, and access
    │   │   ├── DiagnosticTroubleCode.cs   # DTCs with UDS display codes & FTBs
    │   │   ├── DataObjectProperty.cs      # DOP scaling, text tables, units
    │   │   ├── CommunicationParameter.cs  # CAN IDs, baudrates, bus timings
    │   │   └── PdxCatalog.cs              # index.xml manifest catalog
    │   ├── Parsers/                       # Extraction & XML parsing engines
    │   │   ├── IOdxParser.cs & OdxXmlParser.cs
    │   │   ├── IPdxExtractor.cs & PdxPackageExtractor.cs
    │   │   └── OdxXmlExtensions.cs        # Namespace-tolerant XML extensions
    │   ├── Exporters/                     # Excel generation engine
    │   │   ├── IOdxExcelExporter.cs
    │   │   ├── OdxExcelExporter.cs        # ClosedXML multi-sheet generator
    │   │   └── ExcelExportOptions.cs      # Styling & sheet configuration
    │   └── Samples/                       # Built-in sample generator
    │       └── SampleOdxGenerator.cs
    ├── OdxDiagnosticParser.Tester/        # [PROJECT 2] Connected Console Tester & Test Runner
    │   ├── Program.cs                     # Interactive CLI menu & command routing
    │   └── Tests/                         # Comprehensive parser part test functions
    │       ├── TestRunner.cs              # Lightweight test execution engine
    │       └── ParserPartTests.cs         # 15 dedicated test functions for every parser part
    └── Bluepuls_Authoring_Tool/           # [PROJECT 3] WPF Desktop Authoring Tool (net8.0-windows)
        ├── Views/                         # MainWindow UI with DLC hierarchy & toolbars
        ├── ViewModels/                    # MainViewModel with ODX/PDX import & export
        ├── Models/                        # DlcTreeNode, DiagnosticLogItem
        └── Utilities/                     # RelayCommand, ObservableObject
```
            └── ParserPartTests.cs         # Dedicated test functions for every parser part
```

---

## 🚀 Quick Start & Usage

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* Visual Studio 2022, Visual Studio Code, or Rider (optional)

### Build Solution
```powershell
dotnet build
```

### Run Parser Test Suite (Inside Tester Project)
```powershell
dotnet run --project src/OdxDiagnosticParser.Tester -- test
```

---

## 💻 Console Tester Project

The separate tester project (`OdxDiagnosticParser.Tester`) provides an interactive CLI and command-line arguments to test all library features.

### 1. Run Full End-to-End Demo
Automatically creates sample ODX and PDX packages, extracts diagnostic layers, displays formatted summaries in the console, and generates styled Excel reports:
```powershell
dotnet run --project src/OdxDiagnosticParser.Tester -- demo
```

### 2. Convert Single ODX File to Excel
```powershell
dotnet run --project src/OdxDiagnosticParser.Tester -- parse-odx "path/to/ECU.odx-d" "path/to/Report.xlsx"
```

### 3. Unpack and Inspect PDX Archive
Extracts archive contents and displays the `index.xml` manifest in the console:
```powershell
dotnet run --project src/OdxDiagnosticParser.Tester -- unpack-pdx "path/to/Package.pdx" "path/to/DestinationFolder"
```

### 4. Extract and Convert PDX Package Directly to Excel
```powershell
dotnet run --project src/OdxDiagnosticParser.Tester -- pdx-to-excel "path/to/Package.pdx" "path/to/DiagnosticReport.xlsx"
```

### 5. Interactive Menu Mode
Run without arguments to access the guided interactive menu:
```powershell
dotnet run --project src/OdxDiagnosticParser.Tester
```

---

## 🛠️ C# Code Integration Examples

### Example 1: Convert an ODX XML File to Excel
```csharp
using OdxDiagnosticParser.Exporters;
using OdxDiagnosticParser.Parsers;

var parser = new OdxXmlParser();
var exporter = new OdxExcelExporter();

// Parse ODX XML file
var package = parser.ParseFile("EngineControlUnit.odx-d");

// Export to multi-sheet Excel
exporter.ExportToExcel(package, "EngineDiagnostics.xlsx");
```

### Example 2: Extract a PDX Archive and Generate Excel
```csharp
using OdxDiagnosticParser.Exporters;
using OdxDiagnosticParser.Parsers;

var extractor = new PdxPackageExtractor();
var exporter = new OdxExcelExporter();

// Unpack PDX, parse index.xml, resolve all packaged ODX layers
var package = extractor.ExtractAndParse("Powertrain_EMS_2026.pdx");

// Export consolidated diagnostic workbook
exporter.ExportToExcel(package, "Powertrain_EMS_Report.xlsx");
```

---

## 📊 Generated Excel Workbook Structure

1. **Overview**: Executive package summary, extraction timestamp, vehicle model, ECU diagnostic address, metrics breakdown for Current Data vs Stored Data, and diagnostic layer overview.
2. **Diagnostic Services**: ECU Layer, **Data Category** ("Current Data" / "Stored Data" / "System Control"), UDS Service ID (Hex), Service Name, Semantic, Sub-Function, Positive Response SID, Parameter counts, NRCs, Description.
3. **Data Identifiers (DIDs)**: ECU Layer, **Data Category** ("Current Data" vs "Stored Data"), DID Hex (`0xF190`), DID Decimal, Name, Read/Write Access, Length, Data Type, Physical Engineering Unit (`rpm`, `degC`, `V`), Linear Scaling Formula, Min/Max Limits.
4. **DTCs (Trouble Codes)**: UDS Display Code (`P0100-13`), Trouble Code Hex, Trouble Code Decimal, Failure Type Byte (FTB), Severity, Text Description.
5. **Communication Parameters**: Parameter Name (`CP_CanId`, `CP_Baudrate`), Value, Unit, Protocol.
6. **PDX Manifest**: Internal catalog files, file types, relative paths, sizes, and MD5 checksums.

---

## 📖 In-Depth Approach & Complete API Documentation
* **Complete Developer API Reference & Integration Guide**: **[API_DOCUMENTATION.md](API_DOCUMENTATION.md)** (Every API, parameter, return type, and invocation pattern used across the console tester).
* **Technical Architecture & Specifications**: **[PDX_ODX_PARSING_APPROACH.md](PDX_ODX_PARSING_APPROACH.md)** (ASAM MCD-2 D, ISO 22901-1 / ISO 22901-2 standard compliance, Zip-Slip algorithms, and UDS DTC math).

