using BuildingLearn.Global;
using BuildingLearn.Core;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using OxyPlot.Axes;

namespace BuildingLearn;

/// <summary>
/// Person A: 主界面 — 温度实时显示 / 曲线图 / 按钮控制 / 系统消息。
/// 只做 UI 展示与事件转发，所有业务调用 Person B / Person C 对外接口。
/// </summary>
public class MainForm : Form
{
    private readonly AppGlobal _ctx;

    // 温度数值面板
    private Label? _lblTF1, _lblTF2, _lblTS, _lblTC, _lblTCal;
    private Label? _lblState, _lblTimer, _lblDrift, _lblProductId;
    // 按钮
    private Button? _btnNewTest, _btnStartHeat, _btnStopHeat, _btnStartRecord, _btnStopRecord, _btnTestRecord, _btnConfig;
    // 曲线图
    private PlotView? _plotView;
    private PlotModel? _plotModel;
    private LineSeries? _seriesTF1, _seriesTF2, _seriesTS, _seriesTC;
    // 消息区域
    private RichTextBox? _rtbMessages;
    private TabControl? _tabControl;
    private TabPage? _tabMonitor, _tabHistory, _tabCalibration;
    private HistoryForm? _historyForm;
    private CalibrationForm? _calibrationForm;

    private bool _isClosing;

    public MainForm()
    {
        _ctx = AppGlobal.Instance;
        _ctx.TestMaster.DataBroadcast += OnDataBroadcast;
        InitializeUI();
        this.Text = "ISO 11820 建筑材料不燃性试验系统";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Size = new Size(1200, 750);
        this.MinimumSize = new Size(1000, 650);
        this.FormClosing += (s, e) =>
        {
            _isClosing = true;
            _ctx.DaqWorker.Stop();
            _ctx.TestMaster.DataBroadcast -= OnDataBroadcast;
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateButtonStates();
    }

    // ======== UI 布局（省略拆分，与之前一致） ========

    private void InitializeUI()
    {
        var mainPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(8) };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 75));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        this.Controls.Add(mainPanel);

        mainPanel.Controls.Add(BuildLeftPanel(), 0, 0);
        BuildPlotView();
        mainPanel.Controls.Add(_plotView!, 1, 0);
        mainPanel.Controls.Add(BuildButtonPanel(), 2, 0);
        var bottom = BuildBottomPanel();
        mainPanel.SetColumnSpan(bottom, 3);
        mainPanel.Controls.Add(bottom, 0, 1);
    }

    private Panel BuildLeftPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30) };
        int y = 10;
        var titleFont = new Font("Consolas", 9, FontStyle.Bold);
        var valueFont = new Font("Consolas", 18, FontStyle.Bold);
        var labelFont = new Font("Microsoft YaHei", 8);

        void AddChannel(string name, int x, ref Label? lbl, Color color)
        {
            panel.Controls.Add(new Label { Text = name, Location = new Point(x, y), Size = new Size(200, 16), ForeColor = color, BackColor = Color.Transparent, Font = titleFont });
            lbl = new Label { Text = "0.0 °C", Location = new Point(x, y + 18), Size = new Size(200, 30), ForeColor = color, BackColor = Color.Transparent, Font = valueFont };
            panel.Controls.Add(lbl);
            y += 50;
        }

        panel.Controls.Add(new Label { Text = "样品编号", Location = new Point(10, y), Size = new Size(200, 16), ForeColor = Color.Gray, BackColor = Color.Transparent, Font = labelFont });
        _lblProductId = new Label { Text = "—", Location = new Point(10, y + 16), Size = new Size(200, 20), ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Consolas", 11) };
        panel.Controls.Add(_lblProductId);
        y += 40;

        AddChannel("炉温1  TF1", 10, ref _lblTF1, Color.LimeGreen);
        AddChannel("炉温2  TF2", 10, ref _lblTF2, Color.Cyan);
        AddChannel("表面温  TS", 10, ref _lblTS, Color.Orange);
        AddChannel("中心温  TC", 10, ref _lblTC, Color.Yellow);
        AddChannel("校准温  TCal", 10, ref _lblTCal, Color.Magenta);
        y += 10;

        panel.Controls.Add(new Label { Location = new Point(10, y), Size = new Size(200, 2), BorderStyle = BorderStyle.Fixed3D });
        y += 10;

        _lblState = new Label { Location = new Point(10, y), Size = new Size(200, 22), ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Microsoft YaHei", 11, FontStyle.Bold), Text = "空闲" };
        panel.Controls.Add(_lblState); y += 30;

        panel.Controls.Add(new Label { Text = "记录时间", Location = new Point(10, y), Size = new Size(100, 14), ForeColor = Color.Gray, BackColor = Color.Transparent, Font = labelFont });
        _lblTimer = new Label { Text = "0 s", Location = new Point(10, y + 15), Size = new Size(200, 22), ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Consolas", 14, FontStyle.Bold) };
        panel.Controls.Add(_lblTimer); y += 40;

        panel.Controls.Add(new Label { Text = "温漂 (10min)", Location = new Point(10, y), Size = new Size(100, 14), ForeColor = Color.Gray, BackColor = Color.Transparent, Font = labelFont });
        _lblDrift = new Label { Text = "—", Location = new Point(10, y + 15), Size = new Size(200, 22), ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Consolas", 14, FontStyle.Bold) };
        panel.Controls.Add(_lblDrift);

        return panel;
    }

    private void BuildPlotView()
    {
        _plotModel = new PlotModel { Title = "温度曲线", TitleFontSize = 12, PlotAreaBorderColor = OxyColors.Gray };
        _plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "时间 (秒)", Minimum = 0, Maximum = 600, IsZoomEnabled = false });
        _plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "温度 (°C)", Minimum = 0, Maximum = 800, IsZoomEnabled = false });

        _seriesTF1 = new LineSeries { Title = "炉温1", Color = OxyColor.FromRgb(0, 255, 0), StrokeThickness = 1.5 };
        _seriesTF2 = new LineSeries { Title = "炉温2", Color = OxyColor.FromRgb(0, 200, 255), StrokeThickness = 1.5 };
        _seriesTS  = new LineSeries { Title = "表面温", Color = OxyColor.FromRgb(255, 165, 0), StrokeThickness = 1.5 };
        _seriesTC  = new LineSeries { Title = "中心温", Color = OxyColor.FromRgb(255, 255, 0), StrokeThickness = 1.5 };
        _plotModel.Series.Add(_seriesTF1); _plotModel.Series.Add(_seriesTF2);
        _plotModel.Series.Add(_seriesTS);  _plotModel.Series.Add(_seriesTC);

        _plotView = new PlotView { Dock = DockStyle.Fill, Model = _plotModel, BackColor = Color.White };
    }

    private Panel BuildButtonPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 240, 240) };
        int y = 10, btnW = 140, btnH = 38;

        Button MakeBtn(string text, int yPos, Color? bc, EventHandler? h)
        {
            var btn = new Button { Text = text, Location = new Point(15, yPos), Size = new Size(btnW, btnH), BackColor = bc ?? SystemColors.Control, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei", 9) };
            btn.FlatAppearance.BorderSize = 0;
            if (h != null) btn.Click += h;
            panel.Controls.Add(btn);
            return btn;
        }

        _btnNewTest = MakeBtn("新建试验", y, Color.LightSteelBlue, BtnNewTest_Click); y += 50;
        _btnStartHeat = MakeBtn("开始升温", y, Color.FromArgb(255, 140, 100), BtnStartHeat_Click); y += 50;
        _btnStopHeat = MakeBtn("停止升温", y, Color.FromArgb(200, 200, 200), BtnStopHeat_Click); y += 50;
        _btnStartRecord = MakeBtn("开始记录", y, Color.FromArgb(100, 220, 100), BtnStartRecord_Click); y += 50;
        _btnStopRecord = MakeBtn("停止记录", y, Color.FromArgb(255, 200, 100), BtnStopRecord_Click); y += 50;
        _btnTestRecord = MakeBtn("试验记录", y, Color.FromArgb(180, 220, 255), BtnTestRecord_Click); y += 50;
        _btnConfig = MakeBtn("参数设置", y, null, BtnConfig_Click); y += 60;

        var legend = new GroupBox { Text = "状态说明", Location = new Point(15, y), Size = new Size(btnW, 160), Font = new Font("Microsoft YaHei", 8) };
        legend.Controls.Add(new Label { Text = "● Idle     空闲\n● Preparing 升温中\n● Ready     就绪\n● Recording 记录中\n● Complete  完成", Location = new Point(10, 20), Size = new Size(130, 120), Font = new Font("Consolas", 9) });
        panel.Controls.Add(legend);
        return panel;
    }

    private Panel BuildBottomPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        _tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei", 9) };

        _tabMonitor = new TabPage("系统消息");
        _rtbMessages = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 20, 20), ForeColor = Color.White, Font = new Font("Consolas", 9), ReadOnly = true, WordWrap = true };
        _tabMonitor.Controls.Add(_rtbMessages);

        _tabHistory = new TabPage("记录查询");
        _historyForm = new HistoryForm { Dock = DockStyle.Fill, TopLevel = false, FormBorderStyle = FormBorderStyle.None };
        _tabHistory.Controls.Add(_historyForm); _historyForm.Show();

        _tabCalibration = new TabPage("设备校准");
        _calibrationForm = new CalibrationForm { Dock = DockStyle.Fill, TopLevel = false, FormBorderStyle = FormBorderStyle.None };
        _tabCalibration.Controls.Add(_calibrationForm); _calibrationForm.Show();

        _tabControl.TabPages.Add(_tabMonitor); _tabControl.TabPages.Add(_tabHistory); _tabControl.TabPages.Add(_tabCalibration);
        panel.Controls.Add(_tabControl);
        return panel;
    }

    // ======== DataBroadcast 事件处理（跨线程 Invoke） ========

    private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
    {
        if (_isClosing) return;
        try
        {
            if (this.InvokeRequired) this.Invoke(() => ProcessDataBroadcast(e));
            else ProcessDataBroadcast(e);
        }
        catch { }
    }

    private void ProcessDataBroadcast(DataBroadcastEventArgs e)
    {
        _lblTF1!.Text = $"{e.Temperatures[0]:F1} °C";
        _lblTF2!.Text = $"{e.Temperatures[1]:F1} °C";
        _lblTS!.Text = $"{e.Temperatures[2]:F1} °C";
        _lblTC!.Text = $"{e.Temperatures[3]:F1} °C";
        _lblTCal!.Text = $"{e.Temperatures[4]:F1} °C";
        _lblState!.Text = GetStateText(e.State);
        _lblTimer!.Text = $"{e.ElapsedSeconds} s";
        _lblDrift!.Text = $"{e.TemperatureDrift:F2} °C/10min";
        if (!string.IsNullOrEmpty(e.ProductId)) _lblProductId!.Text = e.ProductId;

        UpdatePlot(e.Temperatures);

        foreach (var msg in e.Messages)
        {
            Color color = msg.Message.Contains("终止") || msg.Message.Contains("满足") ? Color.Yellow
                : msg.Message.Contains("回退") || msg.Message.Contains("跌落") ? Color.OrangeRed : Color.White;
            _rtbMessages!.SelectionColor = color;
            _rtbMessages.AppendText($"{msg.Time}  {msg.Message}\n");
            _rtbMessages.ScrollToCaret();
        }
        UpdateButtonStates();
    }

    private int _plotPointCount;
    private void UpdatePlot(double[] temps)
    {
        _plotPointCount++;
        double x = _plotPointCount;
        _seriesTF1!.Points.Add(new DataPoint(x, temps[0]));
        _seriesTF2!.Points.Add(new DataPoint(x, temps[1]));
        _seriesTS!.Points.Add(new DataPoint(x, temps[2]));
        _seriesTC!.Points.Add(new DataPoint(x, temps[3]));

        if (_plotPointCount > 600) { _plotModel!.Axes[0].Minimum = _plotPointCount - 600; _plotModel.Axes[0].Maximum = _plotPointCount; }
        if (_seriesTF1.Points.Count > 2000)
        {
            int remove = _seriesTF1.Points.Count - 2000;
            for (int i = 0; i < remove; i++) { _seriesTF1.Points.RemoveAt(0); _seriesTF2.Points.RemoveAt(0); _seriesTS.Points.RemoveAt(0); _seriesTC.Points.RemoveAt(0); }
        }
        _plotView!.InvalidatePlot(true);
    }

    private static string GetStateText(TestStates state) => state switch
    {
        TestStates.Idle => "空闲", TestStates.Preparing => "升温中", TestStates.Ready => "就绪",
        TestStates.Recording => "记录中", TestStates.Complete => "完成", _ => "未知"
    };

    // ======== 按钮事件（全部转发调用 Person B / Person C） ========

    private void BtnNewTest_Click(object? sender, EventArgs e)
    {
        if (_ctx.TestMaster.IsCompleteUnsaved)
        { MessageBox.Show("当前试验已完成但尚未保存试验记录，请先保存！", "操作禁止", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var form = new NewTestForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _lblProductId!.Text = _ctx.TestMaster.CurrentProductId;
            _plotPointCount = 0;
            _seriesTF1!.Points.Clear(); _seriesTF2!.Points.Clear(); _seriesTS!.Points.Clear(); _seriesTC!.Points.Clear();
            _plotView!.InvalidatePlot(true);
            UpdateButtonStates();
        }
    }

    private void BtnStartHeat_Click(object? sender, EventArgs e)
    {
        if (_ctx.TestMaster.State != TestStates.Idle) return;
        if (_ctx.TestMaster.CurrentTrial == null)
        { MessageBox.Show("请先新建试验", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        _ctx.TestMaster.StartHeating();
        _ctx.Simulator.Reset();
        _ctx.DaqWorker.Start();
        UpdateButtonStates();
    }

    private void BtnStopHeat_Click(object? sender, EventArgs e)
    {
        if (_ctx.TestMaster.State != TestStates.Preparing && _ctx.TestMaster.State != TestStates.Ready && _ctx.TestMaster.State != TestStates.Complete) return;
        _ctx.TestMaster.StopHeating();
        _ctx.DaqWorker.Stop();
        UpdateButtonStates();
    }

    private void BtnStartRecord_Click(object? sender, EventArgs e)
    {
        if (_ctx.TestMaster.State != TestStates.Ready) return;
        if (_ctx.TestMaster.IsCompleteUnsaved) { MessageBox.Show("当前试验已完成但尚未保存试验记录，请先保存！", "操作禁止", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        // CSV 写入由 AppGlobal 中 DaqWorker.OnCsvLineReady 回调自动处理（Person C 桥接）
        _ctx.TestMaster.StartRecording();
        UpdateButtonStates();
    }

    private void BtnStopRecord_Click(object? sender, EventArgs e)
    {
        if (_ctx.TestMaster.State != TestStates.Recording) return;
        _ctx.TestMaster.StopRecording();
        UpdateButtonStates();
    }

    private void BtnTestRecord_Click(object? sender, EventArgs e)
    {
        var trial = _ctx.TestMaster.CurrentTrial;
        if (trial == null) { MessageBox.Show("没有当前试验记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (_ctx.TestMaster.State != TestStates.Complete && _ctx.TestMaster.TotalTestTime == 0)
        { MessageBox.Show("试验尚未开始记录或未完成", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        var form = new TestRecordForm();
        if (form.ShowDialog(this) == DialogResult.OK) { _ctx.DaqWorker.Stop(); UpdateButtonStates(); }
    }

    private void BtnConfig_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("参数配置功能（可后续扩展）\n\n当前仿真参数：\n初始温度: 720°C\n目标温度: 750°C\n升温速率: 40°C/s\n温度波动: ±0.5°C", "参数配置", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ======== 按钮状态控制（严格按五状态联动） ========

    private void UpdateButtonStates()
    {
        var state = _ctx.TestMaster.State;
        bool isCompleteUnsaved = _ctx.TestMaster.IsCompleteUnsaved;
        bool hasTrial = _ctx.TestMaster.CurrentTrial != null;

        _btnNewTest!.Enabled = state == TestStates.Idle || (state == TestStates.Complete && !isCompleteUnsaved);
        _btnStartHeat!.Enabled = state == TestStates.Idle && hasTrial;
        _btnStopHeat!.Enabled = state == TestStates.Preparing || state == TestStates.Ready || state == TestStates.Complete;
        _btnStartRecord!.Enabled = state == TestStates.Ready && !isCompleteUnsaved;
        _btnStopRecord!.Enabled = state == TestStates.Recording;
        _btnTestRecord!.Enabled = hasTrial && _ctx.TestMaster.TotalTestTime > 0;
        _btnConfig!.Enabled = state != TestStates.Recording;
        _historyForm?.RefreshQuery();
    }
}
