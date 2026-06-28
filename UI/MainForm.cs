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
    /// 温度LED面板、OxyPlot曲线、状态标签、系统消息日志、按钮状态联动
    /// 所有业务调用通过ICoreService/IDataService接口转发
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
        private Panel pnlTempLeds;
        private Label lblTF1, lblTF2, lblTS, lblTC, lblTCal;
        private Label lblTF1Val, lblTF2Val, lblTSVal, lblTCVal, lblTCalVal;

        // ========== 状态信息栏 ==========
        private Label lblStatusTitle, lblStatusValue;
        private Label lblTimerTitle, lblTimerValue;
        private Label lblDriftTitle, lblDriftValue;
        private Label lblProductTitle, lblProductValue;

        // ========== 系统消息日志 ==========
        private RichTextBox rtbMessageLog;

        // ========== 按钮区域 ==========
        private Panel pnlButtons;
        private Button btnNewTest;
        private Button btnStartHeating;
        private Button btnStopHeating;
        private Button btnStartRecording;
        private Button btnStopRecording;
        private Button btnTestRecord;

        // ========== TabControl ==========
        private TabControl tabControl;
        private TabPage tabMain;
        private TabPage tabCalibration;
        private TabPage tabRecordQuery;

        // ========== 子Tab控件 ==========
        private CalibrationTab _calibrationTab;
        private RecordQueryTab _recordQueryTab;

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
            SubscribeEvents();
            ApplyButtonStates();
        }

        private void InitializeComponent()
        {
            // ========== 窗体设置 ==========
            this.Text = "ISO 11820 建筑材料不燃性试验系统";
            this.Size = new Size(1280, 820);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1024, 700);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Font = new Font("Microsoft YaHei", 9F);

            // ========== TabControl ==========
            tabControl = new TabControl
            {
                Location = new Point(0, 0),
                Size = new Size(1270, 780),
                Font = new Font("Microsoft YaHei", 10F),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            tabMain = new TabPage("试验控制");
            tabCalibration = new TabPage("设备校准");
            tabRecordQuery = new TabPage("记录查询");

            tabControl.TabPages.AddRange(new[] { tabMain, tabCalibration, tabRecordQuery });

            // ========== 主Tab页面布局 ==========
            BuildMainTabPage();

            this.Controls.Add(tabControl);
            this.FormClosing += MainForm_FormClosing!;
        }

        #region 主Tab页面构建

        private void BuildMainTabPage()
        {
            tabMain.BackColor = Color.FromArgb(30, 30, 30);

            // --- 顶部：温度LED面板 ---
            BuildTemperaturePanel();

            // --- 左侧中间：OxyPlot曲线图 ---
            BuildPlotView();

            // --- 右侧面板：状态信息 + 按钮 ---
            BuildRightPanel();

            // --- 底部：系统消息日志 ---
            BuildMessageLog();

            // 添加到tabMain
            tabMain.Controls.AddRange(new Control[] {
                pnlTempLeds, plotView, pnlButtons, rtbMessageLog
            });

            // 右侧状态面板
            var pnlStatus = BuildStatusPanel();
            tabMain.Controls.Add(pnlStatus);
        }

        private void BuildTemperaturePanel()
        {
            pnlTempLeds = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(1240, 90),
                BackColor = Color.FromArgb(20, 20, 20),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 5个温度通道的LED显示
            var channels = new[]
            {
                ("炉温1 (TF1)", "0.0 °C", Color.FromArgb(255, 80, 80)),
                ("炉温2 (TF2)", "0.0 °C", Color.FromArgb(255, 140, 60)),
                ("表面温 (TS)", "0.0 °C", Color.FromArgb(80, 180, 255)),
                ("中心温 (TC)", "0.0 °C", Color.FromArgb(80, 255, 120)),
                ("校准温 (TCal)", "0.0 °C", Color.FromArgb(200, 180, 100))
            };

            var ledLabels = new List<Label>();
            var ledValues = new List<Label>();

            for (int i = 0; i < 5; i++)
            {
                int x = 15 + i * 248;

                var lblTitle = new Label
                {
                    Text = channels[i].Item1,
                    Font = new Font("Consolas", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(180, 180, 180),
                    Location = new Point(x, 8),
                    AutoSize = true
                };

                var lblValue = new Label
                {
                    Text = channels[i].Item2,
                    Font = new Font("Consolas", 28F, FontStyle.Bold),
                    ForeColor = channels[i].Item3,
                    BackColor = Color.FromArgb(10, 10, 10),
                    Location = new Point(x, 32),
                    Size = new Size(220, 48),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BorderStyle = BorderStyle.FixedSingle
                };

                ledLabels.Add(lblTitle);
                ledValues.Add(lblValue);
            }

            pnlTempLeds.Controls.AddRange(ledLabels.ToArray());
            pnlTempLeds.Controls.AddRange(ledValues.ToArray());

            lblTF1 = ledLabels[0]; lblTF1Val = ledValues[0];
            lblTF2 = ledLabels[1]; lblTF2Val = ledValues[1];
            lblTS = ledLabels[2]; lblTSVal = ledValues[2];
            lblTC = ledLabels[3]; lblTCVal = ledValues[3];
            lblTCal = ledLabels[4]; lblTCalVal = ledValues[4];
        }

        private void BuildPlotView()
        {
            plotView = new PlotView
            {
                Location = new Point(10, 110),
                Size = new Size(900, 420),
                BackColor = Color.FromArgb(20, 20, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
        }

        private Panel BuildStatusPanel()
        {
            var pnl = new Panel
            {
                Location = new Point(920, 110),
                Size = new Size(330, 300),
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = "试验状态",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                AutoSize = true
            };

            // 状态信息
            int y = 50;
            lblStatusTitle = CreateInfoLabel("当前状态：", 15, y);
            lblStatusValue = CreateInfoValue("空闲", 110, y, Color.FromArgb(100, 200, 100));

            y += 35;
            lblTimerTitle = CreateInfoLabel("记录计时：", 15, y);
            lblTimerValue = CreateInfoValue("0 秒", 110, y, Color.White);

            y += 35;
            lblDriftTitle = CreateInfoLabel("温度漂移：", 15, y);
            lblDriftValue = CreateInfoValue("-- °C/10min", 110, y, Color.FromArgb(200, 200, 200));

            y += 35;
            lblProductTitle = CreateInfoLabel("样品编号：", 15, y);
            lblProductValue = CreateInfoValue("--", 110, y, Color.FromArgb(200, 200, 200));

            pnl.Controls.AddRange(new Control[] {
                lblTitle, lblStatusTitle, lblStatusValue,
                lblTimerTitle, lblTimerValue,
                lblDriftTitle, lblDriftValue,
                lblProductTitle, lblProductValue
            });

            return pnl;
        }

        private Label CreateInfoLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(160, 160, 160),
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private Label CreateInfoValue(string text, int x, int y, Color color)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = color,
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private void BuildRightPanel()
        {
            pnlButtons = new Panel
            {
                Location = new Point(920, 420),
                Size = new Size(330, 110),
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };

            var pnlBtnLabel = new Label
            {
                Text = "操作面板",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 5),
                AutoSize = true
            };

            // 第一行按钮
            int btnWidth = 95, btnHeight = 32;
            int startX = 15, startY = 35;

            btnNewTest = CreateButton("新建试验", startX, startY, btnWidth, btnHeight);
            btnStartHeating = CreateButton("开始升温", startX + btnWidth + 5, startY, btnWidth, btnHeight);
            btnStopHeating = CreateButton("停止升温", startX + (btnWidth + 5) * 2, startY, btnWidth, btnHeight);

            // 第二行按钮
            startY += btnHeight + 5;
            btnStartRecording = CreateButton("开始记录", startX, startY, btnWidth, btnHeight);
            btnStopRecording = CreateButton("停止记录", startX + btnWidth + 5, startY, btnWidth, btnHeight);
            btnTestRecord = CreateButton("试验记录", startX + (btnWidth + 5) * 2, startY, btnWidth, btnHeight);

            // 绑定点击事件 → 转发到核心层
            btnNewTest.Click += (s, e) => OnNewTest();
            btnStartHeating.Click += (s, e) => _coreService.StartHeating();
            btnStopHeating.Click += (s, e) => _coreService.StopHeating();
            btnStartRecording.Click += (s, e) => _coreService.StartRecording();
            btnStopRecording.Click += (s, e) => _coreService.StopRecording();
            btnTestRecord.Click += (s, e) => OnTestRecord();

            pnlButtons.Controls.AddRange(new Control[] {
                pnlBtnLabel, btnNewTest, btnStartHeating, btnStopHeating,
                btnStartRecording, btnStopRecording, btnTestRecord
            });
        }

        private Button CreateButton(string text, int x, int y, int width, int height)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font("Microsoft YaHei", 9F),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Enabled = false
            };
        }

        private void BuildMessageLog()
        {
            var lblLogTitle = new Label
            {
                Text = "系统消息",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(10, 545),
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            rtbMessageLog = new RichTextBox
            {
                Location = new Point(10, 570),
                Size = new Size(1240, 170),
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.White,
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            tabMain.Controls.Add(lblLogTitle);
            tabMain.Controls.Add(rtbMessageLog);
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

            // 图例配置（注释掉避免编译错误）
            /*
            plotModel.Legend.Background = OxyColor.FromRgb(30, 30, 30);
            plotModel.Legend.TextColor = OxyColor.FromRgb(200, 200, 200);
            plotModel.Legend.Border = OxyColor.FromRgb(80, 80, 80);
            plotModel.Legend.Position = OxyPlot.LegendPosition.RightTop;
            */

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
                Maximum = 600,  // 10分钟 = 600秒
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

        #endregion

        #region 事件订阅

        private void SubscribeEvents()
        {
            // 订阅B层DataBroadcast事件 → 后台线程触发，需Invoke
            _coreService.DataBroadcast += OnDataBroadcast!;
        }

        /// <summary>
        /// DataBroadcast事件回调（后台线程触发）
        /// 通过Invoke安全地更新所有UI控件
        /// </summary>
        private void OnDataBroadcast(object sender, DataBroadcastEventArgs e)
        {
            this.SafeInvoke(() =>
            {
                // 1. 更新温度LED面板（已处于UI线程，直接赋值）
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

                // 6. 追加系统消息（区分普通/黄色提示）
                foreach (var msg in e.Messages)
                {
                    var color = msg.IsWarning
                        ? Color.FromArgb(255, 220, 80)   // 黄色提示
                        : Color.FromArgb(220, 220, 220);  // 普通白色

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

            // 添加数据点
            lineFurnace1.Points.Add(new DataPoint(_dataPointIndex, temp.TempFurnace1));
            lineFurnace2.Points.Add(new DataPoint(_dataPointIndex, temp.TempFurnace2));
            lineSurface.Points.Add(new DataPoint(_dataPointIndex, temp.TempSurface));
            lineCenter.Points.Add(new DataPoint(_dataPointIndex, temp.TempCenter));

            // 滚动X轴：显示最近600个数据点（约10分钟 @ 1秒/点）
            if (_dataPointIndex > 600)
            {
                double minX = _dataPointIndex - 600;
                plotModel.Axes[0].Minimum = minX;
                plotModel.Axes[0].Maximum = _dataPointIndex + 10;
            }

            // 限制每条曲线最多保留3600个点（60分钟）
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

        /// <summary>
        /// 五状态按钮权限控制
        /// Idle/Preparing/Ready/Recording/Complete
        /// </summary>
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
                    break;

                case TestState.Preparing:
                    // 有活动试验时禁止新建；无活动试验或上次已保存则允许
                    btnNewTest.Enabled = !_hasActiveTest || !hasUnsaved;
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = true;
                    btnStartRecording.Enabled = false;
                    btnStopRecording.Enabled = false;
                    btnTestRecord.Enabled = false;
                    break;

                case TestState.Ready:
                    btnNewTest.Enabled = false;
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = true;
                    btnStartRecording.Enabled = true;
                    btnStopRecording.Enabled = false;
                    btnTestRecord.Enabled = false;
                    break;

                case TestState.Recording:
                    btnNewTest.Enabled = false;
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = false;
                    btnStartRecording.Enabled = false;
                    btnStopRecording.Enabled = true;
                    btnTestRecord.Enabled = false;
                    break;

                case TestState.Complete:
                    btnNewTest.Enabled = !hasUnsaved;  // 未保存时禁止新建
                    btnStartHeating.Enabled = false;
                    btnStopHeating.Enabled = true;
                    btnStartRecording.Enabled = false;
                    btnStopRecording.Enabled = false;
                    btnTestRecord.Enabled = true;  // 允许保存试验记录
                    break;
            }
        }

        #endregion

        #region 按钮事件处理

        private void OnNewTest()
        {
            // 检查是否有未保存的试验
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

        #endregion

        #region 导出功能（菜单/快捷键可触发）

        /// <summary>
        /// 导出Excel报告（供外部菜单/工具栏调用）
        /// </summary>
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

        /// <summary>
        /// 导出PDF报告（供外部菜单/工具栏调用）
        /// </summary>
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
            // 如果正在记录中，提示用户
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

            // 取消事件订阅
            _coreService.DataBroadcast -= OnDataBroadcast;
        }
    }
}