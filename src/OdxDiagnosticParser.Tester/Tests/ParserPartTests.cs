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
