using BuildingLearn.Global;
using BuildingLearn.Data.Models;
using System.Data;

namespace BuildingLearn;

/// <summary>
/// 历史记录查询面板
/// </summary>
public class HistoryForm : Form
{
    private DateTimePicker? _dtpStart;
    private DateTimePicker? _dtpEnd;
    private TextBox? _txtSearchProduct;
    private ComboBox? _cboOperator;
    private Button? _btnSearch;
    private Button? _btnExport;
    private DataGridView? _dgvResults;
    private readonly AppGlobal _ctx;

    public HistoryForm()
    {
        _ctx = AppGlobal.Instance;
        InitializeUI();
    }

    private void InitializeUI()
    {
        this.BackColor = SystemColors.Control;

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45,
            Padding = new Padding(5),
        };

        int x = 5, y = 8;

        topPanel.Controls.Add(new Label { Text = "日期:", Location = new Point(x, y + 3), Size = new Size(40, 20) });
        x += 40;
        _dtpStart = new DateTimePicker
        {
            Location = new Point(x, y),
            Size = new Size(120, 23),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now.AddMonths(-3),
        };
        topPanel.Controls.Add(_dtpStart);
        x += 125;
        topPanel.Controls.Add(new Label { Text = "至", Location = new Point(x, y + 3), Size = new Size(20, 20) });
        x += 25;
        _dtpEnd = new DateTimePicker
        {
            Location = new Point(x, y),
            Size = new Size(120, 23),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now,
        };
        topPanel.Controls.Add(_dtpEnd);
        x += 130;

        topPanel.Controls.Add(new Label { Text = "样品:", Location = new Point(x, y + 3), Size = new Size(40, 20) });
        x += 40;
        _txtSearchProduct = new TextBox { Location = new Point(x, y), Size = new Size(100, 23) };
        topPanel.Controls.Add(_txtSearchProduct);
        x += 110;

        topPanel.Controls.Add(new Label { Text = "操作员:", Location = new Point(x, y + 3), Size = new Size(55, 20) });
        x += 55;
        _cboOperator = new ComboBox { Location = new Point(x, y), Size = new Size(100, 23), DropDownStyle = ComboBoxStyle.DropDownList };
        _cboOperator.Items.Add("全部");
        try
        {
            var ops = _ctx.Db.GetOperatorsByRole("admin");
            ops.AddRange(_ctx.Db.GetOperatorsByRole("experimenter"));
            foreach (var op in ops.DistinctBy(o => o.Username))
                _cboOperator.Items.Add(op.Username);
        }
        catch { }
        _cboOperator.SelectedIndex = 0;
        topPanel.Controls.Add(_cboOperator);
        x += 110;

        _btnSearch = new Button
        {
            Text = "查询",
            Location = new Point(x, y - 2),
            Size = new Size(65, 28),
            BackColor = Color.LightSteelBlue,
        };
        _btnSearch.Click += (s, e) => RefreshQuery();
        topPanel.Controls.Add(_btnSearch);
        x += 75;

        _btnExport = new Button
        {
            Text = "导出 Excel",
            Location = new Point(x, y - 2),
            Size = new Size(85, 28),
        };
        _btnExport.Click += BtnExport_Click;
        topPanel.Controls.Add(_btnExport);

        this.Controls.Add(topPanel);

        // 数据表格
        _dgvResults = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            RowHeadersVisible = false,
        };
        _dgvResults.DoubleClick += DgvResults_DoubleClick;
        this.Controls.Add(_dgvResults);
    }

    public void RefreshQuery()
    {
        try
        {
            string? productId = string.IsNullOrWhiteSpace(_txtSearchProduct!.Text) ? null : _txtSearchProduct.Text.Trim();
            string? startDate = _dtpStart!.Value.ToString("yyyy-MM-dd");
            string? endDate = _dtpEnd!.Value.ToString("yyyy-MM-dd");
            string? op = _cboOperator!.SelectedIndex <= 0 ? null : _cboOperator.SelectedItem?.ToString();

            var records = _ctx.Db.QueryTestMasters(productId, startDate, endDate, op);

            var dt = new DataTable();
            dt.Columns.Add("样品编号");
            dt.Columns.Add("试验标识");
            dt.Columns.Add("试验日期");
            dt.Columns.Add("样品名称");
            dt.Columns.Add("操作员");
            dt.Columns.Add("失重率(%)");
            dt.Columns.Add("温升(°C)");
            dt.Columns.Add("时长(秒)");
            dt.Columns.Add("判定");
            dt.Columns.Add("状态");

            foreach (var r in records)
            {
                var row = dt.NewRow();
                row[0] = r.ProductId;
                row[1] = r.TestId;
                row[2] = r.TestDate;
                row[3] = r.ProductName;
                row[4] = r.Operator;
                row[5] = r.LostWeightPer.ToString("F2");
                row[6] = r.Deltatf.ToString("F1");
                row[7] = r.TotalTestTime;
                string verdict = r.Flag == "10000000"
                    ? Services.TestMetrics.ComputeVerdict(r.Deltatf, r.LostWeightPer, r.FlameDuration)
                    : "未完成";
                row[8] = verdict;
                row[9] = r.Flag == "10000000" ? "已完成" : "未保存";
                dt.Rows.Add(row);
            }

            _dgvResults!.DataSource = dt;
        }
        catch (Exception)
        {
            // 静默失败
        }
    }

    private void DgvResults_DoubleClick(object? sender, EventArgs e)
    {
        if (_dgvResults!.SelectedRows.Count == 0) return;

        var row = _dgvResults.SelectedRows[0];
        string productId = row.Cells[0].Value?.ToString() ?? "";
        string testId = row.Cells[1].Value?.ToString() ?? "";

        var record = _ctx.Db.GetTestMaster(productId, testId);
        if (record == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"试验标识: {record.TestId}");
        sb.AppendLine($"样品编号: {record.ProductId}");
        sb.AppendLine($"样品名称: {record.ProductName}");
        sb.AppendLine($"试验日期: {record.TestDate}");
        sb.AppendLine($"操作员: {record.Operator}");
        sb.AppendLine($"设备: {record.ApparatusId} {record.ApparatusName}");
        sb.AppendLine($"环境温度: {record.AmbientTemp:F1} °C");
        sb.AppendLine($"环境湿度: {record.AmbientHumidity:F1} %");
        sb.AppendLine();
        sb.AppendLine($"试验前质量: {record.PreWeight:F2} g");
        sb.AppendLine($"试验后质量: {record.PostWeight:F2} g");
        sb.AppendLine($"失重量: {record.LostWeight:F2} g");
        sb.AppendLine($"失重率: {record.LostWeightPer:F2} %");
        sb.AppendLine();
        sb.AppendLine($"炉温1 温升: {record.DeltaTF1:F1} °C");
        sb.AppendLine($"炉温2 温升: {record.DeltaTF2:F1} °C");
        sb.AppendLine($"表面温升: {record.DeltaTS:F1} °C");
        sb.AppendLine($"中心温升: {record.DeltaTC:F1} °C");
        sb.AppendLine($"综合温升 deltatf: {record.Deltatf:F1} °C");
        sb.AppendLine();
        sb.AppendLine($"试验时长: {record.TotalTestTime} 秒");
        sb.AppendLine($"恒功率值: {record.ConstPowerValue}");
        sb.AppendLine($"火焰: {(record.HasFlame ? $"是 (发生 {record.FlameStartTime}s, 持续 {record.FlameDuration}s)" : "否")}");
        string verdict = Services.TestMetrics.ComputeVerdict(record.Deltatf, record.LostWeightPer, record.FlameDuration);
        sb.AppendLine($"判定: {verdict}");

        MessageBox.Show(sb.ToString(), $"试验详情 — {productId}/{testId}", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        if (_dgvResults!.Rows.Count == 0)
        {
            MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                Title = "导出查询结果",
                FileName = $"试验记录_{DateTime.Now:yyyyMMdd}.xlsx",
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            string? productId = string.IsNullOrWhiteSpace(_txtSearchProduct!.Text) ? null : _txtSearchProduct.Text.Trim();
            string? startDate = _dtpStart!.Value.ToString("yyyy-MM-dd");
            string? endDate = _dtpEnd!.Value.ToString("yyyy-MM-dd");
            string? op = _cboOperator!.SelectedIndex <= 0 ? null : _cboOperator.SelectedItem?.ToString();
            var records = _ctx.Db.QueryTestMasters(productId, startDate, endDate, op);

            using var package = new OfficeOpenXml.ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("试验记录");
            sheet.Cells["A1"].Value = "样品编号";
            sheet.Cells["B1"].Value = "试验标识";
            sheet.Cells["C1"].Value = "试验日期";
            sheet.Cells["D1"].Value = "样品名称";
            sheet.Cells["E1"].Value = "操作员";
            sheet.Cells["F1"].Value = "失重率(%)";
            sheet.Cells["G1"].Value = "温升(°C)";
            sheet.Cells["H1"].Value = "时长(秒)";
            sheet.Cells["I1"].Value = "判定";

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i]; int ri = i + 2;
                sheet.Cells[$"A{ri}"].Value = r.ProductId;
                sheet.Cells[$"B{ri}"].Value = r.TestId;
                sheet.Cells[$"C{ri}"].Value = r.TestDate;
                sheet.Cells[$"D{ri}"].Value = r.ProductName;
                sheet.Cells[$"E{ri}"].Value = r.Operator;
                sheet.Cells[$"F{ri}"].Value = r.LostWeightPer;
                sheet.Cells[$"G{ri}"].Value = r.Deltatf;
                sheet.Cells[$"H{ri}"].Value = r.TotalTestTime;
                sheet.Cells[$"I{ri}"].Value = Services.TestMetrics.ComputeVerdict(r.Deltatf, r.LostWeightPer, r.FlameDuration);
            }
            package.SaveAs(new FileInfo(sfd.FileName));
            MessageBox.Show("导出成功！", "导出完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
