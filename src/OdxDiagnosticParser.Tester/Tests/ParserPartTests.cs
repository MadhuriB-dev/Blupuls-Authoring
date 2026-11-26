using System.IO.Compression;
using System.Security;
using ClosedXML.Excel;
using OdxDiagnosticParser.Exporters;
using OdxDiagnosticParser.Models;
using OdxDiagnosticParser.Parsers;
using OdxDiagnosticParser.Samples;
using OdxDiagnosticParser.Sessions;

namespace OdxDiagnosticParser.Tester.Tests;

#nullable disable

/// <summary>
/// Contains dedicated test functions that test every component and function of OdxDiagnosticParser.
/// </summary>
public static class ParserPartTests
{
    private static readonly IOdxParser OdxParser = new OdxXmlParser();
    private static readonly IPdxExtractor PdxExtractor = new PdxPackageExtractor(OdxParser);
    private static readonly IOdxExcelExporter ExcelExporter = new OdxExcelExporter();

    public static void RunAll(TestRunner runner)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n================================================================================");
        Console.WriteLine("    RUNNING COMPREHENSIVE PARSER TEST SUITE (INSIDE TESTER PROJECT)");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        runner.RunTest("1. Diagnostic Layers Extraction", TestOdxDiagnosticLayers);
        runner.RunTest("2. UDS Diagnostic Services (0x10, 0x22, 0x2E, 0x14, 0x19, 0x27)", TestUdsServicesParsing);
        runner.RunTest("3. DIDs, Scaling Factors, Physical Units & Access Rights", TestDidsAndScaling);
        runner.RunTest("4. DTCs, UDS Display Codes & Failure Type Bytes (FTB)", TestDtcsAndFailureTypeBytes);
        runner.RunTest("5. Communication Parameters (ComParams) & Bus Timings", TestCommunicationParameters);
        runner.RunTest("6. XML Namespace Resilience (ODX 2.0.1 / 2.2.0 / xmlns)", TestXmlNamespaceResilience);
        runner.RunTest("7. PDX Archive Safe Decompression & Folder Structure", TestPdxSafeExtraction);
        runner.RunTest("8. PDX Security Defense (Zip-Slip Path Traversal Protection)", TestPdxZipSlipSecurityDefense);
        runner.RunTest("9. PDX Catalog Manifest Parsing (index.xml)", TestPdxCatalogParsing);
        runner.RunTest("10. Multi-Sheet Excel Workbook Generation & Styling", TestExcelWorkbookGeneration);
        runner.RunTest("11. Memory Stream Excel Export (ExportToBytes)", TestExcelExportToBytes);
        runner.RunTest("12. Full End-to-End Pipeline (PDX -> Parse -> Excel)", TestFullEndToEndPipeline);
        runner.RunTest("13. Temporary Diagnostic Session Lifecycle & Auto-Cleanup", TestDiagnosticSessionLifecycle);
        runner.RunTest("14. Diagnostic Session Permanent Copy Saving", TestDiagnosticSessionSavePermanentCopy);
        runner.RunTest("15. Stored vs Current Diagnostic Data Categorization", TestStoredAndCurrentDataCategorization);
        runner.RunTest("16. ISO 22901-2 ABLOCKs & Cross-File ODX-C/E/D/V Linking", TestIsoStandardPdxAndOdxCrossLinking);
        runner.RunTest("17. ISO 22901-1 Inheritance Exclusions, Patterns & Consistency", TestIsoInheritanceAndConsistencyValidation);
        runner.RunTest("18. ISO Authoring Round-Trip (DID/DOP/Service/DTC/ComParam) & RelinkPackage", TestIsoAuthoringRoundTripAndRelinking);

        runner.PrintSummary();
    }

    public static void TestOdxDiagnosticLayers()
    {
        var xml = SampleOdxGenerator.CreateSampleOdxXml("EngineControlUnit");
        var package = OdxParser.ParseXml(xml, "EngineControlUnit.odx-d");

        TestRunner.AssertNotNull(package, "DiagnosticPackage should not be null.");
        TestRunner.AssertEqual(1, package.Layers.Count, "Should parse exactly 1 layer.");
        var layer = package.Layers[0];
        TestRunner.AssertEqual("EngineControlUnit", layer.ShortName);
        TestRunner.AssertEqual(DiagnosticLayerType.EcuVariant, layer.LayerType);
        TestRunner.AssertEqual("2.2.0", layer.ModelVersion);
    }

    public static void TestUdsServicesParsing()
    {
        var xml = SampleOdxGenerator.CreateSampleOdxXml("EngineControlUnit");
        var package = OdxParser.ParseXml(xml);
        var layer = package.Layers[0];

        TestRunner.AssertNotEmpty(layer.Services, "Services collection must not be empty.");

        // DiagnosticSessionControl (0x10)
        var sessionSvc = layer.Services.FirstOrDefault(s => s.ServiceIdHex == "0x10");
        TestRunner.AssertNotNull(sessionSvc, "0x10 DiagnosticSessionControl must exist.");
        TestRunner.AssertEqual("DiagnosticSessionControl", sessionSvc.ShortName);
        TestRunner.AssertEqual("SESSION", sessionSvc.Semantic);
        TestRunner.AssertEqual("0x50", sessionSvc.PositiveResponseSidHex);

        // SecurityAccess (0x27)
        var secSvc = layer.Services.FirstOrDefault(s => s.ServiceIdHex == "0x27");
        TestRunner.AssertNotNull(secSvc, "0x27 SecurityAccess must exist.");
        TestRunner.AssertEqual("0x67", secSvc.PositiveResponseSidHex);

        // ReadDataByIdentifier (0x22)
        var readDids = layer.Services.Where(s => s.ServiceIdHex == "0x22").ToList();
        TestRunner.AssertTrue(readDids.Count >= 3, "Should have at least 3 ReadDataByIdentifier services.");

        // WriteDataByIdentifier (0x2E)
        var writeDids = layer.Services.Where(s => s.ServiceIdHex == "0x2E").ToList();
        TestRunner.AssertTrue(writeDids.Count >= 1, "Should have at least 1 WriteDataByIdentifier service.");

        // ClearDiagnosticInformation (0x14)
        var clearSvc = layer.Services.FirstOrDefault(s => s.ServiceIdHex == "0x14");
        TestRunner.AssertNotNull(clearSvc, "0x14 ClearDiagnosticInformation must exist.");
        TestRunner.AssertEqual("0x54", clearSvc.PositiveResponseSidHex);

        // ReadDTCInformation (0x19)
        var readDtcSvc = layer.Services.FirstOrDefault(s => s.ServiceIdHex == "0x19");
        TestRunner.AssertNotNull(readDtcSvc, "0x19 ReadDTCInformation must exist.");
        TestRunner.AssertEqual("0x59", readDtcSvc.PositiveResponseSidHex);
    }

    public static void TestDidsAndScaling()
    {
        var xml = SampleOdxGenerator.CreateSampleOdxXml("EngineControlUnit");
        var package = OdxParser.ParseXml(xml);
        var layer = package.Layers[0];

        TestRunner.AssertNotEmpty(layer.DataIdentifiers, "DataIdentifiers should not be empty.");

        // 1. VIN (0xF190): has both Read (0x22) and Write (0x2E) -> Access must be "Read/Write"
        var vinDid = layer.DataIdentifiers.FirstOrDefault(d => d.DidHex.Equals("0xF190", StringComparison.OrdinalIgnoreCase));
        TestRunner.AssertNotNull(vinDid, "VIN DID 0xF190 must be present.");
        TestRunner.AssertEqual("Read/Write", vinDid.Access);
        TestRunner.AssertEqual(17, vinDid.ByteLength); // 136 bits = 17 bytes
        TestRunner.AssertEqual("A_ASCIISTRING", vinDid.DataType);

        // 2. Engine Coolant Temperature (0x0105): Factor 1.0, Offset -40, Unit degC, Access Read
        var coolantDid = layer.DataIdentifiers.FirstOrDefault(d => d.DidHex.Equals("0x0105", StringComparison.OrdinalIgnoreCase));
        TestRunner.AssertNotNull(coolantDid, "Coolant Temp DID 0x0105 must be present.");
        TestRunner.AssertEqual("degC", coolantDid.Unit);
        TestRunner.AssertEqual(-40.0, coolantDid.Offset);
        TestRunner.AssertEqual(1.0, coolantDid.ScalingFactor);
        TestRunner.AssertEqual("Read", coolantDid.Access);

        // 3. Engine Speed (0x010C): Factor 0.25, Unit rpm
        var rpmDid = layer.DataIdentifiers.FirstOrDefault(d => d.DidHex.Equals("0x010C", StringComparison.OrdinalIgnoreCase));
        TestRunner.AssertNotNull(rpmDid, "Engine Speed DID 0x010C must be present.");
        TestRunner.AssertEqual("rpm", rpmDid.Unit);
        TestRunner.AssertEqual(0.25, rpmDid.ScalingFactor);

        // 4. Battery Voltage (0x0142): Factor 0.001, Unit V
        var voltDid = layer.DataIdentifiers.FirstOrDefault(d => d.DidHex.Equals("0x0142", StringComparison.OrdinalIgnoreCase));
        TestRunner.AssertNotNull(voltDid, "Battery Voltage DID 0x0142 must be present.");
        TestRunner.AssertEqual("V", voltDid.Unit);
        TestRunner.AssertEqual(0.001, voltDid.ScalingFactor);
    }

    public static void TestDtcsAndFailureTypeBytes()
    {
        var xml = SampleOdxGenerator.CreateSampleOdxXml("EngineControlUnit");
        var package = OdxParser.ParseXml(xml);
        var layer = package.Layers[0];

        TestRunner.AssertNotEmpty(layer.DiagnosticTroubleCodes, "DiagnosticTroubleCodes must not be empty.");

        // P0100-13
        var dtc1 = layer.DiagnosticTroubleCodes.FirstOrDefault(d => d.DisplayCode.Contains("P0100"));
        TestRunner.AssertNotNull(dtc1, "DTC P0100-13 must be present.");
        TestRunner.AssertEqual("P0100-13", dtc1.DisplayCode);
        TestRunner.AssertEqual("0x13", dtc1.FailureTypeByte);
        TestRunner.AssertTrue(dtc1.Description.Contains("Air Flow"), "Description should describe Air Flow sensor.");
        TestRunner.AssertEqual("Warning", dtc1.Severity);

        // U0100-87
        var dtcU = layer.DiagnosticTroubleCodes.FirstOrDefault(d => d.DisplayCode.Contains("U0100"));
        TestRunner.AssertNotNull(dtcU, "DTC U0100-87 must be present.");
        TestRunner.AssertEqual("Fatal", dtcU.Severity);
    }

    public static void TestCommunicationParameters()
    {
        var xml = SampleOdxGenerator.CreateSampleOdxXml("EngineControlUnit");
        var package = OdxParser.ParseXml(xml);
        var layer = package.Layers[0];

        TestRunner.AssertNotEmpty(layer.CommunicationParameters, "CommunicationParameters should not be empty.");

        var canId = layer.CommunicationParameters.FirstOrDefault(c => c.Name.Contains("CP_CanId"));
        TestRunner.AssertNotNull(canId, "CP_CanId must be present.");
        TestRunner.AssertEqual("0x7E0", canId.Value);

        var baud = layer.CommunicationParameters.FirstOrDefault(c => c.Name.Contains("CP_Baudrate"));
        TestRunner.AssertNotNull(baud, "CP_Baudrate must be present.");
        TestRunner.AssertEqual("500000", baud.Value);
    }

    public static void TestXmlNamespaceResilience()
    {
        var rawXml = SampleOdxGenerator.CreateSampleOdxXml("NamespacedEcu");
        var namespacedXml = rawXml.Replace(
            @"<ODX MODEL-VERSION=""2.2.0""",
            @"<ODX MODEL-VERSION=""2.2.0"" xmlns=""http://iso.org/iso22901"""
        );

        var package = OdxParser.ParseXml(namespacedXml);

        TestRunner.AssertNotNull(package, "Package should parse successfully with XML namespace.");
        TestRunner.AssertEqual(1, package.Layers.Count);
        TestRunner.AssertEqual("NamespacedEcu", package.Layers[0].ShortName);
        TestRunner.AssertNotEmpty(package.Layers[0].Services);
        TestRunner.AssertNotEmpty(package.Layers[0].DataIdentifiers);
        TestRunner.AssertNotEmpty(package.Layers[0].DiagnosticTroubleCodes);
    }

    public static void TestPdxSafeExtraction()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "PdxExtractTest_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);
            var pdxPath = Path.Combine(tempDir, "TestPackage.pdx");
            SampleOdxGenerator.CreateSamplePdxFile(pdxPath, "TestECU");

            var extractTarget = Path.Combine(tempDir, "Extracted");
            var resultDir = PdxExtractor.UnzipPdx(pdxPath, extractTarget);

            TestRunner.AssertTrue(Directory.Exists(resultDir), "Extracted directory must exist.");
            TestRunner.AssertTrue(File.Exists(Path.Combine(resultDir, "index.xml")), "index.xml must be extracted.");
            TestRunner.AssertTrue(File.Exists(Path.Combine(resultDir, "odx-d", "TestECU.odx-d")), "odx-d file must be extracted.");
            TestRunner.AssertTrue(File.Exists(Path.Combine(resultDir, "odx-c", "Protocols_UDS.odx-c")), "odx-c file must be extracted.");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    public static void TestPdxZipSlipSecurityDefense()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ZipSlipTest_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);
            var maliciousZipPath = Path.Combine(tempDir, "malicious.pdx");

            using (var zipStream = new FileStream(maliciousZipPath, FileMode.Create))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                var maliciousEntry = archive.CreateEntry("../../MaliciousEscape.txt");
                using var writer = new StreamWriter(maliciousEntry.Open());
                writer.WriteLine("Exploit payload");
            }

            var safeTargetDir = Path.Combine(tempDir, "TargetFolder");

            TestRunner.AssertThrows<SecurityException>(() =>
            {
                PdxExtractor.UnzipPdx(maliciousZipPath, safeTargetDir);
            }, "PdxPackageExtractor must throw SecurityException on path traversal attempt.");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    public static void TestPdxCatalogParsing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "CatalogTest_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);
            var pdxPath = Path.Combine(tempDir, "Transmission.pdx");
            SampleOdxGenerator.CreateSamplePdxFile(pdxPath, "TransmissionControlUnit");

            var extractDir = Path.Combine(tempDir, "Extract");
            PdxExtractor.UnzipPdx(pdxPath, extractDir);

            var catalog = PdxExtractor.ParseCatalog(extractDir);

            TestRunner.AssertNotNull(catalog, "Parsed catalog should not be null.");
            TestRunner.AssertTrue(catalog.ShortName.Contains("TransmissionControlUnit"), "Catalog short name must match ECU.");
            TestRunner.AssertNotEmpty(catalog.Vehicles, "Catalog must list vehicles.");
            TestRunner.AssertEqual("BlueBinaries GT-2026", catalog.Vehicles[0].Model);
            TestRunner.AssertNotEmpty(catalog.Ecus, "Catalog must list ECUs.");
            TestRunner.AssertEqual("TransmissionControlUnit", catalog.Ecus[0].EcuName);
            TestRunner.AssertTrue(catalog.Files.Count >= 2, "Catalog should list at least 2 packaged files.");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    public static void TestExcelWorkbookGeneration()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ExcelTest_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);
            var xml = SampleOdxGenerator.CreateSampleOdxXml("EngineControlUnit");
            var package = OdxParser.ParseXml(xml);
            var excelPath = Path.Combine(tempDir, "TestReport.xlsx");

            ExcelExporter.ExportToExcel(package, excelPath);

            TestRunner.AssertTrue(File.Exists(excelPath), "Excel file must exist on disk.");
            TestRunner.AssertTrue(new FileInfo(excelPath).Length > 1000, "Excel file must have non-zero size.");

            // Inspect worksheets using ClosedXML
            using var workbook = new XLWorkbook(excelPath);
            TestRunner.AssertTrue(workbook.Worksheets.Contains("Overview"), "Must contain Overview sheet.");
            TestRunner.AssertTrue(workbook.Worksheets.Contains("Diagnostic Services"), "Must contain Services sheet.");
            TestRunner.AssertTrue(workbook.Worksheets.Contains("Data Identifiers (DIDs)"), "Must contain DIDs sheet.");
            TestRunner.AssertTrue(workbook.Worksheets.Contains("DTCs (Trouble Codes)"), "Must contain DTCs sheet.");
            TestRunner.AssertTrue(workbook.Worksheets.Contains("Communication Parameters"), "Must contain ComParams sheet.");

            var svcSheet = workbook.Worksheet("Diagnostic Services");
            TestRunner.AssertTrue(svcSheet.LastRowUsed()?.RowNumber() >= 6, "Services sheet must contain rows.");

            var didSheet = workbook.Worksheet("Data Identifiers (DIDs)");
            TestRunner.AssertTrue(didSheet.LastRowUsed()?.RowNumber() >= 4, "DIDs sheet must contain rows.");

            var dtcSheet = workbook.Worksheet("DTCs (Trouble Codes)");
            TestRunner.AssertTrue(dtcSheet.LastRowUsed()?.RowNumber() >= 5, "DTCs sheet must contain rows.");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    public static void TestExcelExportToBytes()
    {
        var xml = SampleOdxGenerator.CreateSampleOdxXml("TestEcu");
        var package = OdxParser.ParseXml(xml);

        var bytes = ExcelExporter.ExportToBytes(package);

        TestRunner.AssertNotNull(bytes, "Byte array must not be null.");
        TestRunner.AssertTrue(bytes.Length > 1000, "Byte array must be a valid XLSX stream.");

        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);
        TestRunner.AssertNotNull(workbook.Worksheet("Overview"), "Must be able to open workbook from memory.");
    }

    public static void TestFullEndToEndPipeline()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "E2ETest_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);
            var pdxFile = Path.Combine(tempDir, "Powertrain.pdx");
            SampleOdxGenerator.CreateSamplePdxFile(pdxFile, "EngineControlUnit");

            // Extract & parse
            var package = PdxExtractor.ExtractAndParse(pdxFile);
            TestRunner.AssertNotNull(package);
            TestRunner.AssertTrue(package.IsFromPdx);

            // Export to Excel
            var excelFile = Path.Combine(tempDir, "Powertrain_Report.xlsx");
            ExcelExporter.ExportToExcel(package, excelFile);

            TestRunner.AssertTrue(File.Exists(excelFile));

            // Validate workbook
            using var workbook = new XLWorkbook(excelFile);
            TestRunner.AssertTrue(workbook.Worksheets.Contains("PDX Manifest"), "Must contain PDX Manifest sheet.");
            var manifestSheet = workbook.Worksheet("PDX Manifest");
            TestRunner.AssertTrue(manifestSheet.LastRowUsed()?.RowNumber() >= 3, "Manifest sheet must have file entries.");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    public static void TestDiagnosticSessionLifecycle()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SessionTest_" + Guid.NewGuid().ToString("N")[..8]);
        string sessionDir;
        string tempExcel;

        try
        {
            Directory.CreateDirectory(tempDir);
            var pdxFile = Path.Combine(tempDir, "SessionEcu.pdx");
            SampleOdxGenerator.CreateSamplePdxFile(pdxFile, "SessionEngineControlUnit");

            // 1. Create Session
            using (var session = DiagnosticSession.CreateFromPdx(pdxFile, tempDir))
            {
                sessionDir = session.SessionDirectory;
                tempExcel = session.TemporaryExcelFilePath;

                TestRunner.AssertTrue(session.IsActive, "Session should be active.");
                TestRunner.AssertTrue(Directory.Exists(sessionDir), "Session directory must exist while running.");
                TestRunner.AssertTrue(Directory.Exists(session.ExtractedDirectory), "Extracted directory must exist while running.");
                TestRunner.AssertTrue(File.Exists(tempExcel), "Temporary Excel file must exist while running.");
                TestRunner.AssertNotNull(session.Package, "Diagnostic package must be parsed.");
                TestRunner.AssertTrue(session.Package.Layers.Count > 0, "Diagnostic layers must be populated.");
            }

            // 2. After Dispose: Everything must be deleted
            TestRunner.AssertFalse(Directory.Exists(sessionDir), "Session directory must be automatically deleted after session ends.");
            TestRunner.AssertFalse(File.Exists(tempExcel), "Temporary Excel file must be deleted after session ends.");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    public static void TestDiagnosticSessionSavePermanentCopy()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SessionSaveTest_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);
            var pdxFile = Path.Combine(tempDir, "SaveEcu.pdx");
            SampleOdxGenerator.CreateSamplePdxFile(pdxFile, "SaveEngineControlUnit");

            var permanentExcel = Path.Combine(tempDir, "PermanentReport.xlsx");
            string sessionDir;

            using (var session = DiagnosticSession.CreateFromPdx(pdxFile, tempDir))
            {
                sessionDir = session.SessionDirectory;
                session.SavePermanentCopy(permanentExcel);
                TestRunner.AssertTrue(File.Exists(permanentExcel), "Permanent Excel file must be created.");
            }

            // Session directory is cleaned up, but permanent copy remains
            TestRunner.AssertFalse(Directory.Exists(sessionDir), "Session directory must be deleted.");
            TestRunner.AssertTrue(File.Exists(permanentExcel), "Permanent Excel file must remain after session ends.");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    public static void TestStoredAndCurrentDataCategorization()
    {
        var xml = SampleOdxGenerator.CreateSampleOdxXml("EngineControlUnit");
        var package = OdxParser.ParseXml(xml);
        var layer = package.Layers[0];

        // 1. Verify Service Categorization
        var vinService = layer.Services.FirstOrDefault(s => s.ShortName.Contains("VIN"));
        TestRunner.AssertNotNull(vinService, "VIN service must exist.");
        TestRunner.AssertEqual("Stored Data", vinService.DataCategory, "VIN service must be categorized as Stored Data.");

        var dtcService = layer.Services.FirstOrDefault(s => s.ServiceIdHex == "0x19");
        TestRunner.AssertNotNull(dtcService, "0x19 ReadDTCInformation must exist.");
        TestRunner.AssertEqual("Stored Data", dtcService.DataCategory, "0x19 ReadDTCInformation must be categorized as Stored Data.");

        var clearDtcService = layer.Services.FirstOrDefault(s => s.ServiceIdHex == "0x14");
        TestRunner.AssertNotNull(clearDtcService, "0x14 ClearDiagnosticInformation must exist.");
        TestRunner.AssertEqual("Stored Data", clearDtcService.DataCategory, "0x14 ClearDiagnosticInformation must be categorized as Stored Data.");

        var engineSpeedService = layer.Services.FirstOrDefault(s => s.ShortName.Contains("EngineSpeed"));
        TestRunner.AssertNotNull(engineSpeedService, "EngineSpeed service must exist.");
        TestRunner.AssertEqual("Current Data", engineSpeedService.DataCategory, "EngineSpeed service must be categorized as Current Data.");

        var coolantTempService = layer.Services.FirstOrDefault(s => s.ShortName.Contains("CoolantTemp"));
        TestRunner.AssertNotNull(coolantTempService, "CoolantTemp service must exist.");
        TestRunner.AssertEqual("Current Data", coolantTempService.DataCategory, "CoolantTemp service must be categorized as Current Data.");

        var sessionService = layer.Services.FirstOrDefault(s => s.ServiceIdHex == "0x10");
        TestRunner.AssertNotNull(sessionService, "0x10 DiagnosticSessionControl must exist.");
        TestRunner.AssertEqual("System Control", sessionService.DataCategory, "0x10 must be categorized as System Control.");

        // 2. Verify DID Categorization
        var vinDid = layer.DataIdentifiers.FirstOrDefault(d => d.DidHex == "0xF190");
        TestRunner.AssertNotNull(vinDid, "0xF190 DID must exist.");
        TestRunner.AssertEqual("Stored Data", vinDid.DataCategory, "0xF190 VIN DID must be categorized as Stored Data.");

        var rpmDid = layer.DataIdentifiers.FirstOrDefault(d => d.DidHex == "0x010C");
        TestRunner.AssertNotNull(rpmDid, "0x010C DID must exist.");
        TestRunner.AssertEqual("Current Data", rpmDid.DataCategory, "0x010C EngineSpeed DID must be categorized as Current Data.");

        var tempDid = layer.DataIdentifiers.FirstOrDefault(d => d.DidHex == "0x0105");
        TestRunner.AssertNotNull(tempDid, "0x0105 DID must exist.");
        TestRunner.AssertEqual("Current Data", tempDid.DataCategory, "0x0105 CoolantTemp DID must be categorized as Current Data.");

        // 3. Verify Excel Export has Data Category column
        var tempFile = Path.Combine(Path.GetTempPath(), "CatTest_" + Guid.NewGuid().ToString("N")[..8] + ".xlsx");
        try
        {
            ExcelExporter.ExportToExcel(package, tempFile);
            using var workbook = new XLWorkbook(tempFile);
            var svcSheet = workbook.Worksheet("Diagnostic Services");
            TestRunner.AssertEqual("Data Category", svcSheet.Cell(1, 2).GetString(), "Services sheet Column 2 header must be 'Data Category'.");

            bool foundStored = false;
            bool foundCurrent = false;
            int lastRow = svcSheet.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = 2; r <= lastRow; r++)
            {
                var val = svcSheet.Cell(r, 2).GetString();
                if (val == "Stored Data") foundStored = true;
                if (val == "Current Data") foundCurrent = true;
            }
            TestRunner.AssertTrue(foundStored, "Excel Services tab must contain 'Stored Data' rows.");
            TestRunner.AssertTrue(foundCurrent, "Excel Services tab must contain 'Current Data' rows.");

            var didSheet = workbook.Worksheet("Data Identifiers (DIDs)");
            TestRunner.AssertEqual("Data Category", didSheet.Cell(1, 2).GetString(), "DIDs sheet Column 2 header must be 'Data Category'.");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public static void TestIsoStandardPdxAndOdxCrossLinking()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "IsoLinkTest_" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);
            var pdxPath = Path.Combine(tempDir, "IsoPowertrain.pdx");
            SampleOdxGenerator.CreateSamplePdxFile(pdxPath, "EngineControlUnit");

            var package = PdxExtractor.ExtractAndParse(pdxPath);

            // 1. ISO 22901-2 ABLOCKS
            TestRunner.AssertNotNull(package.Catalog, "Catalog must not be null.");
            TestRunner.AssertTrue(package.Catalog.Ablocks.Count >= 4, "ISO 22901-2 catalog must contain at least 4 ABLOCKs (ODX-C, ODX-E, ODX-D, ODX-V).");
            TestRunner.AssertTrue(package.Catalog.Ablocks.Any(a => a.Category == "ODX-COMPARAM-SPEC"), "Must contain ODX-COMPARAM-SPEC ABLOCK.");
            TestRunner.AssertTrue(package.Catalog.Ablocks.Any(a => a.Category == "ODX-VEHICLE-INFO"), "Must contain ODX-VEHICLE-INFO ABLOCK.");

            // 2. ODX-C ComParamSpec & Protocol linkage
            TestRunner.AssertNotEmpty(package.ComParamSpecs, "ODX-C ComParamSpecs must be parsed.");
            var proto = package.Protocols.FirstOrDefault(p => p.ShortName == "PR_UDSonCAN");
            TestRunner.AssertNotNull(proto, "Protocol PR_UDSonCAN must be parsed.");
            TestRunner.AssertTrue(proto.CommunicationParameters.Any(c => c.Name == "CP_P2Max"), "Protocol must inherit CP_P2Max from ODX-C COMPARAM-SPEC.");

            // 3. ODX-V Vehicle Topology & Logical Links
            TestRunner.AssertNotEmpty(package.VehicleInfos, "ODX-V VehicleInfos must be parsed.");
            var ll = package.VehicleInfos[0].LogicalLinks.FirstOrDefault(l => l.ShortName == "LL_EngineControlUnit");
            TestRunner.AssertNotNull(ll, "LogicalLink LL_EngineControlUnit must exist.");
            TestRunner.AssertEqual("BV_EngineControlUnit", ll.ResolvedBaseVariantName, "LogicalLink must resolve BaseVariant.");
            TestRunner.AssertEqual("PR_UDSonCAN", ll.ResolvedProtocolName, "LogicalLink must resolve Protocol.");

            // 4. Cross-File & Multi-Layer PARENT-REF Inheritance onto ECU Variants
            var gasVariant = package.EcuVariants.FirstOrDefault(ev => ev.ShortName == "EngineControlUnit_Gasoline");
            TestRunner.AssertNotNull(gasVariant, "ECU Variant EngineControlUnit_Gasoline must exist.");
            TestRunner.AssertTrue(gasVariant.InheritanceResolved, "ECU Variant inheritance must be resolved.");

            // Must have inherited services from Shared Data (SD_15765_3_Services), Functional Group (FG_Powertrain), BaseVariant (BV_EngineControlUnit)
            TestRunner.AssertTrue(gasVariant.Services.Any(s => s.ShortName == "DiagnosticSessionControl" && s.IsInherited), "Must inherit 0x10 SessionControl from SharedData via BaseVariant.");
            TestRunner.AssertTrue(gasVariant.Services.Any(s => s.ShortName == "ReadDataByIdentifier_VIN" && s.IsInherited && s.DefinedInLayer == "BV_EngineControlUnit"), "Must inherit ReadVIN from BaseVariant.");
            TestRunner.AssertTrue(gasVariant.Services.Any(s => s.ShortName == "ReadDataByIdentifier_FuelRailPressure" && !s.IsInherited), "Must retain own Gasoline FuelRailPressure service.");

            // Verify cross-file UNIT-REF resolution (DOP_FUEL_PRESSURE -> DLC_SD_15765_3 UNIT_KPA -> "kPa")
            var fuelDid = gasVariant.DataIdentifiers.FirstOrDefault(d => d.DidHex == "0x0123");
            TestRunner.AssertNotNull(fuelDid, "Gasoline variant must expose DID 0x0123 FuelRailPressure.");
            TestRunner.AssertEqual("kPa", fuelDid.Unit, "Cross-file UNIT-REF UNIT_KPA must resolve to 'kPa'.");

            // Zero consistency errors/warnings on compliant package
            TestRunner.AssertEqual(0, package.ConsistencyIssues.Count, "ISO-compliant sample PDX should produce 0 consistency issues.");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    public static void TestIsoInheritanceAndConsistencyValidation()
    {
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ODX MODEL-VERSION=""2.2.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <DIAG-LAYER-CONTAINER ID=""DLC_TEST"">
    <SHORT-NAME>DLC_TEST</SHORT-NAME>
    <BASE-VARIANTS>
      <BASE-VARIANT ID=""BV_TEST"">
        <SHORT-NAME>BV_TEST</SHORT-NAME>
        <DIAG-COMMS>
          <DIAG-SERVICE ID=""S1"" SEMANTIC=""SESSION""><SHORT-NAME>KeepService</SHORT-NAME><REQUEST><PARAMS><PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x10</CODED-VALUE></PARAM></PARAMS></REQUEST></DIAG-SERVICE>
          <DIAG-SERVICE ID=""S2"" SEMANTIC=""ROUTINE""><SHORT-NAME>ExcludedService</SHORT-NAME><REQUEST><PARAMS><PARAM SEMANTIC=""SERVICE-ID"" xsi:type=""CODED-CONST""><BYTE-POSITION>0</BYTE-POSITION><CODED-VALUE>0x31</CODED-VALUE></PARAM></PARAMS></REQUEST></DIAG-SERVICE>
        </DIAG-COMMS>
      </BASE-VARIANT>
    </BASE-VARIANTS>
    <ECU-VARIANTS>
      <ECU-VARIANT ID=""EV_TEST"">
        <SHORT-NAME>EV_TEST</SHORT-NAME>
        <PARENT-REFS>
          <PARENT-REF xsi:type=""BASE-VARIANT-REF"" ID-REF=""BV_TEST"">
            <NOT-INHERITED-DIAG-COMMS>
              <NOT-INHERITED-DIAG-COMM><DIAG-COMM-SNREF SHORT-NAME=""ExcludedService""/></NOT-INHERITED-DIAG-COMM>
            </NOT-INHERITED-DIAG-COMMS>
          </PARENT-REF>
          <PARENT-REF xsi:type=""PROTOCOL-REF"" ID-REF=""MISSING_PROTO_REF""/>
        </PARENT-REFS>
        <ECU-VARIANT-PATTERNS>
          <ECU-VARIANT-PATTERN>
            <SHORT-NAME>Pat_Test</SHORT-NAME>
            <MATCHING-PARAMETERS>
              <MATCHING-PARAMETER>
                <EXPECTED-VALUE>V99</EXPECTED-VALUE>
                <DIAG-COMM-SNREF SHORT-NAME=""KeepService""/>
                <OUT-PARAM-IF-SNREF SHORT-NAME=""Ver""/>
              </MATCHING-PARAMETER>
            </MATCHING-PARAMETERS>
          </ECU-VARIANT-PATTERN>
        </ECU-VARIANT-PATTERNS>
      </ECU-VARIANT>
    </ECU-VARIANTS>
  </DIAG-LAYER-CONTAINER>
</ODX>";

        var pkg = OdxParser.ParseXml(xml, "InheritanceTest.odx-d");
        var ev = pkg.EcuVariants.FirstOrDefault();
        TestRunner.AssertNotNull(ev, "EV_TEST must be parsed.");
        TestRunner.AssertTrue(ev.Services.Any(s => s.ShortName == "KeepService" && s.IsInherited), "KeepService must be inherited.");
        TestRunner.AssertFalse(ev.Services.Any(s => s.ShortName == "ExcludedService"), "ExcludedService must NOT be inherited due to NOT-INHERITED-DIAG-COMMS.");
        TestRunner.AssertTrue(ev.VariantPatterns.Count == 1 && ev.VariantPatterns[0].Summary.Contains("V99"), "ECU-VARIANT-PATTERN must be parsed.");
        TestRunner.AssertTrue(pkg.ConsistencyIssues.Any(i => i.Message.Contains("MISSING_PROTO_REF")), "Broken PARENT-REF must be flagged in ConsistencyIssues.");
    }

    private static void TestIsoAuthoringRoundTripAndRelinking()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "OdxAuthoringRoundTrip_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var tempPdxPath = Path.Combine(tempRoot, "AuthoredRoundTrip.pdx");

        try
        {
            var proto = new DiagnosticLayer
            {
                LayerType = DiagnosticLayerType.Protocol,
                Id = "PR_UDS_RT",
                ShortName = "PR_UDS_RT",
                ContainerShortName = "Protocols_UDS"
            };
            proto.CommunicationParameters.Add(new CommunicationParameter
            {
                Name = "CP_P2Max",
                Value = "50000",
                Unit = "us",
                Protocol = "UDS",
                Description = "P2 server timing",
                DefinedInLayer = "PR_UDS_RT"
            });

            var bv = new DiagnosticLayer
            {
                LayerType = DiagnosticLayerType.BaseVariant,
                Id = "BV_RT",
                ShortName = "BV_RT",
                ContainerShortName = "BV_RT"
            };
            bv.ParentRefs.Add(new LayerParentRef
            {
                IdRef = "PR_UDS_RT",
                ShortNameRef = "PR_UDS_RT",
                DocRef = "Protocols_UDS",
                ParentRefType = "PROTOCOL-REF"
            });

            var ev = new DiagnosticLayer
            {
                LayerType = DiagnosticLayerType.EcuVariant,
                Id = "EV_RT_V1",
                ShortName = "EV_RT_V1",
                ContainerShortName = "EV_RT_V1"
            };
            ev.ParentRefs.Add(new LayerParentRef
            {
                IdRef = "BV_RT",
                ShortNameRef = "BV_RT",
                DocRef = "BV_RT",
                ParentRefType = "BASE-VARIANT-REF"
            });

            var dop = new DataObjectProperty
            {
                Id = "DOP_OilTemp",
                ShortName = "DOP_OilTemp",
                LongName = "Engine Oil Temperature",
                Category = "LINEAR",
                BaseDataType = "A_UINT32",
                BitLength = 16,
                PhysicalUnit = "degC",
                Factor = 0.1,
                Offset = -40,
                MinPhysicalValue = -40,
                MaxPhysicalValue = 215,
                DefinedInLayer = "EV_RT_V1"
            };
            ev.DataObjectProperties.Add(dop);

            var did = new DiagnosticDataIdentifier
            {
                DidHex = "0x2010",
                DidDecimal = 0x2010,
                ShortName = "OilTemperature",
                LongName = "Engine Oil Temperature",
                DataCategory = "Current Data",
                Access = "Read/Write",
                AssociatedService = "0x22 / 0x2E",
                Description = "Authored engine oil temp DID",
                DefinedInLayer = "EV_RT_V1"
            };
            did.ApplyDop(dop);
            ev.DataIdentifiers.Add(did);

            ev.Services.Add(new DiagnosticService
            {
                Id = "SVC_Read_2010",
                ShortName = "Read_OilTemperature",
                LongName = "Read Oil Temperature",
                ServiceIdHex = "0x22",
                PositiveResponseSidHex = "0x62",
                Semantic = "CURRENT-DATA",
                DataCategory = "Current Data",
                Addressing = "Physical",
                DefinedInLayer = "EV_RT_V1",
                DefinedInLayerType = "EcuVariant",
                RequestParameters =
                {
                    new DiagnosticParameter { Name = "SID", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = "0x22" },
                    new DiagnosticParameter { Name = "DID", ParameterType = "CODED-CONST", BytePosition = 1, BitLength = 16, DefaultValueHex = "0x2010" }
                },
                ResponseParameters =
                {
                    new DiagnosticParameter { Name = "SID_PR", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = "0x62" },
                    new DiagnosticParameter { Name = "DID_PR", ParameterType = "CODED-CONST", BytePosition = 1, BitLength = 16, DefaultValueHex = "0x2010" },
                    new DiagnosticParameter { Name = "OilTemperature", ParameterType = "VALUE", BytePosition = 3, BitLength = 16, DataType = "A_UINT32", DopRef = dop.Id }
                },
                NegativeResponses = { "0x13", "0x22", "0x31" }
            });

            ev.Services.Add(new DiagnosticService
            {
                Id = "SVC_Write_2010",
                ShortName = "Write_OilTemperature",
                LongName = "Write Oil Temperature",
                ServiceIdHex = "0x2E",
                PositiveResponseSidHex = "0x6E",
                Semantic = "CURRENT-DATA",
                DataCategory = "Current Data",
                Addressing = "Physical",
                DefinedInLayer = "EV_RT_V1",
                DefinedInLayerType = "EcuVariant",
                RequestParameters =
                {
                    new DiagnosticParameter { Name = "SID", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = "0x2E" },
                    new DiagnosticParameter { Name = "DID", ParameterType = "CODED-CONST", BytePosition = 1, BitLength = 16, DefaultValueHex = "0x2010" },
                    new DiagnosticParameter { Name = "OilTemperature", ParameterType = "VALUE", BytePosition = 3, BitLength = 16, DataType = "A_UINT32", DopRef = dop.Id }
                },
                ResponseParameters =
                {
                    new DiagnosticParameter { Name = "SID_PR", ParameterType = "CODED-CONST", BytePosition = 0, BitLength = 8, DefaultValueHex = "0x6E" },
                    new DiagnosticParameter { Name = "DID_PR", ParameterType = "CODED-CONST", BytePosition = 1, BitLength = 16, DefaultValueHex = "0x2010" }
                },
                NegativeResponses = { "0x13", "0x31", "0x33" }
            });

            ev.DiagnosticTroubleCodes.Add(new DiagnosticTroubleCode
            {
                Id = "DTC_019800",
                TroubleCode = 0x019800,
                TroubleCodeHex = "0x019800",
                DisplayCode = "P019800",
                Description = "Engine Oil Temperature Sensor High",
                Severity = "Warning",
                FailureTypeByte = "00",
                DefinedInLayer = "EV_RT_V1"
            });

            var pkg = new DiagnosticPackage
            {
                Name = "AuthoredRoundTrip",
                Description = "ISO authoring round-trip verification package",
                Version = "1.0",
                Layers = { proto, bv, ev }
            };

            OdxIsoLinker.LinkPackage(pkg);
            TestRunner.AssertTrue(ev.CommunicationParameters.Any(c => c.Name == "CP_P2Max" && c.IsInherited), "EV_RT_V1 must inherit CP_P2Max through BV_RT -> PR_UDS_RT.");

            OdxXmlSerializer.ExportToPdx(pkg, tempPdxPath);
            TestRunner.AssertTrue(File.Exists(tempPdxPath), "Exported PDX file must exist on disk.");

            var reloaded = PdxExtractor.ExtractAndParse(tempPdxPath);
            TestRunner.AssertNotNull(reloaded.Catalog, "Reloaded PDX must have index.xml ABLOCK catalog.");

            var reloadedEv = reloaded.EcuVariants.FirstOrDefault(l => l.ShortName == "EV_RT_V1");
            TestRunner.AssertNotNull(reloadedEv, "Reloaded PDX must contain ECU variant EV_RT_V1.");
            TestRunner.AssertTrue(reloadedEv.ParentRefs.Any(p => p.IsResolved && p.ResolvedLayerShortName == "BV_RT"), "Reloaded EV_RT_V1 must have resolved PARENT-REF to BV_RT.");

            var reloadedDop = reloadedEv.DataObjectProperties.FirstOrDefault(d => d.ShortName == "DOP_OilTemp");
            TestRunner.AssertNotNull(reloadedDop, "Reloaded EV_RT_V1 must preserve authored DOP_OilTemp.");
            TestRunner.AssertTrue(reloadedDop.PhysicalUnit == "degC" && Math.Abs(reloadedDop.Factor - 0.1) < 1e-6 && Math.Abs(reloadedDop.Offset - (-40)) < 1e-6 && reloadedDop.BitLength == 16,
                "Reloaded DOP_OilTemp must preserve unit, factor, offset, and bit length.");

            var reloadedDid = reloadedEv.DataIdentifiers.FirstOrDefault(d => d.DidHex == "0x2010");
            TestRunner.AssertNotNull(reloadedDid, "Reloaded EV_RT_V1 must preserve authored DID 0x2010.");
            TestRunner.AssertTrue(reloadedDid.DopRef == "DOP_OilTemp" && reloadedDid.Unit == "degC" && Math.Abs(reloadedDid.ScalingFactor - 0.1) < 1e-6 && Math.Abs(reloadedDid.Offset - (-40)) < 1e-6,
                "Reloaded DID 0x2010 must retain DopRef, unit, scaling factor, and offset.");
            TestRunner.AssertTrue(reloadedDid.Access == "Read/Write", "Reloaded DID 0x2010 must preserve Read/Write access derived from 0x22 and 0x2E services.");

            TestRunner.AssertTrue(reloadedEv.Services.Any(s => s.ShortName == "Read_OilTemperature" && s.ServiceIdHex == "0x22" && s.NegativeResponses.Contains("0x22")),
                "Reloaded EV_RT_V1 must preserve 0x22 Read service and NRCs.");
            TestRunner.AssertTrue(reloadedEv.Services.Any(s => s.ShortName == "Write_OilTemperature" && s.ServiceIdHex == "0x2E" && s.NegativeResponses.Contains("0x33")),
                "Reloaded EV_RT_V1 must preserve 0x2E Write service and NRCs.");
            TestRunner.AssertTrue(reloadedEv.DiagnosticTroubleCodes.Any(d => d.DisplayCode == "P019800" || d.TroubleCodeHex == "0x019800"),
                "Reloaded EV_RT_V1 must preserve authored DTC P019800.");
            TestRunner.AssertTrue(reloadedEv.CommunicationParameters.Any(c => c.Name == "CP_P2Max"),
                "Reloaded EV_RT_V1 must preserve inherited CP_P2Max.");

            // Verify OdxIsoLinker.RelinkPackage adds & removes inherited services dynamically when ParentRefs are mutated
            var bvExtra = new DiagnosticLayer
            {
                LayerType = DiagnosticLayerType.BaseVariant,
                Id = "BV_Extra",
                ShortName = "BV_Extra",
                ContainerShortName = "BV_Extra"
            };
            bvExtra.Services.Add(new DiagnosticService
            {
                Id = "SVC_ExtraVersion",
                ShortName = "ReadExtraVersion",
                ServiceIdHex = "0x22",
                PositiveResponseSidHex = "0x62",
                DataCategory = "Stored Data",
                DefinedInLayer = "BV_Extra",
                DefinedInLayerType = "BaseVariant"
            });
            reloaded.Layers.Add(bvExtra);

            var addedRef = new LayerParentRef
            {
                IdRef = "BV_Extra",
                ShortNameRef = "BV_Extra",
                ParentRefType = "BASE-VARIANT-REF"
            };
            reloadedEv.ParentRefs.Add(addedRef);
            OdxIsoLinker.RelinkPackage(reloaded);
            TestRunner.AssertTrue(reloadedEv.Services.Any(s => s.ShortName == "ReadExtraVersion" && s.IsInherited),
                "RelinkPackage must resolve newly added PARENT-REF inheritance.");

            reloadedEv.ParentRefs.Remove(addedRef);
            OdxIsoLinker.RelinkPackage(reloaded);
            TestRunner.AssertFalse(reloadedEv.Services.Any(s => s.ShortName == "ReadExtraVersion"),
                "RelinkPackage must remove inherited service after PARENT-REF is deleted.");
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private static void TryDeleteDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        catch { }
    }
}
