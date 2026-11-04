using System.Diagnostics;
using OdxDiagnosticParser.Exporters;
using OdxDiagnosticParser.Models;
using OdxDiagnosticParser.Parsers;
using OdxDiagnosticParser.Samples;
using OdxDiagnosticParser.Sessions;
using OdxDiagnosticParser.Tester.Tests;

namespace OdxDiagnosticParser.Tester;

internal class Program
{
    // =========================================================================================
    // QUICK TEST CONFIGURATION:
    // To test a specific PDX file directly on F5, set its path below.
    // If left null, the program displays the interactive tester menu.
    // Example: private static readonly string? CustomPdxFilePath = @"C:\Diagnostics\Vehicle_ECM.pdx";
    // =========================================================================================
    private static readonly string? CustomPdxFilePath = null;

    // Core Parser and Exporter APIs
    private static readonly IOdxParser OdxParser = new OdxXmlParser();
    private static readonly IPdxExtractor PdxExtractor = new PdxPackageExtractor(OdxParser);
    private static readonly IOdxExcelExporter ExcelExporter = new OdxExcelExporter();

    static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        PrintBanner();

        // 1. If a custom PDX path is specified above, test it directly
        if (!string.IsNullOrWhiteSpace(CustomPdxFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Config] Using CustomPdxFilePath: {CustomPdxFilePath}\n");
            Console.ResetColor();
            return RunTemporarySession(CustomPdxFilePath);
        }

        // 2. If CLI arguments are provided
        if (args.Length > 0)
        {
            return HandleCommandLineArguments(args);
        }

        // 3. Otherwise, run the interactive tester menu
        return RunInteractiveMenu();
    }

    private static void PrintBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"================================================================================");
        Console.WriteLine(@"     ODX / PDX AUTOMOTIVE DIAGNOSTIC PARSER & EXCEL REPORT GENERATOR            ");
        Console.WriteLine(@"     ISO 22901-1 (ODX) & ISO 22901-2 (PDX) / ASAM MCD-2 D Standard               ");
        Console.WriteLine(@"================================================================================");
        Console.ResetColor();
    }

    private static int RunInteractiveMenu()
    {
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nChoose a testing option:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  1. Run Temporary Diagnostic Session (Extracts data, creates temp Excel, and AUTO-DELETES on exit)");
            Console.ResetColor();
            Console.WriteLine("  2. Test a PDX Package (.pdx) -> Extract Diagnostic Data & Export Permanent Excel");
            Console.WriteLine("  3. Test a single ODX XML file (.odx / .odx-d / .xml) -> Export to Excel");
            Console.WriteLine("  4. Run Complete Automated Test Suite (14 unit tests covering all parser parts)");
            Console.WriteLine("  5. Test Specific Parser Part (Services, DIDs, DTCs, Sessions, PDX Zip-Slip, Excel)");
            Console.WriteLine("  6. Run Built-in End-to-End Demo (Creates sample PDX/ODX, extracts, and exports)");
            Console.WriteLine("  7. Exit");
            Console.Write("\nEnter choice (1-7): ");

            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    PromptAndRunTemporarySession();
                    break;
                case "2":
                    PromptAndTestPdx();
                    break;
                case "3":
                    PromptAndTestOdx();
                    break;
                case "4":
                    var runner = new TestRunner();
                    ParserPartTests.RunAll(runner);
                    break;
                case "5":
                    RunSpecificPartMenu();
                    break;
                case "6":
                    RunFullDemo();
                    break;
                case "7":
                case "exit":
                case "q":
                    return 0;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Invalid selection. Please choose 1-7.");
                    Console.ResetColor();
                    break;
            }
        }
    }

    /// <summary>
    /// Runs a temporary diagnostic session: extracts the PDX into an isolated session directory,
    /// creates a temporary Excel file, keeps the session active for inspection,
    /// and automatically deletes the extracted directory and Excel file when the session ends.
    /// </summary>
    public static int RunTemporarySession(string? pdxPath = null)
    {
        if (string.IsNullOrWhiteSpace(pdxPath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[Temporary Session - PDX Input]");
            Console.WriteLine("  * Drag and drop your .pdx file into this window, or type the path.");
            Console.WriteLine("  * Or press [ENTER] to use a generated realistic Sample PDX file.");
            Console.Write("Enter PDX file path: ");
            Console.ResetColor();

            var input = Console.ReadLine()?.Trim('"', ' ', '\'');
            if (string.IsNullOrWhiteSpace(input))
            {
                var demoDir = Path.Combine(Directory.GetCurrentDirectory(), "OdxDemoOutput");
                Directory.CreateDirectory(demoDir);
                pdxPath = Path.Combine(demoDir, "Sample_Powertrain_ECU.pdx");
                SampleOdxGenerator.CreateSamplePdxFile(pdxPath, "EngineControlUnit");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  -> Using sample PDX: {pdxPath}");
                Console.ResetColor();
            }
            else
            {
                pdxPath = input;
            }
        }

        if (!File.Exists(pdxPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR] File not found: '{pdxPath}'");
            Console.ResetColor();
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"  STARTING TEMPORARY DIAGNOSTIC SESSION FOR: {Path.GetFileName(pdxPath)}");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // Start session: creates temp folder, extracts PDX, parses data, generates temp Excel
        using (var session = DiagnosticSession.CreateFromPdx(pdxPath))
        {
            Console.WriteLine($"[1/3] Created isolated session folder: {session.SessionDirectory}");
            Console.WriteLine($"      Extracted files located in:     {session.ExtractedDirectory}");
            Console.WriteLine($"      Temporary Excel file created:   {session.TemporaryExcelFilePath}");

            Console.WriteLine($"\n[2/3] Diagnostic Data Extracted Successfully:");
            DisplayDetailedDiagnosticReport(session.Package);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\nWould you like to open the temporary Excel file now? (Y/n): ");
            Console.ResetColor();
            var openChoice = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (openChoice == "" || openChoice == "y" || openChoice == "yes")
            {
                Console.WriteLine("Launching Excel viewer...");
                session.OpenExcelInDefaultViewer();
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n================================================================================");
            Console.WriteLine("  SESSION IS ACTIVE - DATA IS TEMPORARY");
            Console.WriteLine($"  * Extracted ODX Folder: {session.ExtractedDirectory}");
            Console.WriteLine($"  * Temporary Excel File: {session.TemporaryExcelFilePath}");
            Console.WriteLine();
            Console.WriteLine("  Commands:");
            Console.WriteLine("    - Press [ENTER] to end session and permanently DELETE all temporary files.");
            Console.WriteLine("    - Type 'save <path>' to save a permanent copy of the Excel workbook.");
            Console.WriteLine("    - Type 'open' to open the Excel file again.");
            Console.WriteLine("================================================================================");
            Console.ResetColor();

            while (true)
            {
                Console.Write("\nSession command ([ENTER] to delete & exit): ");
                var cmd = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(cmd))
                {
                    break; // End session and trigger Dispose()
                }

                if (cmd.StartsWith("save ", StringComparison.OrdinalIgnoreCase))
                {
                    var dest = cmd[5..].Trim('"', ' ');
                    if (!string.IsNullOrEmpty(dest))
                    {
                        try
                        {
                            session.SavePermanentCopy(dest);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[Saved] Permanent copy saved to: {dest}");
                            Console.ResetColor();
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[Error saving copy] {ex.Message}");
                            Console.ResetColor();
                        }
                    }
                }
                else if (cmd.Equals("open", StringComparison.OrdinalIgnoreCase))
                {
                    session.OpenExcelInDefaultViewer();
                }
                else if (cmd.Equals("exit", StringComparison.OrdinalIgnoreCase) || cmd.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Unknown command. Press [ENTER] to delete temporary files and exit session.");
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[Closing Session] Cleaning up and deleting temporary files...");
            Console.ResetColor();
        } // session.Dispose() called here

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[SUCCESS] Temporary session folder, extracted ODX files, and temporary Excel workbook have been completely deleted.");
        Console.ResetColor();

        return 0;
    }

    private static void PromptAndRunTemporarySession()
    {
        RunTemporarySession(null);
    }

    /// <summary>
    /// Prompts the user for a PDX file path (or drag-and-drop), or runs with a generated sample if left blank.
    /// </summary>
    private static void PromptAndTestPdx()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[PDX Input - Permanent Export]");
        Console.WriteLine("  * Drag and drop your .pdx file into this window, or type the path.");
        Console.WriteLine("  * Or press [ENTER] to automatically generate and test a realistic Sample PDX file.");
        Console.Write("Enter PDX file path: ");
        Console.ResetColor();

        var input = Console.ReadLine()?.Trim('"', ' ', '\'');

        string pdxPath;
        if (string.IsNullOrWhiteSpace(input))
        {
            var demoDir = Path.Combine(Directory.GetCurrentDirectory(), "OdxDemoOutput");
            Directory.CreateDirectory(demoDir);
            pdxPath = Path.Combine(demoDir, "Sample_Powertrain_ECU.pdx");
            SampleOdxGenerator.CreateSamplePdxFile(pdxPath, "EngineControlUnit");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  -> Generated sample PDX at: {pdxPath}");
            Console.ResetColor();
        }
        else
        {
            pdxPath = input;
        }

        TestAndExportPdx(pdxPath, null);
    }

    /// <summary>
    /// Prompts the user for a single ODX XML file path (or drag-and-drop), or uses sample if left blank.
    /// </summary>
    private static void PromptAndTestOdx()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n[ODX XML Input]");
        Console.WriteLine("  * Drag and drop your .odx / .odx-d file here, or type the path.");
        Console.WriteLine("  * Or press [ENTER] to use a generated sample ODX-D file.");
        Console.Write("Enter ODX file path: ");
        Console.ResetColor();

        var input = Console.ReadLine()?.Trim('"', ' ', '\'');

        string odxPath;
        if (string.IsNullOrWhiteSpace(input))
        {
            var demoDir = Path.Combine(Directory.GetCurrentDirectory(), "OdxDemoOutput");
            Directory.CreateDirectory(demoDir);
            odxPath = Path.Combine(demoDir, "Sample_EngineControlUnit.odx-d");
            File.WriteAllText(odxPath, SampleOdxGenerator.CreateSampleOdxXml("EngineControlUnit"));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  -> Generated sample ODX-D at: {odxPath}");
            Console.ResetColor();
        }
        else
        {
            odxPath = input;
        }

        var outExcel = Path.ChangeExtension(odxPath, ".xlsx");
        ConvertOdxToExcel(odxPath, outExcel);
    }

    /// <summary>
    /// Core testing function: Unpacks PDX, parses all diagnostic data, prints full console report, and exports to Excel.
    /// </summary>
    public static int TestAndExportPdx(string pdxFilePath, string? outputExcelPath = null)
    {
        if (!File.Exists(pdxFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ERROR] File not found: '{pdxFilePath}'");
            Console.ResetColor();
            return 1;
        }

        outputExcelPath ??= Path.ChangeExtension(pdxFilePath, ".xlsx");

        var sw = Stopwatch.StartNew();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"  TESTING PDX PACKAGE: {Path.GetFileName(pdxFilePath)}");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        // Step 1: Unzip & Parse Catalog
        Console.WriteLine($"[1/3] Extracting PDX archive & parsing catalog manifest (index.xml)...");
        var package = PdxExtractor.ExtractAndParse(pdxFilePath);

        // Step 2: Display Detailed Extracted Diagnostic Data
        Console.WriteLine($"[2/3] Diagnostic Data Extracted Successfully:");
        DisplayDetailedDiagnosticReport(package);

        // Step 3: Export to Excel
        Console.WriteLine($"[3/3] Generating multi-sheet Excel workbook...");
        ExcelExporter.ExportToExcel(package, outputExcelPath);
        sw.Stop();

        var fileInfo = new FileInfo(outputExcelPath);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"  SUCCESS! Excel Report Generated in {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"  File: {outputExcelPath} ({fileInfo.Length:N0} bytes)");
        Console.WriteLine("================================================================================");
        Console.ResetColor();

        return 0;
    }

    private static void DisplayDetailedDiagnosticReport(DiagnosticPackage package)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n--- PACKAGE SUMMARY ---");
        Console.ResetColor();
        Console.WriteLine($"  Package Name:       {package.Name}");
        Console.WriteLine($"  Source File:        {Path.GetFileName(package.SourcePath)}");
        Console.WriteLine($"  ODX Model Version:  {package.Version}");
        Console.WriteLine($"  Extracted UTC:      {package.ExtractedAt:yyyy-MM-dd HH:mm:ss}");

        if (package.Catalog != null)
        {
            if (!string.IsNullOrEmpty(package.Catalog.Creator))
                Console.WriteLine($"  Catalog Creator:    {package.Catalog.Creator}");
            if (package.Catalog.Vehicles.Count > 0)
                Console.WriteLine($"  Vehicle Models:     {string.Join(", ", package.Catalog.Vehicles.Select(v => $"{v.Model} ({v.Company})"))}");
            if (package.Catalog.Ecus.Count > 0)
                Console.WriteLine($"  ECU Info:           {string.Join(", ", package.Catalog.Ecus.Select(e => $"{e.EcuName} [DiagAddr: {e.DiagnosticAddress}]"))}");
            if (package.Catalog.Files.Count > 0)
            {
                Console.WriteLine($"  Packaged Files ({package.Catalog.Files.Count}):");
                foreach (var f in package.Catalog.Files)
                {
                    Console.WriteLine($"    - [{f.FileType}] {f.ShortName} ({f.RelativePath}, {f.FileSizeBytes} bytes)");
                }
            }
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n--- DIAGNOSTIC METRICS ---");
        Console.ResetColor();
        Console.WriteLine($"  Diagnostic Layers:  {package.Layers.Count}");
        Console.WriteLine($"  Total Services:     {package.TotalServicesCount}");
        Console.WriteLine($"  Total DIDs:         {package.TotalDidsCount}");
        Console.WriteLine($"  Total DTCs:         {package.TotalDtcsCount}");
        Console.WriteLine($"  Total ComParams:    {package.TotalComParamsCount}");

        foreach (var layer in package.Layers)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n>>> LAYER: {layer.ShortName} ({layer.LayerType}) <<<");
            Console.ResetColor();

            // Services
            if (layer.Services.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"  Diagnostic Services ({layer.Services.Count}):");
                Console.ResetColor();
                foreach (var s in layer.Services)
                {
                    var subFunc = !string.IsNullOrEmpty(s.SubFunctionHex) ? $" [SubFunc: {s.SubFunctionHex}]" : "";
                    var posSid = !string.IsNullOrEmpty(s.PositiveResponseSidHex) ? $" -> PosResp: {s.PositiveResponseSidHex}" : "";
                    Console.WriteLine($"    * [{s.ServiceIdHex}] {s.ShortName,-32} Semantic: {s.Semantic,-14}{subFunc}{posSid}");
                }
            }

            // DIDs
            if (layer.DataIdentifiers.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n  Data Identifiers / DIDs ({layer.DataIdentifiers.Count}):");
                Console.ResetColor();
                foreach (var did in layer.DataIdentifiers)
                {
                    var unitStr = !string.IsNullOrEmpty(did.Unit) ? $" [{did.Unit}]" : "";
                    var scalingStr = did.ScalingFactor != 1.0 || did.Offset != 0.0 ? $" (Scaling: x{did.ScalingFactor} + {did.Offset})" : "";
                    var accessColor = did.Access.Contains("Write") ? ConsoleColor.Magenta : ConsoleColor.Green;

                    Console.Write($"    * [{did.DidHex}] {did.ShortName,-30} Access: ");
                    Console.ForegroundColor = accessColor;
                    Console.Write($"{did.Access,-10}");
                    Console.ResetColor();
                    Console.WriteLine($" Len: {did.ByteLength}B, Type: {did.DataType}{unitStr}{scalingStr}");
                }
            }

            // DTCs
            if (layer.DiagnosticTroubleCodes.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n  Diagnostic Trouble Codes / DTCs ({layer.DiagnosticTroubleCodes.Count}):");
                Console.ResetColor();
                foreach (var dtc in layer.DiagnosticTroubleCodes)
                {
                    var sevColor = dtc.Severity.Equals("Fatal", StringComparison.OrdinalIgnoreCase) ? ConsoleColor.Red : ConsoleColor.DarkYellow;
                    Console.Write($"    * [{dtc.DisplayCode,-10}] Hex: {dtc.TroubleCodeHex,-10} FTB: {dtc.FailureTypeByte,-6} Severity: ");
                    Console.ForegroundColor = sevColor;
                    Console.Write($"{dtc.Severity,-8}");
                    Console.ResetColor();
                    Console.WriteLine($" {dtc.Description}");
                }
            }

            // ComParams
            if (layer.CommunicationParameters.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"\n  Communication Parameters ({layer.CommunicationParameters.Count}):");
                Console.ResetColor();
                foreach (var cp in layer.CommunicationParameters)
                {
                    var unit = !string.IsNullOrEmpty(cp.Unit) ? $" {cp.Unit}" : "";
                    Console.WriteLine($"    * {cp.Name,-28} = {cp.Value}{unit}");
                }
            }
        }
    }

    private static int ConvertOdxToExcel(string odxFilePath, string outputExcelPath)
    {
        if (!File.Exists(odxFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: ODX file not found at '{odxFilePath}'");
            Console.ResetColor();
            return 1;
        }

        Console.WriteLine($"\nParsing ODX XML file: {odxFilePath}...");
        var sw = Stopwatch.StartNew();
        var package = OdxParser.ParseFile(odxFilePath);

        DisplayDetailedDiagnosticReport(package);

        Console.WriteLine($"\nWriting Excel workbook: {outputExcelPath}...");
        ExcelExporter.ExportToExcel(package, outputExcelPath);
        sw.Stop();

        var fileInfo = new FileInfo(outputExcelPath);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SUCCESS] Excel file generated: {outputExcelPath} ({fileInfo.Length:N0} bytes) in {sw.ElapsedMilliseconds} ms");
        Console.ResetColor();
        return 0;
    }

    private static void RunSpecificPartMenu()
    {
        Console.WriteLine("\n--- Select Parser Part to Test ---");
        Console.WriteLine("  a. Test Diagnostic Layers & XML Structure");
        Console.WriteLine("  b. Test UDS Diagnostic Services (0x10, 0x22, 0x2E, 0x14, 0x19, 0x27)");
        Console.WriteLine("  c. Test DIDs, Scaling Factors, Physical Units & Access Rights");
        Console.WriteLine("  d. Test DTCs, UDS Display Codes & Failure Type Bytes");
        Console.WriteLine("  e. Test PDX Safe Unzipping & Security (Zip-Slip defense)");
        Console.WriteLine("  f. Test PDX Catalog Manifest (index.xml)");
        Console.WriteLine("  g. Test Excel Workbook Generation & Sheets");
        Console.WriteLine("  h. Test Temporary Diagnostic Session Lifecycle & Auto-Cleanup");
        Console.Write("\nChoice (a-h): ");

        var subChoice = Console.ReadLine()?.Trim().ToLowerInvariant();
        var runner = new TestRunner();

        switch (subChoice)
        {
            case "a":
                runner.RunTest("Diagnostic Layers Extraction", ParserPartTests.TestOdxDiagnosticLayers);
                break;
            case "b":
                runner.RunTest("UDS Diagnostic Services", ParserPartTests.TestUdsServicesParsing);
                break;
            case "c":
                runner.RunTest("DIDs, Scaling & Units", ParserPartTests.TestDidsAndScaling);
                break;
            case "d":
                runner.RunTest("DTCs & Failure Type Bytes", ParserPartTests.TestDtcsAndFailureTypeBytes);
                break;
            case "e":
                runner.RunTest("PDX Safe Extraction", ParserPartTests.TestPdxSafeExtraction);
                runner.RunTest("PDX Zip-Slip Defense", ParserPartTests.TestPdxZipSlipSecurityDefense);
                break;
            case "f":
                runner.RunTest("PDX Catalog Parsing", ParserPartTests.TestPdxCatalogParsing);
                break;
            case "g":
                runner.RunTest("Excel Workbook Generation", ParserPartTests.TestExcelWorkbookGeneration);
                break;
            case "h":
                runner.RunTest("Temporary Session Lifecycle & Auto-Cleanup", ParserPartTests.TestDiagnosticSessionLifecycle);
                runner.RunTest("Session Permanent Copy Saving", ParserPartTests.TestDiagnosticSessionSavePermanentCopy);
                break;
            default:
                Console.WriteLine("Invalid choice.");
                return;
        }

        runner.PrintSummary();
    }

    private static int HandleCommandLineArguments(string[] args)
    {
        var command = args[0].ToLowerInvariant();
        switch (command)
        {
            case "test":
            case "--test":
            case "-t":
                var runner = new TestRunner();
                ParserPartTests.RunAll(runner);
                return runner.TotalFailed == 0 ? 0 : 1;

            case "demo":
            case "--demo":
            case "-d":
                return RunFullDemo();

            case "session":
            case "--session":
            case "temp":
                var sessionPath = args.Length >= 2 ? args[1] : null;
                return RunTemporarySession(sessionPath);

            case "pdx":
            case "pdx-to-excel":
                if (args.Length < 2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Usage: dotnet run -- pdx <path-to-pdx-file> [output-excel-path]");
                    Console.ResetColor();
                    return 1;
                }
                var pdxIn = args[1];
                var pdxExcelOut = args.Length >= 3 ? args[2] : Path.ChangeExtension(pdxIn, ".xlsx");
                return TestAndExportPdx(pdxIn, pdxExcelOut);

            case "parse-odx":
            case "odx-to-excel":
                if (args.Length < 2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Usage: dotnet run -- parse-odx <path-to-odx-file> [output-excel-path]");
                    Console.ResetColor();
                    return 1;
                }
                var odxFile = args[1];
                var excelOut = args.Length >= 3 ? args[2] : Path.ChangeExtension(odxFile, ".xlsx");
                return ConvertOdxToExcel(odxFile, excelOut);

            case "unpack-pdx":
                if (args.Length < 2)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Usage: dotnet run -- unpack-pdx <path-to-pdx-file> [target-directory]");
                    Console.ResetColor();
                    return 1;
                }
                var pdxArchive = args[1];
                var destDir = args.Length >= 3 ? args[2] : null;
                var extractedDir = PdxExtractor.UnzipPdx(pdxArchive, destDir);
                Console.WriteLine($"Extracted to: {extractedDir}");
                return 0;

            case "--help":
            case "-h":
            case "help":
                PrintHelp();
                return 0;

            default:
                // If user passed a file path directly as first argument (e.g. dotnet run -- MyPackage.pdx)
                if (File.Exists(args[0]))
                {
                    var ext = Path.GetExtension(args[0]).ToLowerInvariant();
                    if (ext == ".pdx" || ext == ".zip")
                    {
                        var outPath = args.Length >= 2 ? args[1] : Path.ChangeExtension(args[0], ".xlsx");
                        return TestAndExportPdx(args[0], outPath);
                    }
                    else
                    {
                        var outPath = args.Length >= 2 ? args[1] : Path.ChangeExtension(args[0], ".xlsx");
                        return ConvertOdxToExcel(args[0], outPath);
                    }
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unknown command: {command}");
                Console.ResetColor();
                PrintHelp();
                return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
Available CLI Commands:
  session [pdx-path]                   Runs a temporary session (extracts, creates temp Excel, auto-deletes on close)
  test                                 Runs complete automated test suite testing all parser parts
  demo                                 Runs end-to-end sample test (ODX/PDX creation, parsing, Excel export)
  pdx <pdx-path> [excel-path]          Extracts PDX, resolves all diagnostic layers, and creates permanent Excel
  parse-odx <odx-path> [excel-path]    Parses single ODX XML file and exports to formatted Excel
  unpack-pdx <pdx-path> [dest-folder]  Extracts PDX archive safely and inspects catalog manifest

Direct File Invocation:
  dotnet run -- session MyPackage.pdx  Runs temporary session with auto-cleanup on close
  dotnet run -- MyPackage.pdx          Directly parses MyPackage.pdx and creates MyPackage.xlsx
  dotnet run -- Engine.odx-d           Directly parses Engine.odx-d and creates Engine.xlsx
");
    }

    public static int RunFullDemo()
    {
        var demoDir = Path.Combine(Directory.GetCurrentDirectory(), "OdxDemoOutput");
        Directory.CreateDirectory(demoDir);

        var samplePdxPath = Path.Combine(demoDir, "Powertrain_EMS_2026.pdx");
        var outputExcelFromPdx = Path.Combine(demoDir, "Powertrain_EMS_Diagnostics.xlsx");

        SampleOdxGenerator.CreateSamplePdxFile(samplePdxPath, "EngineControlUnit");
        return TestAndExportPdx(samplePdxPath, outputExcelFromPdx);
    }
}
