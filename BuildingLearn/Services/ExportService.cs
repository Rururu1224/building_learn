using BuildingLearn.Data;
using BuildingLearn.Data.Models;
using BuildingLearn.Services;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using Serilog;

namespace BuildingLearn.Services;

/// <summary>
/// 导出服务 — CSV / Excel / PDF
/// </summary>
public class ExportService
{
    private readonly ConfigService _config;
    private readonly DbHelper _db;

    public ExportService(ConfigService config, DbHelper db)
    {
        _config = config;
        _db = db;
        // 设置 EPPlus 许可证上下文
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// 生成 Excel 报告
    /// Sheet1: 试验信息 | Sheet2: 温度数据 | Sheet3: 曲线图
    /// </summary>
    public string ExportExcel(TestMasterRecord record, List<double[]> temperatures)
    {
        var reportDir = _config.OutputDirectory;
        if (!Directory.Exists(reportDir))
            Directory.CreateDirectory(reportDir);

        var filePath = Path.Combine(reportDir, $"{record.TestId}_报告.xlsx");

        using var package = new ExcelPackage();
        var infoSheet = package.Workbook.Worksheets.Add("试验信息");
        var dataSheet = package.Workbook.Worksheets.Add("温度数据");
        var chartSheet = package.Workbook.Worksheets.Add("温度曲线");

        // Sheet1: 试验信息
        FillInfoSheet(infoSheet, record);

        // Sheet2: 温度数据
        FillDataSheet(dataSheet, temperatures);

        // Sheet3: 曲线图
        CreateChartSheet(chartSheet, temperatures);

        package.SaveAs(new FileInfo(filePath));
        Log.Information("Excel 报告已生成: {Path}", filePath);
        return filePath;
    }

    private void FillInfoSheet(ExcelWorksheet sheet, TestMasterRecord r)
    {
        sheet.Cells["A1"].Value = "ISO 11820 不燃性试验报告";
        sheet.Cells["A1"].Style.Font.Size = 16;
        sheet.Cells["A1"].Style.Font.Bold = true;

        int row = 3;
        void AddRow(string label, object value)
        {
            sheet.Cells[$"A{row}"].Value = label;
            sheet.Cells[$"A{row}"].Style.Font.Bold = true;
            sheet.Cells[$"B{row}"].Value = value?.ToString() ?? "";
            row++;
        }

        AddRow("试验标识", r.TestId);
        AddRow("样品编号", r.ProductId);
        AddRow("样品名称", r.ProductName);
        AddRow("规格", r.Specification);
        AddRow("试验日期", r.TestDate);
        AddRow("操作员", r.Operator);
        AddRow("设备编号", r.ApparatusId);
        AddRow("设备名称", r.ApparatusName);
        row++;
        AddRow("环境温度 (°C)", r.AmbientTemp);
        AddRow("环境湿度 (%)", r.AmbientHumidity);
        row++;
        AddRow("试验前质量 (g)", r.PreWeight);
        AddRow("试验后质量 (g)", r.PostWeight);
        AddRow("失重量 (g)", r.LostWeight.ToString("F2"));
        AddRow("失重率 (%)", r.LostWeightPer.ToString("F2"));
        row++;
        AddRow("炉温1 温升 (°C)", r.DeltaTF1.ToString("F1"));
        AddRow("炉温2 温升 (°C)", r.DeltaTF2.ToString("F1"));
        AddRow("表面温升 (°C)", r.DeltaTS.ToString("F1"));
        AddRow("中心温升 (°C)", r.DeltaTC.ToString("F1"));
        AddRow("综合温升 deltatf (°C)", r.Deltatf.ToString("F1"));
        row++;
        AddRow("是否出现火焰", r.HasFlame ? "是" : "否");
        if (r.HasFlame)
        {
            AddRow("火焰发生时刻 (s)", r.FlameStartTime);
            AddRow("火焰持续时间 (s)", r.FlameDuration);
        }
        row++;
        AddRow("试验时长 (s)", r.TotalTestTime);
        AddRow("恒功率值", r.ConstPowerValue);
        row++;
        string verdict = TestMetrics.ComputeVerdict(r.Deltatf, r.LostWeightPer, r.FlameDuration);
        AddRow("判定结论", verdict);

        sheet.Column(1).Width = 25;
        sheet.Column(2).Width = 30;
    }

    private void FillDataSheet(ExcelWorksheet sheet, List<double[]> temps)
    {
        sheet.Cells["A1"].Value = "时间(秒)";
        sheet.Cells["B1"].Value = "炉温1 (°C)";
        sheet.Cells["C1"].Value = "炉温2 (°C)";
        sheet.Cells["D1"].Value = "表面温 (°C)";
        sheet.Cells["E1"].Value = "中心温 (°C)";
        sheet.Cells["F1"].Value = "校准温 (°C)";

        for (int i = 0; i < temps.Count; i++)
        {
            sheet.Cells[$"A{i + 2}"].Value = i;
            sheet.Cells[$"B{i + 2}"].Value = temps[i][0];
            sheet.Cells[$"C{i + 2}"].Value = temps[i][1];
            sheet.Cells[$"D{i + 2}"].Value = temps[i][2];
            sheet.Cells[$"E{i + 2}"].Value = temps[i][3];
            sheet.Cells[$"F{i + 2}"].Value = temps[i][4];
        }

        sheet.Column(1).Width = 12;
        sheet.Column(2).Width = 14;
        sheet.Column(3).Width = 14;
        sheet.Column(4).Width = 14;
        sheet.Column(5).Width = 14;
        sheet.Column(6).Width = 14;
    }

    private void CreateChartSheet(ExcelWorksheet sheet, List<double[]> temps)
    {
        if (temps.Count < 2) return;

        // 写入数据到本表（隐藏区域）
        for (int i = 0; i < temps.Count; i++)
        {
            sheet.Cells[$"A{i + 1}"].Value = i;
            sheet.Cells[$"B{i + 1}"].Value = temps[i][0];
            sheet.Cells[$"C{i + 1}"].Value = temps[i][1];
            sheet.Cells[$"D{i + 1}"].Value = temps[i][2];
            sheet.Cells[$"E{i + 1}"].Value = temps[i][3];
        }

        var chart = sheet.Drawings.AddChart("TemperatureChart", eChartType.XYScatterLines);
        chart.Title.Text = "温度曲线";
        chart.XAxis.Title.Text = "时间 (秒)";
        chart.YAxis.Title.Text = "温度 (°C)";
        chart.SetSize(800, 500);
        chart.SetPosition(1, 0, 6, 0);

        int count = temps.Count;
        var series1 = chart.Series.Add($"B1:B{count}", $"A1:A{count}");
        series1.Header = "炉温1";
        var series2 = chart.Series.Add($"C1:C{count}", $"A1:A{count}");
        series2.Header = "炉温2";
        var series3 = chart.Series.Add($"D1:D{count}", $"A1:A{count}");
        series3.Header = "表面温";
        var series4 = chart.Series.Add($"E1:E{count}", $"A1:A{count}");
        series4.Header = "中心温";
    }

    /// <summary>
    /// 导出 PDF 报告
    /// </summary>
    public string ExportPdf(TestMasterRecord record, List<double[]> temperatures)
    {
        var reportDir = _config.OutputDirectory;
        if (!Directory.Exists(reportDir))
            Directory.CreateDirectory(reportDir);

        var filePath = Path.Combine(reportDir, $"{record.TestId}_报告.pdf");

        var doc = new MigraDoc.DocumentObjectModel.Document();
        var section = doc.AddSection();
        section.PageSetup.TopMargin = "3cm";
        section.PageSetup.BottomMargin = "2cm";

        // 标题
        var title = section.AddParagraph("ISO 11820 不燃性试验报告");
        title.Format.Font.Size = 18;
        title.Format.Font.Bold = true;
        title.Format.Alignment = ParagraphAlignment.Center;
        title.Format.SpaceAfter = "1cm";

        // 基本信息表格
        var table = section.AddTable();
        table.Borders.Visible = true;
        table.AddColumn("5cm");
        table.AddColumn("11cm");

        void AddTableRow(string label, string value)
        {
            var row = table.AddRow();
            row.Cells[0].AddParagraph(label).Format.Font.Bold = true;
            row.Cells[1].AddParagraph(value);
        }

        AddTableRow("试验标识", record.TestId);
        AddTableRow("样品编号", record.ProductId);
        AddTableRow("样品名称", record.ProductName);
        AddTableRow("试验日期", record.TestDate);
        AddTableRow("操作员", record.Operator);
        AddTableRow("试验前质量 (g)", record.PreWeight.ToString("F2"));
        AddTableRow("试验后质量 (g)", record.PostWeight.ToString("F2"));
        AddTableRow("失重量 (g)", record.LostWeight.ToString("F2"));
        AddTableRow("失重率 (%)", record.LostWeightPer.ToString("F2"));
        AddTableRow("综合温升 (°C)", record.Deltatf.ToString("F1"));
        AddTableRow("试验时长 (秒)", record.TotalTestTime.ToString());

        string verdict = TestMetrics.ComputeVerdict(record.Deltatf, record.LostWeightPer, record.FlameDuration);
        AddTableRow("判定结论", verdict);

        // 保存
        var renderer = new PdfDocumentRenderer();
        renderer.Document = doc;
        renderer.RenderDocument();
        renderer.PdfDocument.Save(filePath);

        Log.Information("PDF 报告已生成: {Path}", filePath);
        return filePath;
    }

    /// <summary>
    /// 生成 CSV 文件（如果尚未生成）
    /// </summary>
    public string ExportCsv(string productId, string testId, List<double[]> temperatures)
    {
        var dir = Path.Combine(_config.TestDataDirectory, productId, testId);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, "sensor_data.csv");
        var counter = 0;
        using var writer = new StreamWriter(filePath);
        writer.WriteLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");
        foreach (var t in temperatures)
        {
            writer.WriteLine($"{counter},{t[0]:F1},{t[1]:F1},{t[2]:F1},{t[3]:F1},{t[4]:F1}");
            counter++;
        }

        Log.Information("CSV 文件已导出: {Path}", filePath);
        return filePath;
    }

    /// <summary>
    /// 读取已有 CSV 文件
    /// </summary>
    public List<double[]>? ReadCsv(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var result = new List<double[]>();
        var lines = File.ReadAllLines(filePath);
        for (int i = 1; i < lines.Length; i++) // skip header
        {
            var parts = lines[i].Split(',');
            if (parts.Length >= 6)
            {
                result.Add(new[]
                {
                    double.Parse(parts[1]),
                    double.Parse(parts[2]),
                    double.Parse(parts[3]),
                    double.Parse(parts[4]),
                    double.Parse(parts[5]),
                });
            }
        }
        return result;
    }
}
