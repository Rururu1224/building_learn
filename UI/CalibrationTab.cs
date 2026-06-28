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
    /// 实时显示校准温、记录校准数据、查看历史校准记录
    /// </summary>
    public partial class CalibrationTab : UserControl
    {
        private readonly ICoreService _coreService;
        private readonly IDataService _dataService;

        // ========== 上方：实时校准温度 ==========
        private Label lblCalTempTitle;
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

            // ========== 顶部：实时校准区 ==========
            var pnlLive = new Panel
            {
                Location = new Point(15, 15),
                Size = new Size(1220, 130),
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblLiveTitle = new Label
            {
                Text = "实时校准温度",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            lblCalTempTitle = new Label
            {
                Text = "校准通道 (TCal)：",
                Font = new Font("Microsoft YaHei", 10F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(20, 55),
                AutoSize = true
            };

            lblCalTempValue = new Label
            {
                Text = "0.0 °C",
                Font = new Font("Consolas", 32F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 180, 100),
                BackColor = Color.FromArgb(20, 20, 20),
                Location = new Point(200, 42),
                Size = new Size(250, 55),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblStdTemp = new Label
            {
                Text = "标准温度值：",
                Font = new Font("Microsoft YaHei", 10F),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(500, 55),
                AutoSize = true
            };

            txtStandardTemp = new TextBox
            {
                Text = "750.0",
                Location = new Point(605, 52),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei", 10F)
            };

            btnRecordCalPoint = new Button
            {
                Text = "记录校准点",
                Font = new Font("Microsoft YaHei", 10F),
                Size = new Size(120, 35),
                Location = new Point(710, 46),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRecordCalPoint.FlatAppearance.BorderSize = 0;
            btnRecordCalPoint.Click += BtnRecordCalPoint_Click!;

            lblLastRecorded = new Label
            {
                Text = "尚未记录校准点",
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(140, 140, 140),
                Location = new Point(20, 100),
                AutoSize = true
            };

            pnlLive.Controls.AddRange(new Control[] {
                lblLiveTitle, lblCalTempTitle, lblCalTempValue,
                lblStdTemp, txtStandardTemp, btnRecordCalPoint, lblLastRecorded
            });

            // ========== 底部：历史校准记录 ==========
            var pnlHistory = new Panel
            {
                Location = new Point(15, 160),
                Size = new Size(1220, 560),
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblHistoryTitle = new Label
            {
                Text = "历史校准记录",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            btnRefreshHistory = new Button
            {
                Text = "刷新",
                Font = new Font("Microsoft YaHei", 9F),
                Size = new Size(80, 30),
                Location = new Point(1120, 10),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefreshHistory.FlatAppearance.BorderSize = 0;
            btnRefreshHistory.Click += BtnRefreshHistory_Click!;

            dgvCalibrationHistory = new DataGridView
            {
                Location = new Point(20, 55),
                Size = new Size(1180, 490),
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

            pnlHistory.Controls.AddRange(new Control[] {
                lblHistoryTitle, btnRefreshHistory, dgvCalibrationHistory
            });

            this.Controls.AddRange(new Control[] { pnlLive, pnlHistory });
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