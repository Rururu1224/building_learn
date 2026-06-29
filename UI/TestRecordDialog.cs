#nullable disable

using System;
using System.Drawing;
using System.Windows.Forms;
using BuildingFireTest.Interfaces;

namespace BuildingFireTest.UI
{
    /// <summary>
    /// 试验现象记录弹窗
    /// 填写火焰信息、试验后质量、备注
    /// </summary>
    public partial class TestRecordDialog : Form
    {
        // ========== 控件 ==========
        private CheckBox chkHasFlame;
        private Label lblFlameStart, lblFlameDuration;
        private NumericUpDown nudFlameStart, nudFlameDuration;
        private TextBox txtPostWeight;
        private TextBox txtRemark;
        private Button btnSave, btnCancel;
        private Label lblError;

        /// <summary>
        /// 收集的试验现象记录（DialogResult.OK时有效）
        /// </summary>
        public TestPhenomenonRecord Record { get; private set; } = new();

        public TestRecordDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "试验现象记录";
            this.Size = new Size(440, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 45);
            this.Font = new Font("Microsoft YaHei", 9F);

            int leftLabel = 25, leftInput = 155, inputWidth = 180;
            int y = 15, rowHeight = 35;

            // ========== 标题 ==========
            var lblTitle = new Label
            {
                Text = "请填写试验现象记录",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(leftLabel, y),
                AutoSize = true
            };
            y += 40;

            // ========== 火焰信息 ==========
            chkHasFlame = new CheckBox
            {
                Text = "是否出现持续火焰",
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(leftLabel, y),
                AutoSize = true,
                Checked = false
            };

            lblFlameStart = new Label
            {
                Text = "火焰发生时刻 (秒)：",
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(leftLabel + 20, y + 28),
                AutoSize = true,
                Enabled = false
            };

            nudFlameStart = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 3600,
                Location = new Point(leftInput + 10, y + 25),
                Size = new Size(80, 23),
                Enabled = false,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };

            lblFlameDuration = new Label
            {
                Text = "火焰持续时间 (秒)：",
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(leftLabel + 20, y + 56),
                AutoSize = true,
                Enabled = false
            };

            nudFlameDuration = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 3600,
                Location = new Point(leftInput + 10, y + 53),
                Size = new Size(80, 23),
                Enabled = false,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };

            chkHasFlame.CheckedChanged += (s, e) =>
            {
                bool hasFlame = chkHasFlame.Checked;
                lblFlameStart.Enabled = hasFlame;
                nudFlameStart.Enabled = hasFlame;
                lblFlameDuration.Enabled = hasFlame;
                nudFlameDuration.Enabled = hasFlame;
            };

            y += 85;

            // ========== 试验后质量 ==========
            var lblPostWeight = new Label
            {
                Text = "试验后质量 (g)：",
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(leftLabel, y + 3),
                AutoSize = true
            };

            var lblRequired = new Label
            {
                Text = "*必填",
                ForeColor = Color.FromArgb(255, 100, 100),
                Location = new Point(leftLabel, y + 20),
                AutoSize = true,
                Font = new Font("Microsoft YaHei", 7F)
            };

            txtPostWeight = new TextBox
            {
                Location = new Point(leftInput, y),
                Size = new Size(inputWidth, 23),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            y += rowHeight + 5;

            // ========== 备注 ==========
            var lblRemark = new Label
            {
                Text = "备注：",
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(leftLabel, y),
                AutoSize = true
            };
            y += 22;

            txtRemark = new TextBox
            {
                Location = new Point(leftLabel, y),
                Size = new Size(380, 60),
                Multiline = true,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };
            y += 70;

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
            btnSave = new Button
            {
                Text = "保存记录",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                Size = new Size(110, 35),
                Location = new Point(120, y),
                BackColor = Color.FromArgb(0, 150, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click!;

            btnCancel = new Button
            {
                Text = "取消",
                Font = new Font("Microsoft YaHei", 10F),
                Size = new Size(80, 35),
                Location = new Point(245, y),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] {
                lblTitle, chkHasFlame, lblFlameStart, nudFlameStart,
                lblFlameDuration, nudFlameDuration,
                lblPostWeight, lblRequired, txtPostWeight,
                lblRemark, txtRemark,
                lblError, btnSave, btnCancel
            });
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 试验后质量必填
            if (string.IsNullOrWhiteSpace(txtPostWeight.Text))
            {
                ShowError("请输入试验后质量");
                return;
            }

            if (!double.TryParse(txtPostWeight.Text.Trim(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out double postWeight) || postWeight <= 0)
            {
                ShowError("试验后质量请输入有效正数");
                return;
            }

            Record = new TestPhenomenonRecord
            {
                HasFlame = chkHasFlame.Checked,
                FlameStartTime = chkHasFlame.Checked ? (int)nudFlameStart.Value : 0,
                FlameDuration = chkHasFlame.Checked ? (int)nudFlameDuration.Value : 0,
                PostWeight = postWeight,
                Remark = txtRemark.Text.Trim()
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }
    }
}