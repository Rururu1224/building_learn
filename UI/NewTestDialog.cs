#nullable disable

using System;
using System.Drawing;
using System.Windows.Forms;
using BuildingFireTest.Interfaces;

namespace BuildingFireTest.UI
{
    /// <summary>
    /// 新建试验弹窗
    /// 使用 TableLayoutPanel 布局，避免文字堆叠，适应不同 DPI
    /// </summary>
    public partial class NewTestDialog : Form
    {
        private readonly IDataService _dataService;

        // ========== 控件 ==========
        private TextBox txtEnvTemp, txtEnvHumidity;
        private TextBox txtProductId, txtTestId, txtProductName, txtSpecification;
        private TextBox txtHeight, txtDiameter;
        private TextBox txtOperator, txtPreWeight;
        private TextBox txtDeviceId, txtDeviceName, txtCalibrationDate, txtConstPower;
        private RadioButton rbStandard, rbCustom;
        private NumericUpDown nudCustomMinutes;
        private Button btnCreate, btnCancel;
        private Label lblError;

        /// <summary>
        /// 收集的试验信息（DialogResult.OK时有效）
        /// </summary>
        public TestCreationInfo TestInfo { get; private set; } = new();

        public NewTestDialog(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            InitializeComponent();
            LoadDeviceInfo();
        }

        private void InitializeComponent()
        {
            this.Text = "新建试验";
            this.Size = new Size(540, 700);
            this.MinimumSize = new Size(480, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 45);
            this.Font = new Font("Microsoft YaHei", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;

            // ========== 主布局：使用单个 TableLayoutPanel ==========
            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.FromArgb(45, 45, 45),
                Padding = new Padding(20, 15, 20, 15),
                Margin = new Padding(0),
                AutoScroll = true
            };

            // 两列：标签列（固定宽）、输入列（自适应）
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;

            // ===== 环境信息标题 =====
            var lblSection1 = CreateSectionLabel("环境信息");
            mainTable.Controls.Add(lblSection1, 0, row);
            mainTable.SetColumnSpan(lblSection1, 2);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            // 环境温度
            AddFieldRow(mainTable, "环境温度 (°C)：", out txtEnvTemp, "25.0", row); row++;
            // 环境湿度
            AddFieldRow(mainTable, "环境湿度 (%)：", out txtEnvHumidity, "50.0", row); row++;

            // 间隔
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            var spacer1 = new Label { Height = 1, Margin = new Padding(0) };
            mainTable.Controls.Add(spacer1, 0, row);
            mainTable.SetColumnSpan(spacer1, 2);
            row++;

            // ===== 样品信息标题 =====
            var lblSection2 = CreateSectionLabel("样品信息");
            mainTable.Controls.Add(lblSection2, 0, row);
            mainTable.SetColumnSpan(lblSection2, 2);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            AddFieldRow(mainTable, "样品编号 *：", out txtProductId, "", row); row++;
            AddFieldRow(mainTable, "试验标识 *：", out txtTestId, "", row); row++;
            AddFieldRow(mainTable, "样品名称：", out txtProductName, "", row); row++;
            AddFieldRow(mainTable, "规格型号：", out txtSpecification, "", row); row++;
            AddFieldRow(mainTable, "高度 (mm)：", out txtHeight, "50.0", row); row++;
            AddFieldRow(mainTable, "直径 (mm)：", out txtDiameter, "45.0", row); row++;

            // 间隔
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            var spacer2 = new Label { Height = 1, Margin = new Padding(0) };
            mainTable.Controls.Add(spacer2, 0, row);
            mainTable.SetColumnSpan(spacer2, 2);
            row++;

            // ===== 试验参数标题 =====
            var lblSection3 = CreateSectionLabel("试验参数");
            mainTable.Controls.Add(lblSection3, 0, row);
            mainTable.SetColumnSpan(lblSection3, 2);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            AddFieldRow(mainTable, "操作员 *：", out txtOperator, "", row); row++;
            AddFieldRow(mainTable, "初始质量 (g) *：", out txtPreWeight, "", row); row++;

            // 时长模式行（特殊处理）
            var lblMode = new Label
            {
                Text = "时长模式：",
                ForeColor = Color.FromArgb(200, 200, 200),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 6, 8, 0)
            };
            mainTable.Controls.Add(lblMode, 0, row);

            var flowDuration = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 3, 0, 0),
                Padding = new Padding(0)
            };

            rbStandard = new RadioButton
            {
                Text = "标准60分钟",
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
                Checked = true,
                Margin = new Padding(0, 0, 12, 0)
            };

            rbCustom = new RadioButton
            {
                Text = "自定义",
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
                Margin = new Padding(0, 0, 8, 0)
            };

            nudCustomMinutes = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 600,
                Value = 30,
                Size = new Size(60, 25),
                Enabled = false,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 6, 0)
            };

            var lblMinutes = new Label
            {
                Text = "分钟",
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0)
            };

            rbCustom.CheckedChanged += (s, e) => nudCustomMinutes.Enabled = rbCustom.Checked;

            flowDuration.Controls.AddRange(new Control[] { rbStandard, rbCustom, nudCustomMinutes, lblMinutes });
            mainTable.Controls.Add(flowDuration, 1, row);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            row++;

            // 间隔
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            var spacer3 = new Label { Height = 1, Margin = new Padding(0) };
            mainTable.Controls.Add(spacer3, 0, row);
            mainTable.SetColumnSpan(spacer3, 2);
            row++;

            // ===== 设备信息标题 =====
            var lblSection4 = CreateSectionLabel("设备信息（自动带入）");
            mainTable.Controls.Add(lblSection4, 0, row);
            mainTable.SetColumnSpan(lblSection4, 2);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            row++;

            AddReadOnlyFieldRow(mainTable, "设备编号：", out txtDeviceId, row); row++;
            AddReadOnlyFieldRow(mainTable, "设备名称：", out txtDeviceName, row); row++;
            AddReadOnlyFieldRow(mainTable, "检定日期：", out txtCalibrationDate, row); row++;
            AddReadOnlyFieldRow(mainTable, "恒功率值：", out txtConstPower, row); row++;

            // 间隔
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
            var spacer4 = new Label { Height = 1, Margin = new Padding(0) };
            mainTable.Controls.Add(spacer4, 0, row);
            mainTable.SetColumnSpan(spacer4, 2);
            row++;

            // ===== 错误提示 =====
            lblError = new Label
            {
                ForeColor = Color.FromArgb(255, 100, 100),
                AutoSize = true,
                Visible = false,
                Margin = new Padding(0, 4, 0, 4)
            };
            mainTable.Controls.Add(lblError, 0, row);
            mainTable.SetColumnSpan(lblError, 2);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            row++;

            // ===== 按钮行 =====
            var btnFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0),
                Padding = new Padding(0)
            };

            btnCreate = new Button
            {
                Text = "创建试验",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                Size = new Size(120, 36),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 12, 0)
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += BtnCreate_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Font = new Font("Microsoft YaHei", 10F),
                Size = new Size(90, 36),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            btnFlow.Controls.AddRange(new Control[] { btnCreate, btnCancel });
            mainTable.Controls.Add(btnFlow, 0, row);
            mainTable.SetColumnSpan(btnFlow, 2);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            row++;

            this.Controls.Add(mainTable);
        }

        private Label CreateSectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 220),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 4, 0, 0)
            };
        }

        private void AddFieldRow(TableLayoutPanel table, string labelText,
            out TextBox textBox, string defaultValue, int row)
        {
            var lbl = new Label
            {
                Text = labelText,
                ForeColor = Color.FromArgb(200, 200, 200),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 6, 8, 0)
            };
            table.Controls.Add(lbl, 0, row);

            textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = defaultValue,
                Margin = new Padding(0, 3, 0, 0)
            };
            table.Controls.Add(textBox, 1, row);
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
        }

        private void AddReadOnlyFieldRow(TableLayoutPanel table, string labelText,
            out TextBox textBox, int row)
        {
            AddFieldRow(table, labelText, out textBox, "", row);
            textBox.ReadOnly = true;
            textBox.BackColor = Color.FromArgb(50, 50, 50);
            textBox.ForeColor = Color.FromArgb(160, 160, 160);
        }

        private void LoadDeviceInfo()
        {
            try
            {
                var device = _dataService.GetDeviceInfo();
                txtDeviceId.Text = device.DeviceId;
                txtDeviceName.Text = device.DeviceName;
                txtCalibrationDate.Text = device.CalibrationDate.ToString("yyyy-MM-dd");
                txtConstPower.Text = device.ConstPower.ToString("F0");
            }
            catch
            {
                txtDeviceId.Text = "DEV-001";
                txtDeviceName.Text = "不燃性试验炉";
                txtCalibrationDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
                txtConstPower.Text = "2048";
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            // ========== 输入校验 ==========
            if (string.IsNullOrWhiteSpace(txtProductId.Text))
            {
                ShowError("请输入样品编号");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtTestId.Text))
            {
                ShowError("请输入试验标识");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtOperator.Text))
            {
                ShowError("请输入操作员");
                return;
            }

            if (!TryParseDouble(txtEnvTemp.Text, out double envTemp))
            {
                ShowError("环境温度请输入有效数字");
                return;
            }
            if (!TryParseDouble(txtEnvHumidity.Text, out double envHumidity))
            {
                ShowError("环境湿度请输入有效数字");
                return;
            }
            if (!TryParseDouble(txtHeight.Text, out double height) || height <= 0)
            {
                ShowError("样品高度请输入有效正数");
                return;
            }
            if (!TryParseDouble(txtDiameter.Text, out double diameter) || diameter <= 0)
            {
                ShowError("样品直径请输入有效正数");
                return;
            }
            if (!TryParseDouble(txtPreWeight.Text, out double preWeight) || preWeight <= 0)
            {
                ShowError("初始质量请输入有效正数");
                return;
            }

            // ========== 构建试验信息 ==========
            TestInfo = new TestCreationInfo
            {
                EnvironmentTemp = envTemp,
                EnvironmentHumidity = envHumidity,
                ProductId = txtProductId.Text.Trim(),
                TestId = txtTestId.Text.Trim(),
                ProductName = txtProductName.Text.Trim(),
                Specification = txtSpecification.Text.Trim(),
                Height = height,
                Diameter = diameter,
                Operator = txtOperator.Text.Trim(),
                PreWeight = preWeight,
                IsStandardDuration = rbStandard.Checked,
                CustomDurationMinutes = rbCustom.Checked ? (int)nudCustomMinutes.Value : 0
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(text.Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}
