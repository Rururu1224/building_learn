using BuildingLearn.Global;
using BuildingLearn.Data.Models;

namespace BuildingLearn;

/// <summary>
/// Person A: 设备校准面板
/// </summary>
public class CalibrationForm : Form
{
    private Label? _lblCalTemp;
    private TextBox? _txtReferenceTemp;
    private TextBox? _txtRemark;
    private Button? _btnRecord;
    private DataGridView? _dgvCalHistory;
    private System.Windows.Forms.Timer? _refreshTimer;
    private readonly AppGlobal _ctx;

    public CalibrationForm()
    {
        _ctx = AppGlobal.Instance;
        InitializeUI();
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _refreshTimer.Tick += (s, e) => RefreshCalTemp();
        _refreshTimer.Start();
    }

    private void InitializeUI()
    {
        this.BackColor = SystemColors.Control;
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(8) };

        topPanel.Controls.Add(new Label { Text = "校准温度 (TCal):", Location = new Point(10, 10), Size = new Size(110, 20), Font = new Font("Microsoft YaHei", 9) });
        _lblCalTemp = new Label { Text = "— °C", Location = new Point(10, 32), Size = new Size(150, 24), Font = new Font("Consolas", 16, FontStyle.Bold), ForeColor = Color.Magenta };
        topPanel.Controls.Add(_lblCalTemp);

        topPanel.Controls.Add(new Label { Text = "标准温度:", Location = new Point(180, 10), Size = new Size(65, 20) });
        _txtReferenceTemp = new TextBox { Location = new Point(245, 8), Size = new Size(80, 23), Text = "750.0" };
        topPanel.Controls.Add(_txtReferenceTemp);

        topPanel.Controls.Add(new Label { Text = "备注:", Location = new Point(340, 10), Size = new Size(40, 20) });
        _txtRemark = new TextBox { Location = new Point(380, 8), Size = new Size(200, 23) };
        topPanel.Controls.Add(_txtRemark);

        _btnRecord = new Button { Text = "记录校准点", Location = new Point(590, 6), Size = new Size(110, 28), BackColor = Color.LightSteelBlue };
        _btnRecord.Click += BtnRecord_Click;
        topPanel.Controls.Add(_btnRecord);
        this.Controls.Add(topPanel);

        _dgvCalHistory = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White, RowHeadersVisible = false };
        _dgvCalHistory.Columns.Add("colId", "ID"); _dgvCalHistory.Columns.Add("colDate", "日期"); _dgvCalHistory.Columns.Add("colOperator", "操作员");
        _dgvCalHistory.Columns.Add("colRef", "标准温度 (°C)"); _dgvCalHistory.Columns.Add("colMeas", "测量温度 (°C)"); _dgvCalHistory.Columns.Add("colDev", "偏差 (°C)");
        _dgvCalHistory.Columns.Add("colRemark", "备注");
        this.Controls.Add(_dgvCalHistory);
        LoadCalibrationHistory();
    }

    private void RefreshCalTemp()
    {
        try { _lblCalTemp!.Text = $"{_ctx.Simulator.TCal:F1} °C"; } catch { _lblCalTemp!.Text = "— °C"; }
    }

    private void BtnRecord_Click(object? sender, EventArgs e)
    {
        if (!double.TryParse(_txtReferenceTemp!.Text, out double refTemp))
        { MessageBox.Show("请输入有效的标准温度", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        double measuredTemp = _ctx.Simulator.TCal;
        double deviation = measuredTemp - refTemp;

        var record = new CalibrationRecord
        {
            CalibrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Operator = _ctx.CurrentOperator,
            ApparatusId = _ctx.TestMaster.CurrentTrial?.ApparatusId ?? "ISO-001",
            ReferenceTemp = refTemp, MeasuredTemp = measuredTemp, Deviation = deviation, Remark = _txtRemark!.Text,
        };
        _ctx.Db.InsertCalibrationRecord(record);
        LoadCalibrationHistory();
        MessageBox.Show($"校准点已记录\n偏差: {deviation:F2} °C", "记录完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LoadCalibrationHistory()
    {
        try
        {
            var records = _ctx.Db.GetCalibrationRecords();
            _dgvCalHistory!.Rows.Clear();
            foreach (var r in records)
                _dgvCalHistory.Rows.Add(r.Id, r.CalibrationDate, r.Operator, r.ReferenceTemp.ToString("F1"), r.MeasuredTemp.ToString("F1"), r.Deviation.ToString("F2"), r.Remark);
        }
        catch { }
    }
}
