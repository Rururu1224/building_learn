#nullable disable

using System;
using System.Drawing;
using System.Windows.Forms;
using BuildingFireTest.Interfaces;

namespace BuildingFireTest.UI
{
    /// <summary>
    /// 新建试验弹窗
    /// 填写样品信息、试验参数、初始质量，设备信息自动带入
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
            this.Size = new Size(520, 580);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 45);
            this.Font = new Font("Microsoft YaHei", 9F);

            int leftLabel = 25, leftInput = 140, inputWidth = 160;
            int y = 15, rowHeight = 35;

            // ========== 环境信息 ==========
            var lblSection1 = CreateSectionLabel("环境信息", leftLabel, y); y += 28;
            CreateField("环境温度 (°C)：", leftLabel, y, out txtEnvTemp, leftInput, inputWidth, "25.0"); y += rowHeight;
            CreateField("环境湿度 (%)：", leftLabel, y, out txtEnvHumidity, leftInput, inputWidth, "50.0"); y += rowHeight + 5;

            // ========== 样品信息 ==========
            var lblSection2 = CreateSectionLabel("样品信息", leftLabel, y); y += 28;
            CreateField("样品编号：", leftLabel, y, out txtProductId, leftInput, inputWidth); y += rowHeight;
            CreateField("试验标识：", leftLabel, y, out txtTestId, leftInput, inputWidth); y += rowHeight;
            CreateField("样品名称：", leftLabel, y, out txtProductName, leftInput, inputWidth); y += rowHeight;
            CreateField("规格型号：", leftLabel, y, out txtSpecification, leftInput, inputWidth); y += rowHeight;
            CreateField("高度 (mm)：", leftLabel, y, out txtHeight, leftInput, inputWidth, "50.0"); y += rowHeight;
            CreateField("直径 (mm)：", leftLabel, y, out txtDiameter, leftInput, inputWidth, "45.0"); y += rowHeight + 5;

            // ========== 试验参数 ==========
            var lblSection3 = CreateSectionLabel("试验参数", leftLabel, y); y += 28;
            CreateField("操作员：", leftLabel, y, out txtOperator, leftInput, inputWidth); y += rowHeight;
            CreateField("初始质量 (g)：", leftLabel, y, out txtPreWeight, leftInput, inputWidth); y += rowHeight;

            // 试验时长模式
            var lblMode = new Label
            {
                Text = "时长模式：",
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(leftLabel, y),
                AutoSize = true
            };

            rbStandard = new RadioButton
            {
                Text = "标准60分钟",
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(leftInput, y),
                AutoSize = true,
                Checked = true
            };

            rbCustom = new RadioButton
            {
                Text = "自定义",
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(leftInput + 125, y),
                AutoSize = true
            };

            nudCustomMinutes = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 600,
                Value = 30,
                Location = new Point(leftInput + 205, y - 2),
                Size = new Size(55, 23),
                Enabled = false,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };

            var lblMinutes = new Label
            {
                Text = "分钟",
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(leftInput + 265, y),
                AutoSize = true
            };

            rbCustom.CheckedChanged += (s, e) => nudCustomMinutes.Enabled = rbCustom.Checked;
            y += rowHeight + 5;

            // ========== 设备信息（自动带入，只读） ==========
            var lblSection4 = CreateSectionLabel("设备信息（自动带入）", leftLabel, y); y += 28;
            CreateReadOnlyField("设备编号：", leftLabel, y, out txtDeviceId, leftInput, inputWidth); y += rowHeight;
            CreateReadOnlyField("设备名称：", leftLabel, y, out txtDeviceName, leftInput, inputWidth); y += rowHeight;
            CreateReadOnlyField("检定日期：", leftLabel, y, out txtCalibrationDate, leftInput, inputWidth); y += rowHeight;
            CreateReadOnlyField("恒功率值：", leftLabel, y, out txtConstPower, leftInput, inputWidth); y += rowHeight + 10;

            // ========== 错误提示 ==========
            lblError = new Label
            {
                ForeColor = Color.FromArgb(255, 100, 100),
                Location = new Point(leftLabel, y),
                AutoSize = true,
                Visible = false
            };
            y += 25;

            // ========== 按钮 ==========
            btnCreate = new Button
            {
                Text = "创建试验",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                Size = new Size(110, 35),
                Location = new Point(140, y),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += BtnCreate_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Font = new Font("Microsoft YaHei", 10F),
                Size = new Size(80, 35),
                Location = new Point(270, y),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] {
                lblSection1, txtEnvTemp, txtEnvHumidity,
                lblSection2, txtProductId, txtTestId, txtProductName, txtSpecification, txtHeight, txtDiameter,
                lblSection3, txtOperator, txtPreWeight, lblMode, rbStandard, rbCustom, nudCustomMinutes, lblMinutes,
                lblSection4, txtDeviceId, txtDeviceName, txtCalibrationDate, txtConstPower,
                lblError, btnCreate, btnCancel
            });
        }

        private Label CreateSectionLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 220),
                Location = new Point(x, y),
                AutoSize = true
            };
        }

        private void CreateField(string labelText, int x, int y,
            out TextBox textBox, int inputX, int inputWidth, string defaultValue = "")
        {
            var lbl = new Label
            {
                Text = labelText,
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(x, y + 3),
                AutoSize = true
            };

            textBox = new TextBox
            {
                Location = new Point(inputX, y),
                Size = new Size(inputWidth, 23),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = defaultValue
            };

            this.Controls.Add(lbl);
        }

        private void CreateReadOnlyField(string labelText, int x, int y,
            out TextBox textBox, int inputX, int inputWidth)
        {
            CreateField(labelText, x, y, out textBox, inputX, inputWidth);
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
                // 设备信息加载失败时使用默认值（不影响其他功能）
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

            // 数值校验
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