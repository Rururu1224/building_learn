#nullable disable

using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using BuildingFireTest.Interfaces;

namespace BuildingFireTest.UI
{
    /// <summary>
    /// 设备校准Tab页面
    /// 使用 Dock 停靠布局，自适应窗口尺寸
    /// </summary>
    public partial class CalibrationTab : UserControl
    {
        private readonly ICoreService _coreService;
        private readonly IDataService _dataService;

        // ========== 上方：实时校准温度 ==========
        private Label lblCalTempValue;
        private Button btnRecordCalPoint;
        private TextBox txtStandardTemp;
        private Label lblLastRecorded;

        // ========== 下方：历史校准记录 ==========
        private DataGridView dgvCalibrationHistory;
        private Button btnRefreshHistory;

        public CalibrationTab(ICoreService coreService, IDataService dataService)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Padding = new Padding(10);

            // ========== 整体布局：上方校准区 + 下方历史表格 ==========
            // 使用 SplitContainer 让两部分可调整大小
            var splitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(30, 30, 30),
                SplitterDistance = 160,
                Panel1MinSize = 120,
                FixedPanel = FixedPanel.Panel1
            };

            // ========== 上方：实时校准区 ==========
            var pnlLive = splitter.Panel1;
            pnlLive.BackColor = Color.FromArgb(40, 40, 40);
            pnlLive.Padding = new Padding(15, 12, 15, 12);

            var lblLiveTitle = new Label
            {
                Text = "实时校准温度",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 校准温度显示行：使用 FlowLayoutPanel 避免重叠
            var flowCal = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 0),
                Margin = new Padding(0)
            };

            var lblCalTitle = new Label
            {
                Text = "校准通道 (TCal)：",
                Font = new Font("Microsoft YaHei", 10F),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Margin = new Padding(0, 8, 10, 0)
            };
            flowCal.Controls.Add(lblCalTitle);

            lblCalTempValue = new Label
            {
                Text = "0.0 °C",
                Font = new Font("Consolas", 26F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 180, 100),
                BackColor = Color.FromArgb(20, 20, 20),
                Size = new Size(200, 48),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 25, 0)
            };
            flowCal.Controls.Add(lblCalTempValue);

            var lblStdTemp = new Label
            {
                Text = "标准温度值：",
                Font = new Font("Microsoft YaHei", 10F),
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Margin = new Padding(0, 14, 8, 0)
            };
            flowCal.Controls.Add(lblStdTemp);

            txtStandardTemp = new TextBox
            {
                Text = "750.0",
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei", 10F),
                Margin = new Padding(0, 11, 10, 0)
            };
            flowCal.Controls.Add(txtStandardTemp);

            btnRecordCalPoint = new Button
            {
                Text = "记录校准点",
                Font = new Font("Microsoft YaHei", 10F),
                Size = new Size(110, 34),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 8, 0, 0)
            };
            btnRecordCalPoint.FlatAppearance.BorderSize = 0;
            btnRecordCalPoint.Click += BtnRecordCalPoint_Click!;
            flowCal.Controls.Add(btnRecordCalPoint);

            lblLastRecorded = new Label
            {
                Text = "尚未记录校准点",
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(140, 140, 140),
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 6, 0, 0)
            };

            // 注意添加顺序：Dock从底部开始排列
            pnlLive.Controls.Add(lblLastRecorded);
            pnlLive.Controls.Add(flowCal);
            pnlLive.Controls.Add(lblLiveTitle);

            // ========== 下方：历史校准记录 ==========
            var pnlHistory = splitter.Panel2;
            pnlHistory.BackColor = Color.FromArgb(40, 40, 40);
            pnlHistory.Padding = new Padding(15, 12, 15, 12);

            var pnlHistoryHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(0)
            };

            var lblHistoryTitle = new Label
            {
                Text = "历史校准记录",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnRefreshHistory = new Button
            {
                Text = "刷新",
                Font = new Font("Microsoft YaHei", 9F),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right
            };
            btnRefreshHistory.FlatAppearance.BorderSize = 0;
            btnRefreshHistory.Click += BtnRefreshHistory_Click!;

            pnlHistoryHeader.Controls.Add(lblHistoryTitle);
            pnlHistoryHeader.Controls.Add(btnRefreshHistory);

            dgvCalibrationHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(0, 122, 204),
                    SelectionForeColor = Color.White,
                    Font = new Font("Microsoft YaHei", 9F)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(70, 70, 70)
            };

            pnlHistory.Controls.Add(dgvCalibrationHistory);
            pnlHistory.Controls.Add(pnlHistoryHeader);

            // 添加 splitter 到 UserControl
            this.Controls.Add(splitter);
        }

        /// <summary>
        /// 更新校准温度显示（由MainForm的DataBroadcast事件驱动）
        /// </summary>
        public void UpdateCalibrationTemperature(double temp)
        {
            lblCalTempValue.SetTextSafe($"{temp:F1} °C");
        }

        private void BtnRecordCalPoint_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(txtStandardTemp.Text.Trim(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double standardTemp))
            {
                MessageBox.Show("请输入有效的标准温度值。", "输入错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _coreService.RecordCalibrationPoint(standardTemp);
                lblLastRecorded.SetTextSafe(
                    $"上次记录：{DateTime.Now:HH:mm:ss} | 标准温度 {standardTemp:F1}°C");
                RefreshHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"记录校准点失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefreshHistory_Click(object sender, EventArgs e)
        {
            RefreshHistory();
        }

        private void RefreshHistory()
        {
            try
            {
                DataTable dt = _dataService.GetCalibrationRecords();
                dgvCalibrationHistory.DataSource = dt;
            }
            catch
            {
                // 数据加载失败时静默处理
            }
        }
    }
}
