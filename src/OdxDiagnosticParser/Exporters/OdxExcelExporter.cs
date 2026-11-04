using ClosedXML.Excel;
using OdxDiagnosticParser.Models;

namespace OdxDiagnosticParser.Exporters;

/// <summary>
/// Exports automotive diagnostic data parsed from ODX/PDX into beautifully formatted, multi-worksheet Excel (.xlsx) files.
/// </summary>
public class OdxExcelExporter : IOdxExcelExporter
{
    public void ExportToExcel(DiagnosticPackage package, string outputFilePath, ExcelExportOptions? options = null)
    {
        if (package == null)
            throw new ArgumentNullException(nameof(package));

        if (string.IsNullOrWhiteSpace(outputFilePath))
            throw new ArgumentException("Output file path cannot be empty.", nameof(outputFilePath));

        var dir = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        options ??= new ExcelExportOptions();

        using var workbook = CreateWorkbook(package, options);
        workbook.SaveAs(outputFilePath);
    }

    public byte[] ExportToBytes(DiagnosticPackage package, ExcelExportOptions? options = null)
    {
        if (package == null)
            throw new ArgumentNullException(nameof(package));

        options ??= new ExcelExportOptions();

        using var workbook = CreateWorkbook(package, options);
        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    private XLWorkbook CreateWorkbook(DiagnosticPackage package, ExcelExportOptions options)
    {
        var workbook = new XLWorkbook();

        if (options.IncludeOverviewSheet)
        {
            BuildOverviewSheet(workbook, package, options);
        }

        if (options.IncludeServicesSheet)
        {
            BuildServicesSheet(workbook, package, options);
        }

        if (options.IncludeDidsSheet)
        {
            BuildDidsSheet(workbook, package, options);
        }

        if (options.IncludeDtcsSheet)
        {
            BuildDtcsSheet(workbook, package, options);
        }

        if (options.IncludeComParamsSheet)
        {
            BuildComParamsSheet(workbook, package, options);
        }

        if (options.IncludePdxManifestSheet && package.Catalog != null)
        {
            BuildPdxManifestSheet(workbook, package, options);
        }

        return workbook;
    }

    private void BuildOverviewSheet(XLWorkbook workbook, DiagnosticPackage package, ExcelExportOptions options)
    {
        var ws = workbook.Worksheets.Add("Overview");
        ws.ShowGridLines = true;

        var headerColor = XLColor.FromHtml(options.HeaderBackgroundColorHex);

        // Title Block
        ws.Cell("A1").Value = "AUTOMOTIVE DIAGNOSTIC DATA SPECIFICATION";
        ws.Range("A1:G1").Merge();
        ws.Range("A1:G1").Style.Font.Bold = true;
        ws.Range("A1:G1").Style.Font.FontSize = 14;
        ws.Range("A1:G1").Style.Font.FontColor = XLColor.White;
        ws.Range("A1:G1").Style.Fill.BackgroundColor = headerColor;
        ws.Range("A1:G1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 28;

        // Package Metadata Table
        var metaHeaders = new[] { "Property", "Value" };
        var metaRows = new List<(string Prop, string Val)>
        {
            ("Package / ECU Name", package.Name),
            ("Source Type", package.IsFromPdx ? "Packaged Diagnostic Exchange (PDX Archive)" : "Single ODX Diagnostic File"),
            ("Source File", Path.GetFileName(package.SourcePath)),
            ("ODX Model Version", package.Version),
            ("Extracted At (UTC)", package.ExtractedAt.ToString("yyyy-MM-dd HH:mm:ss")),
            ("Package Description", string.IsNullOrWhiteSpace(package.Description) ? "N/A" : package.Description)
        };

        if (package.Catalog != null)
        {
            if (!string.IsNullOrEmpty(package.Catalog.Creator))
                metaRows.Add(("Catalog Creator", package.Catalog.Creator));
            if (!string.IsNullOrEmpty(package.Catalog.CreationDate))
                metaRows.Add(("Creation Date", package.Catalog.CreationDate));
        }

        int curRow = 3;
        ws.Cell(curRow, 1).Value = "METADATA & PACKAGE INFORMATION";
        ws.Range(curRow, 1, curRow, 2).Merge();
        ws.Range(curRow, 1, curRow, 2).Style.Font.Bold = true;
        ws.Range(curRow, 1, curRow, 2).Style.Font.FontColor = XLColor.White;
        ws.Range(curRow, 1, curRow, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");

        curRow++;
        foreach (var (prop, val) in metaRows)
        {
            ws.Cell(curRow, 1).Value = prop;
            ws.Cell(curRow, 1).Style.Font.Bold = true;
            ws.Cell(curRow, 2).Value = val;
            ws.Range(curRow, 1, curRow, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(curRow, 1, curRow, 2).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            curRow++;
        }

        // Summary Statistics Box
        int statRowStart = 3;
        ws.Cell(statRowStart, 4).Value = "METRIC";
        ws.Cell(statRowStart, 5).Value = "COUNT";
        ws.Range(statRowStart, 4, statRowStart, 5).Style.Font.Bold = true;
        ws.Range(statRowStart, 4, statRowStart, 5).Style.Font.FontColor = XLColor.White;
        ws.Range(statRowStart, 4, statRowStart, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");

        var metrics = new List<(string Metric, int Count)>
        {
            ("Total Diagnostic Layers", package.Layers.Count),
            ("Total ECU Variants", package.EcuVariants.Count()),
            ("Total Base Variants", package.BaseVariants.Count()),
            ("Total Diagnostic Services (UDS/KWP)", package.TotalServicesCount),
            ("Total Data Identifiers (DIDs)", package.TotalDidsCount),
            ("Total Diagnostic Trouble Codes (DTCs)", package.TotalDtcsCount),
            ("Total Communication Parameters", package.TotalComParamsCount)
        };

        int r = statRowStart + 1;
        foreach (var (metric, count) in metrics)
        {
            ws.Cell(r, 4).Value = metric;
            ws.Cell(r, 4).Style.Font.Bold = true;
            ws.Cell(r, 5).Value = count;
            ws.Cell(r, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Range(r, 4, r, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(r, 4, r, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            r++;
        }

        curRow = Math.Max(curRow, r) + 2;

        // Diagnostic Layers Table
        ws.Cell(curRow, 1).Value = "DIAGNOSTIC LAYERS & ECU BREAKDOWN";
        ws.Range(curRow, 1, curRow, 7).Merge();
        ws.Range(curRow, 1, curRow, 7).Style.Font.Bold = true;
        ws.Range(curRow, 1, curRow, 7).Style.Font.FontColor = XLColor.White;
        ws.Range(curRow, 1, curRow, 7).Style.Fill.BackgroundColor = headerColor;
        curRow++;

        var layerHeaders = new[] { "Layer Type", "Short Name", "Long Name", "Source File", "Services", "DIDs", "DTCs" };
        for (int i = 0; i < layerHeaders.Length; i++)
        {
            var cell = ws.Cell(curRow, i + 1);
            cell.Value = layerHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#34495E");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
        curRow++;

        int startLayerRow = curRow;
        foreach (var layer in package.Layers)
        {
            ws.Cell(curRow, 1).Value = layer.LayerType.ToString();
            ws.Cell(curRow, 2).Value = layer.ShortName;
            ws.Cell(curRow, 3).Value = layer.LongName;
            ws.Cell(curRow, 4).Value = layer.SourceFileName;
            ws.Cell(curRow, 5).Value = layer.Services.Count;
            ws.Cell(curRow, 6).Value = layer.DataIdentifiers.Count;
            ws.Cell(curRow, 7).Value = layer.DiagnosticTroubleCodes.Count;

            if ((curRow - startLayerRow) % 2 == 1)
            {
                ws.Range(curRow, 1, curRow, 7).Style.Fill.BackgroundColor = XLColor.FromHtml(options.AlternateRowColorHex);
            }

            ws.Range(curRow, 1, curRow, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(curRow, 1, curRow, 7).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            curRow++;
        }

        ws.Columns(1, 7).AdjustToContents(1, options.MaxColumnWidth);
    }

    private void BuildServicesSheet(XLWorkbook workbook, DiagnosticPackage package, ExcelExportOptions options)
    {
        var ws = workbook.Worksheets.Add("Diagnostic Services");
        ws.ShowGridLines = true;

        var headers = new[]
        {
            "ECU / Layer",
            "Service ID (Hex)",
            "Service Name",
            "Semantic",
            "Sub-Function",
            "Pos Resp SID",
            "Request Parameters Count",
            "Response Parameters Count",
            "Supported NRCs",
            "Description"
        };

        FormatHeaderRow(ws, headers, options);

        int row = 2;
        foreach (var layer in package.Layers)
        {
            foreach (var svc in layer.Services)
            {
                ws.Cell(row, 1).Value = layer.ShortName;
                ws.Cell(row, 2).Value = svc.ServiceIdHex;
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 2).Style.Font.Bold = true;

                ws.Cell(row, 3).Value = svc.ShortName;
                ws.Cell(row, 4).Value = svc.Semantic;
                ws.Cell(row, 5).Value = svc.SubFunctionHex;
                ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(row, 6).Value = svc.PositiveResponseSidHex;
                ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(row, 7).Value = svc.RequestParameters.Count;
                ws.Cell(row, 8).Value = svc.ResponseParameters.Count;
                ws.Cell(row, 9).Value = svc.NegativeResponses.Count > 0 ? string.Join(", ", svc.NegativeResponses) : "Standard UDS";
                ws.Cell(row, 10).Value = svc.Description;

                if (row % 2 == 1)
                {
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml(options.AlternateRowColorHex);
                }

                ws.Range(row, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;
            }
        }

        FinalizeWorksheet(ws, headers.Length, options);
    }

    private void BuildDidsSheet(XLWorkbook workbook, DiagnosticPackage package, ExcelExportOptions options)
    {
        var ws = workbook.Worksheets.Add("Data Identifiers (DIDs)");
        ws.ShowGridLines = true;

        var headers = new[]
        {
            "ECU / Layer",
            "DID (Hex)",
            "DID (Dec)",
            "DID Name",
            "Access",
            "Length (Bytes)",
            "Bit Length",
            "Data Type",
            "Unit",
            "Scaling Factor",
            "Scaling Offset",
            "Min Physical",
            "Max Physical",
            "Associated Service",
            "Description"
        };

        FormatHeaderRow(ws, headers, options);

        int row = 2;
        foreach (var layer in package.Layers)
        {
            foreach (var did in layer.DataIdentifiers)
            {
                ws.Cell(row, 1).Value = layer.ShortName;
                ws.Cell(row, 2).Value = did.DidHex;
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 2).Style.Font.Bold = true;

                ws.Cell(row, 3).Value = did.DidDecimal;
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(row, 4).Value = did.ShortName;
                ws.Cell(row, 5).Value = did.Access;
                ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Color code access: Read = Green tint, Write/Read-Write = Blue/Amber tint
                if (did.Access.Contains("Write", StringComparison.OrdinalIgnoreCase))
                    ws.Cell(row, 5).Style.Font.FontColor = XLColor.FromHtml("#C0392B");
                else
                    ws.Cell(row, 5).Style.Font.FontColor = XLColor.FromHtml("#27AE60");

                ws.Cell(row, 6).Value = did.ByteLength;
                ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(row, 7).Value = did.BitLength;
                ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(row, 8).Value = did.DataType;
                ws.Cell(row, 9).Value = did.Unit;
                ws.Cell(row, 10).Value = did.ScalingFactor;
                ws.Cell(row, 11).Value = did.Offset;
                ws.Cell(row, 12).Value = did.MinPhysicalValue.HasValue ? did.MinPhysicalValue.Value : "";
                ws.Cell(row, 13).Value = did.MaxPhysicalValue.HasValue ? did.MaxPhysicalValue.Value : "";
                ws.Cell(row, 14).Value = did.AssociatedService;
                ws.Cell(row, 15).Value = did.Description;

                if (row % 2 == 1)
                {
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml(options.AlternateRowColorHex);
                }

                ws.Range(row, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;
            }
        }

        FinalizeWorksheet(ws, headers.Length, options);
    }

    private void BuildDtcsSheet(XLWorkbook workbook, DiagnosticPackage package, ExcelExportOptions options)
    {
        var ws = workbook.Worksheets.Add("DTCs (Trouble Codes)");
        ws.ShowGridLines = true;

        var headers = new[]
        {
            "ECU / Layer",
            "Display Code (UDS)",
            "Trouble Code (Hex)",
            "Trouble Code (Dec)",
            "Failure Type (FTB)",
            "Severity",
            "Description"
        };

        FormatHeaderRow(ws, headers, options);

        int row = 2;
        foreach (var layer in package.Layers)
        {
            foreach (var dtc in layer.DiagnosticTroubleCodes)
            {
                ws.Cell(row, 1).Value = layer.ShortName;

                ws.Cell(row, 2).Value = dtc.DisplayCode;
                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(row, 3).Value = dtc.TroubleCodeHex;
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(row, 4).Value = dtc.TroubleCode;
                ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                ws.Cell(row, 5).Value = dtc.FailureTypeByte;
                ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(row, 6).Value = dtc.Severity;
                ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(row, 7).Value = dtc.Description;

                if (row % 2 == 1)
                {
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml(options.AlternateRowColorHex);
                }

                ws.Range(row, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;
            }
        }

        FinalizeWorksheet(ws, headers.Length, options);
    }

    private void BuildComParamsSheet(XLWorkbook workbook, DiagnosticPackage package, ExcelExportOptions options)
    {
        var ws = workbook.Worksheets.Add("Communication Parameters");
        ws.ShowGridLines = true;

        var headers = new[]
        {
            "ECU / Layer",
            "Parameter Name",
            "Value",
            "Unit",
            "Protocol",
            "Description"
        };

        FormatHeaderRow(ws, headers, options);

        int row = 2;
        foreach (var layer in package.Layers)
        {
            foreach (var cp in layer.CommunicationParameters)
            {
                ws.Cell(row, 1).Value = layer.ShortName;
                ws.Cell(row, 2).Value = cp.Name;
                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Cell(row, 3).Value = cp.Value;
                ws.Cell(row, 4).Value = cp.Unit;
                ws.Cell(row, 5).Value = cp.Protocol;
                ws.Cell(row, 6).Value = cp.Description;

                if (row % 2 == 1)
                {
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml(options.AlternateRowColorHex);
                }

                ws.Range(row, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;
            }
        }

        FinalizeWorksheet(ws, headers.Length, options);
    }

    private void BuildPdxManifestSheet(XLWorkbook workbook, DiagnosticPackage package, ExcelExportOptions options)
    {
        var ws = workbook.Worksheets.Add("PDX Manifest");
        ws.ShowGridLines = true;

        var headers = new[]
        {
            "File Name",
            "File Type",
            "Relative Path",
            "Size (Bytes)",
            "Checksum (MD5)"
        };

        FormatHeaderRow(ws, headers, options);

        int row = 2;
        if (package.Catalog?.Files != null)
        {
            foreach (var file in package.Catalog.Files)
            {
                ws.Cell(row, 1).Value = file.ShortName;
                ws.Cell(row, 2).Value = file.FileType;
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 3).Value = file.RelativePath;
                ws.Cell(row, 4).Value = file.FileSizeBytes;
                ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                ws.Cell(row, 5).Value = file.Checksum;

                if (row % 2 == 1)
                {
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml(options.AlternateRowColorHex);
                }

                ws.Range(row, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;
            }
        }

        FinalizeWorksheet(ws, headers.Length, options);
    }

    private void FormatHeaderRow(IXLWorksheet ws, string[] headers, ExcelExportOptions options)
    {
        var headerColor = XLColor.FromHtml(options.HeaderBackgroundColorHex);
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = headerColor;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        ws.Row(1).Height = 24;
    }

    private void FinalizeWorksheet(IXLWorksheet ws, int columnCount, ExcelExportOptions options)
    {
        // Freeze header row
        ws.SheetView.FreezeRows(1);

        // Auto-filter on all data
        var lastRow = ws.LastRowUsed();
        if (lastRow != null && lastRow.RowNumber() > 1)
        {
            ws.Range(1, 1, lastRow.RowNumber(), columnCount).SetAutoFilter();
        }

        // Auto-adjust column widths with maximum cap
        ws.Columns(1, columnCount).AdjustToContents(1, options.MaxColumnWidth);
    }
}
