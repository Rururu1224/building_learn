#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BuildingFireTest.Interfaces;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace BuildingFireTest.UI
{
    /// <summary>
    /// 主窗体
    /// 使用 Dock 停靠布局，避免文字堆叠，适应不同 DPI 和窗口尺寸
    /// </summary>
    public partial class MainForm : Form
    {
        // ========== 依赖注入 ==========
        private readonly ICoreService _coreService;
        private readonly IDataService _dataService;

        // ========== 温度曲线数据缓存 ==========
        private readonly List<DataPoint> _seriesFurnace1 = new();
        private readonly List<DataPoint> _seriesFurnace2 = new();
        private readonly List<DataPoint> _seriesSurface = new();
        private readonly List<DataPoint> _seriesCenter = new();
        private int _dataPointIndex;

        // ========== OxyPlot 相关 ==========
        private PlotView plotView;
        private PlotModel plotModel;
        private LineSeries lineFurnace1;
        private LineSeries lineFurnace2;
        private LineSeries lineSurface;
        private LineSeries lineCenter;

        // ========== 温度LED面板控件 ==========
        private Label lblTF1Val, lblTF2Val, lblTSVal, lblTCVal, lblTCalVal;

        // ========== 状态信息栏 ==========
        private Label lblStatusValue;
        private Label lblTimerValue;
        private Label lblDriftValue;
        private Label lblProductValue;

        // ========== 系统消息日志 ==========
        private RichTextBox rtbMessageLog;

        // ========== 按钮 ==========
        private Button btnNewTest;
        private Button btnStartHeating;
        private Button btnStopHeating;
        private Button btnStartRecording;
        private Button btnStopRecording;
        private Button btnTestRecord;
        private Button btnParameterSettings;

        // ========== TabControl ==========
        private TabControl tabControl;
        private TabPage tabMain;
        private TabPage tabCalibration;
        private TabPage tabRecordQuery;
        private ComparisonTab _comparisonTab;
        // ========== 当前状态 ==========
        private TestState _currentState = TestState.Idle;
        private bool _hasActiveTest;

        public MainForm(ICoreService coreService, IDataService dataService)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));

            InitializeComponent();
            InitializeOxyPlot();
            InitializeCalibrationTab();
            InitializeRecordQueryTab();
            tabComparison = new TabPage("对比分析");
            tabMain.BackColor = Color.FromArgb(30, 30, 30);
            tabCalibration.BackColor = Color.FromArgb(30, 30, 30);
            tabRecordQuery.BackColor = Color.FromArgb(30, 30, 30);
            tabComparison.BackColor = Color.FromArgb(30, 30, 30);

            tabControl.TabPages.AddRange(new[] { tabMain, tabCalibration, tabRecordQuery, tabComparison });
            // ========== 主Tab页面布局（使用Dock停靠） ==========
            BuildMainTabPage();

            this.Controls.Add(tabControl);
            this.FormClosing += MainForm_FormClosing!;
        }

        #region 主Tab页面构建（Dock停靠布局）

        private void BuildMainTabPage()
        {
            // 布局结构（从上到下）：
            //   1. 温度LED面板 (Dock=Top)
            //   2. 系统消息日志 (Dock=Bottom)
            //   3. 右侧状态+按钮面板 (Dock=Right)
            //   4. 温度曲线图 (Dock=Fill，占满剩余空间)

            // --- 1. 顶部：温度LED面板 ---
            var pnlTempLeds = BuildTemperaturePanel();
            pnlTempLeds.Dock = DockStyle.Top;
            tabMain.Controls.Add(pnlTempLeds);

            // --- 2. 底部：系统消息日志 ---
            var pnlLog = BuildMessageLogPanel();
            pnlLog.Dock = DockStyle.Bottom;
            tabMain.Controls.Add(pnlLog);

            // --- 3. 右侧：状态信息 + 按钮 ---
            var pnlRight = BuildRightPanel();
            pnlRight.Dock = DockStyle.Right;
            tabMain.Controls.Add(pnlRight);

            // --- 4. 中间：OxyPlot曲线图（Fill占满剩余空间） ---
            BuildPlotView();
            plotView.Dock = DockStyle.Fill;
            tabMain.Controls.Add(plotView);
        }

        /// <summary>
        /// 顶部温度LED面板（5通道）
        /// </summary>
        private Panel BuildTemperaturePanel()
        {
            var pnl = new Panel
            {
                Height = 95,
                BackColor = Color.FromArgb(20, 20, 20),
                Padding = new Padding(8, 6, 8, 6)
            };

            // 使用 TableLayoutPanel 均分5列，避免手动算坐标
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 2,
                BackColor = Color.FromArgb(20, 20, 20),
                Margin = new Padding(0),
                Padding = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            // 5列等宽
            for (int i = 0; i < 5; i++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            // 第0行：通道名（矮），第1行：温度值（高）
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var channels = new[]
            {
                ("炉温1 (TF1)", Color.FromArgb(255, 80, 80)),
                ("炉温2 (TF2)", Color.FromArgb(255, 140, 60)),
                ("表面温 (TS)", Color.FromArgb(80, 180, 255)),
                ("中心温 (TC)", Color.FromArgb(80, 255, 120)),
                ("校准温 (TCal)", Color.FromArgb(200, 180, 100))
            };

            for (int i = 0; i < 5; i++)
            {
                // 通道名标签
                var lblName = new Label
                {
                    Text = channels[i].Item1,
                    Font = new Font("Consolas", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(180, 180, 180),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(0)
                };
                table.Controls.Add(lblName, i, 0);

                // 温度值标签（LED大字风格）
                var lblValue = new Label
                {
                    Text = "0.0 °C",
                    Font = new Font("Consolas", 22F, FontStyle.Bold),
                    ForeColor = channels[i].Item2,
                    BackColor = Color.FromArgb(10, 10, 10),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(4, 0, 4, 4)
                };
                table.Controls.Add(lblValue, i, 1);

                // 保存引用
                switch (i)
                {
                    case 0: lblTF1Val = lblValue; break;
                    case 1: lblTF2Val = lblValue; break;
                    case 2: lblTSVal = lblValue; break;
                    case 3: lblTCVal = lblValue; break;
                    case 4: lblTCalVal = lblValue; break;
                }
            }

            pnl.Controls.Add(table);
            return pnl;
        }

        /// <summary>
        /// 温度曲线图
        /// </summary>
        private void BuildPlotView()
        {
            plotView = new PlotView
            {
                BackColor = Color.FromArgb(20, 20, 20),
                Margin = new Padding(4)
            };
        }

        /// <summary>
        /// 右侧面板：状态信息 + 操作按钮
        /// </summary>
        private Panel BuildRightPanel()
        {
            var pnlRight = new Panel
            {
                Width = 310,
                BackColor = Color.FromArgb(35, 35, 35),
                Padding = new Padding(6)
            };

            // --- 状态信息区（Dock=Top） ---
            var pnlStatus = BuildStatusPanel();
            pnlStatus.Dock = DockStyle.Top;
            pnlRight.Controls.Add(pnlStatus);

            // --- 操作按钮区（用固定高度 Panel，放在状态区下方） ---
            var pnlButtons = BuildButtonsPanel();
            pnlButtons.Dock = DockStyle.Top;
            pnlRight.Controls.Add(pnlButtons);

            return pnlRight;
        }

        /// <summary>
        /// 状态信息面板
        /// </summary>
        private Panel BuildStatusPanel()
        {
            var pnl = new Panel
            {
                Height = 210,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(10, 8, 10, 8)
            };

            var lblTitle = new Label
            {
                Text = "试验状态",
                Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 使用 TableLayoutPanel 布局状态行（避免文字堆叠）
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                BackColor = Color.FromArgb(40, 40, 40),
                Margin = new Padding(0),
                Padding = new Padding(0, 6, 0, 0)
            };

            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            for (int i = 0; i < 4; i++)
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));

            // 行0：当前状态
            table.Controls.Add(CreateStatusLabel("当前状态："), 0, 0);
            lblStatusValue = CreateStatusValue("空闲", Color.FromArgb(100, 200, 100));
            table.Controls.Add(lblStatusValue, 1, 0);

            // 行1：记录计时
            table.Controls.Add(CreateStatusLabel("记录计时："), 0, 1);
            lblTimerValue = CreateStatusValue("0 秒", Color.White);
            table.Controls.Add(lblTimerValue, 1, 1);

            // 行2：温度漂移
            table.Controls.Add(CreateStatusLabel("温度漂移："), 0, 2);
            lblDriftValue = CreateStatusValue("-- °C/10min", Color.FromArgb(200, 200, 200));
            table.Controls.Add(lblDriftValue, 1, 2);

            // 行3：样品编号
            table.Controls.Add(CreateStatusLabel("样品编号："), 0, 3);
            lblProductValue = CreateStatusValue("--", Color.FromArgb(200, 200, 200));
            table.Controls.Add(lblProductValue, 1, 3);

            pnl.Controls.Add(table);
            pnl.Controls.Add(lblTitle);

            return pnl;
        }

        private Label CreateStatusLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(160, 160, 160),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };
        }

        private Label CreateStatusValue(string text, Color color)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = color,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };
        }

        /// <summary>
        /// 操作按钮面板
        /// </summary>
        private Panel BuildButtonsPanel()
        {
            var pnl = new Panel
            {
                Height = 200,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(10, 8, 10, 8)
            };

            var lblTitle = new Label
            {
                Text = "操作面板",
                Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 使用 FlowLayoutPanel 自动排列按钮，避免硬编码坐标
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(0, 8, 0, 0),
                Margin = new Padding(0)
            };

            int btnWidth = 88, btnHeight = 34;

            btnNewTest = CreateButton("新建试验", btnWidth, btnHeight);
            btnStartHeating = CreateButton("开始升温", btnWidth, btnHeight);
            btnStopHeating = CreateButton("停止升温", btnWidth, btnHeight);
            btnStartRecording = CreateButton("开始记录", btnWidth, btnHeight);
            btnStopRecording = CreateButton("停止记录", btnWidth, btnHeight);
            btnTestRecord = CreateButton("试验记录", btnWidth, btnHeight);
            btnParameterSettings = CreateButton("参数设置", btnWidth, btnHeight);

            // 绑定点击事件
            btnNewTest.Click += (s, e) => OnNewTest();
            btnStartHeating.Click += (s, e) => _coreService.StartHeating();
            btnStopHeating.Click += (s, e) => _coreService.StopHeating();
            btnStartRecording.Click += (s, e) => _coreService.StartRecording();
            btnStopRecording.Click += (s, e) => _coreService.StopRecording();
            btnTestRecord.Click += (s, e) => OnTestRecord();
            btnParameterSettings.Click += (s, e) => OnParameterSettings();

            flow.Controls.AddRange(new Control[] {
                btnNewTest, btnStartHeating, btnStopHeating,
                btnStartRecording, btnStopRecording, btnTestRecord,
                btnParameterSettings
            });

            pnl.Controls.Add(flow);
            pnl.Controls.Add(lblTitle);

            return pnl;
        }

        private Button CreateButton(string text, int width, int height)
        {
            return new Button
            {
                Text = text,
                Size = new Size(width, height),
                Margin = new Padding(3),
                Font = new Font("Microsoft YaHei", 9F),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Enabled = false
            };
        }

        /// <summary>
        /// 底部系统消息日志面板
        /// </summary>
        private Panel BuildMessageLogPanel()
        {
            var pnl = new Panel
            {
                Height = 180,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(8, 4, 8, 8)
            };

            var lblLogTitle = new Label
            {
                Text = "系统消息",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 22,
                TextAlign = ContentAlignment.MiddleLeft
            };

            rtbMessageLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0)
            };

            pnl.Controls.Add(rtbMessageLog);
            pnl.Controls.Add(lblLogTitle);

            return pnl;
        }

        #endregion

        #region OxyPlot 初始化

        private void InitializeOxyPlot()
        {
            plotModel = new PlotModel
            {
                Title = "温度曲线",
                TitleFontSize = 14,
                TitleColor = OxyColor.FromRgb(220, 220, 220),
                TextColor = OxyColor.FromRgb(200, 200, 200),
                PlotAreaBorderColor = OxyColor.FromRgb(100, 100, 100),
                Background = OxyColor.FromRgb(20, 20, 20)
            };

            // X轴：时间（秒），滚动显示最近10分钟
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "时间 (秒)",
                TitleColor = OxyColor.FromRgb(200, 200, 200),
                TextColor = OxyColor.FromRgb(180, 180, 180),
                AxislineColor = OxyColor.FromRgb(100, 100, 100),
                TicklineColor = OxyColor.FromRgb(100, 100, 100),
                Minimum = 0,
                Maximum = 600,
                IsZoomEnabled = false,
                IsPanEnabled = false
            });

            // Y轴：温度（°C），范围 0~800
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "温度 (°C)",
                TitleColor = OxyColor.FromRgb(200, 200, 200),
                TextColor = OxyColor.FromRgb(180, 180, 180),
                AxislineColor = OxyColor.FromRgb(100, 100, 100),
                TicklineColor = OxyColor.FromRgb(100, 100, 100),
                Minimum = 0,
                Maximum = 800,
                IsZoomEnabled = false,
                IsPanEnabled = false
            });

            // 4条温度曲线
            lineFurnace1 = new LineSeries
            {
                Title = "炉温1 (TF1)",
                Color = OxyColor.FromRgb(255, 80, 80),
                StrokeThickness = 1.5,
                MarkerType = MarkerType.None
            };

            lineFurnace2 = new LineSeries
            {
                Title = "炉温2 (TF2)",
                Color = OxyColor.FromRgb(255, 140, 60),
                StrokeThickness = 1.5,
                MarkerType = MarkerType.None
            };

            lineSurface = new LineSeries
            {
                Title = "表面温 (TS)",
                Color = OxyColor.FromRgb(80, 180, 255),
                StrokeThickness = 1.5,
                MarkerType = MarkerType.None
            };

            lineCenter = new LineSeries
            {
                Title = "中心温 (TC)",
                Color = OxyColor.FromRgb(80, 255, 120),
                StrokeThickness = 1.5,
                MarkerType = MarkerType.None
            };

            plotModel.Series.Add(lineFurnace1);
            plotModel.Series.Add(lineFurnace2);
            plotModel.Series.Add(lineSurface);
            plotModel.Series.Add(lineCenter);

            plotView.Model = plotModel;
        }

        #endregion

        #region 子Tab初始化

        private void InitializeCalibrationTab()
        {
            _calibrationTab = new CalibrationTab(_coreService, _dataService);
            _calibrationTab.Dock = DockStyle.Fill;
            tabCalibration.Controls.Add(_calibrationTab);
        }

        private void InitializeRecordQueryTab()
        {
            _recordQueryTab = new RecordQueryTab(_dataService);
            _recordQueryTab.Dock = DockStyle.Fill;
            tabRecordQuery.Controls.Add(_recordQueryTab);
        }

        private void InitializeComparisonTab()
        {
            _comparisonTab = new ComparisonTab(_dataService);
            _comparisonTab.Dock = DockStyle.Fill;
            tabComparison.Controls.Add(_comparisonTab);
        }

        #endregion

        #region 事件订阅

        private void SubscribeEvents()
        {
            _coreService.DataBroadcast += OnDataBroadcast!;
        }

        /// <summary>
        /// DataBroadcast事件回调（后台线程触发）
        /// 通过SafeInvoke安全地更新所有UI控件
        /// </summary>
        private void OnDataBroadcast(object sender, DataBroadcastEventArgs e)
        {
            this.SafeInvoke(() =>
            {
                // 1. 更新温度LED面板
                UpdateTemperatureDisplay(e.Temperature);

                // 2. 更新校准Tab的校准温显示
                _calibrationTab.UpdateCalibrationTemperature(e.Temperature.TempCalibration);

                // 3. 更新OxyPlot曲线
                UpdatePlotCurves(e.Temperature);

                // 4. 更新状态信息
                _currentState = e.CurrentState;
                lblStatusValue.Text = e.StateText;
                lblStatusValue.ForeColor = GetStateColor(e.CurrentState);
                lblTimerValue.Text = $"{e.RecordingSeconds} 秒";
                lblDriftValue.Text = $"{e.TemperatureDrift:F2} °C/10min";
                lblProductValue.Text = string.IsNullOrEmpty(e.ProductId) ? "--" : e.ProductId;

                // 5. 更新按钮状态
                _hasActiveTest = !string.IsNullOrEmpty(e.ProductId) && !string.IsNullOrEmpty(e.TestId);
                ApplyButtonStates();

                // 6. 追加系统消息
                foreach (var msg in e.Messages)
                {
                    var color = msg.IsWarning
                        ? Color.FromArgb(255, 220, 80)
                        : Color.FromArgb(220, 220, 220);

                    rtbMessageLog.SelectionStart = rtbMessageLog.TextLength;
                    rtbMessageLog.SelectionLength = 0;
                    rtbMessageLog.SelectionColor = color;
                    rtbMessageLog.AppendText($"{msg.Time}  {msg.Message}\n");
                    rtbMessageLog.SelectionColor = rtbMessageLog.ForeColor;
                    rtbMessageLog.ScrollToCaret();
                }
            });
        }

        private Color GetStateColor(TestState state)
        {
            return state switch
            {
                TestState.Idle => Color.FromArgb(150, 150, 150),
                TestState.Preparing => Color.FromArgb(255, 180, 60),
                TestState.Ready => Color.FromArgb(100, 200, 100),
                TestState.Recording => Color.FromArgb(80, 180, 255),
                TestState.Complete => Color.FromArgb(200, 100, 255),
                _ => Color.White
            };
        }

        #endregion

        #region 温度显示更新

        private void UpdateTemperatureDisplay(TemperatureData temp)
        {
            lblTF1Val.Text = $"{temp.TempFurnace1:F1} °C";
            lblTF2Val.Text = $"{temp.TempFurnace2:F1} °C";
            lblTSVal.Text = $"{temp.TempSurface:F1} °C";
            lblTCVal.Text = $"{temp.TempCenter:F1} °C";
            lblTCalVal.Text = $"{temp.TempCalibration:F1} °C";
        }

        private void UpdatePlotCurves(TemperatureData temp)
        {
            _dataPointIndex++;

            lineFurnace1.Points.Add(new DataPoint(_dataPointIndex, temp.TempFurnace1));
            lineFurnace2.Points.Add(new DataPoint(_dataPointIndex, temp.TempFurnace2));
            lineSurface.Points.Add(new DataPoint(_dataPointIndex, temp.TempSurface));
            lineCenter.Points.Add(new DataPoint(_dataPointIndex, temp.TempCenter));

            // 滚动X轴：显示最近600个数据点
            if (_dataPointIndex > 600)
            {
                double minX = _dataPointIndex - 600;
                plotModel.Axes[0].Minimum = minX;
                plotModel.Axes[0].Maximum = _dataPointIndex + 10;
            }

            const int maxPoints = 3600;
            TrimSeries(lineFurnace1, maxPoints);
            TrimSeries(lineFurnace2, maxPoints);
            TrimSeries(lineSurface, maxPoints);
            TrimSeries(lineCenter, maxPoints);

            plotModel.InvalidatePlot(true);
        }

        private void TrimSeries(LineSeries series, int maxPoints)
        {
            while (series.Points.Count > maxPoints)
                series.Points.RemoveAt(0);
        }

        #endregion

        #region 按钮状态控制

        private void ApplyButtonStates()
        {
            bool hasUnsaved = _coreService.HasUnsavedCompleteTest();

            switch (_currentState)
            {
                case TestState.Idle:
                    btnNewTest.Enabled = true;
                    btnStartHeating.Enabled = true;
                    btnStopHeating.Enabled = false;
                    btnStartRecording.Enabled = false;
                    btnStopRecording.Enabled = false;
                    btnTestRecord.Enabled = false;
                    btnParameterSettings.Enabled = true;
                    break;

                case TestState.Preparing:
                    btnNewTest.Enabled = !_hasActiveTest || !hasUnsaved;
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = true;
                    btnStartRecording.Enabled = false;
                    btnStopRecording.Enabled = false;
                    btnTestRecord.Enabled = false;
                    btnParameterSettings.Enabled = true;
                    break;

                case TestState.Ready:
                    btnNewTest.Enabled = false;
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = true;
                    btnStartRecording.Enabled = true;
                    btnStopRecording.Enabled = false;
                    btnTestRecord.Enabled = false;
                    btnParameterSettings.Enabled = true;
                    break;

                case TestState.Recording:
                    btnNewTest.Enabled = false;
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = false;
                    btnStartRecording.Enabled = false;
                    btnStopRecording.Enabled = true;
                    btnTestRecord.Enabled = false;
                    btnParameterSettings.Enabled = false;
                    break;

                case TestState.Complete:
                    btnNewTest.Enabled = !hasUnsaved;
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = true;
                    btnStartRecording.Enabled = false;
                    btnStopRecording.Enabled = false;
                    btnTestRecord.Enabled = true;
                    btnParameterSettings.Enabled = true;
                    break;
            }
        }

        #endregion

        #region 按钮事件处理

        private void OnNewTest()
        {
            if (_coreService.HasUnsavedCompleteTest())
            {
                MessageBox.Show("当前有未保存的试验记录，请先保存后再新建试验。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new NewTestDialog(_dataService);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                bool success = _coreService.CreateNewTest(dialog.TestInfo);
                if (!success)
                {
                    MessageBox.Show("创建试验失败，请检查输入信息。",
                        "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnTestRecord()
        {
            using var dialog = new TestRecordDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                bool success = _coreService.SaveTestRecord(dialog.Record);
                if (success)
                {
                    MessageBox.Show("试验记录保存成功！", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("保存试验记录失败，请重试。",
                        "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnParameterSettings()
        {
            using var dialog = new ParameterSettingsDialog();
            dialog.ShowDialog();
        }

        #endregion

        #region 导出功能

        public void ExportExcel(string productId, string testId)
        {
            try
            {
                string path = _dataService.ExportExcel(productId, testId);
                MessageBox.Show($"Excel报告已导出至：\n{path}", "导出成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出Excel失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ExportPdf(string productId, string testId)
        {
            try
            {
                string path = _dataService.ExportPdf(productId, testId);
                MessageBox.Show($"PDF报告已导出至：\n{path}", "导出成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出PDF失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_currentState == TestState.Recording)
            {
                var result = MessageBox.Show(
                    "试验正在进行中，确定要退出程序吗？",
                    "确认退出", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }

            _coreService.DataBroadcast -= OnDataBroadcast;
        }
    }
}
