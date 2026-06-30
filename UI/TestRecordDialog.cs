#nullable disable

using System;
using System.Drawing;
using System.Windows.Forms;
using BuildingFireTest.Interfaces;

namespace BuildingFireTest.UI
{
    /// <summary>
    /// 试验现象记录弹窗
    /// 使用 TableLayoutPanel 布局，避免文字堆叠
    /// </summary>
    public partial class TestRecordDialog : Form
    {
        // ========== 控件 ==========
        private CheckBox chkHasFlame;
        private NumericUpDown nudFlameStart, nudFlameDuration;
        private TextBox txtPostWeight;
        private TextBox txtRemark;
        private Button btnSave, btnCancel;
        private Label lblError;
        private Label lblFlameStart, lblFlameDuration;

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
            this.Size = new Size(460, 460);
            this.MinimumSize = new Size(400, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 45);
            this.Font = new Font("Microsoft YaHei", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;

            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.FromArgb(45, 45, 45),
                Padding = new Padding(22, 15, 22, 15),
                Margin = new Padding(0),
                AutoScroll = true
            };

            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            int row = 0;

            // ===== 标题 =====
            var lblTitle = new Label
            {
                Text = "请填写试验现象记录",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 0, 10)
            };
            mainTable.Controls.Add(lblTitle, 0, row);
            mainTable.SetColumnSpan(lblTitle, 2);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            row++;

            // ===== 火焰复选框 =====
            chkHasFlame = new CheckBox
            {
                Text = "是否出现持续火焰",
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 6)
            };
            mainTable.Controls.Add(chkHasFlame, 0, row);
            mainTable.SetColumnSpan(chkHasFlame, 2);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            row++;

            // ===== 火焰发生时刻 =====
            lblFlameStart = new Label
            {
                Text = "火焰发生时刻 (秒)：",
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Enabled = false,
                Margin = new Padding(0, 6, 8, 0)
            };
            mainTable.Controls.Add(lblFlameStart, 0, row);

            nudFlameStart = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 3600,
                Size = new Size(100, 25),
                Enabled = false,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Margin = new Padding(0, 3, 0, 0)
            };
            mainTable.Controls.Add(nudFlameStart, 1, row);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
            row++;

            // ===== 火焰持续时间 =====
            lblFlameDuration = new Label
            {
                Text = "火焰持续时间 (秒)：",
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Enabled = false,
                Margin = new Padding(0, 6, 8, 0)
            };
            mainTable.Controls.Add(lblFlameDuration, 0, row);

            nudFlameDuration = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 3600,
                Size = new Size(100, 25),
                Enabled = false,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Margin = new Padding(0, 3, 0, 0)
            };
            mainTable.Controls.Add(nudFlameDuration, 1, row);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
            row++;

            // 启用/禁用联动
            chkHasFlame.CheckedChanged += (s, e) =>
            {
                bool hasFlame = chkHasFlame.Checked;
                lblFlameStart.Enabled = hasFlame;
                nudFlameStart.Enabled = hasFlame;
                lblFlameDuration.Enabled = hasFlame;
                nudFlameDuration.Enabled = hasFlame;
            };

            // 间隔
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            var spacer1 = new Label { Height = 1, Margin = new Padding(0) };
            mainTable.Controls.Add(spacer1, 0, row);
            mainTable.SetColumnSpan(spacer1, 2);
            row++;

            // ===== 试验后质量 =====
            var pnlPostWeight = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };

            // 标签行：试验后质量 + *必填（红色）
            var flowPostWeight = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            var lblRequired = new Label
            {
                Text = "*必填",
                ForeColor = Color.FromArgb(255, 100, 100),
                Font = new Font("Microsoft YaHei", 9F),
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0)
            };

            var lblPostWeight = new Label
            {
                Text = "试验后质量 (g)：",
                ForeColor = Color.FromArgb(200, 200, 200),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 6, 0, 0)
            };

            flowPostWeight.Controls.Add(lblRequired);
            flowPostWeight.Controls.Add(lblPostWeight);
            pnlPostWeight.Controls.Add(flowPostWeight);
            mainTable.Controls.Add(pnlPostWeight, 0, row);

            txtPostWeight = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 3, 0, 0)
            };
            mainTable.Controls.Add(txtPostWeight, 1, row);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            row++;

            // ===== 备注 =====
            var lblRemark = new Label
            {
                Text = "备注：",
                ForeColor = Color.FromArgb(200, 200, 200),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopRight,
                Margin = new Padding(0, 8, 8, 0)
            };
            mainTable.Controls.Add(lblRemark, 0, row);

            txtRemark = new TextBox
            {
                Size = new Size(240, 70),
                Multiline = true,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 0)
            };
            mainTable.Controls.Add(txtRemark, 1, row);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            row++;

            // ===== 错误提示 =====
            lblError = new Label
            {
                ForeColor = Color.FromArgb(255, 100, 100),
                AutoSize = true,
                Visible = false,
                Margin = new Padding(0, 6, 0, 4)
            };
            mainTable.Controls.Add(lblError, 0, row);
            mainTable.SetColumnSpan(lblError, 2);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            row++;

            // ===== 按钮 =====
            var btnFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 0),
                Padding = new Padding(0)
            };

            btnSave = new Button
            {
                Text = "保存记录",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                Size = new Size(120, 36),
                BackColor = Color.FromArgb(0, 150, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 12, 0)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click!;

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

            btnFlow.Controls.AddRange(new Control[] { btnSave, btnCancel });
            mainTable.Controls.Add(btnFlow, 0, row);
            mainTable.SetColumnSpan(btnFlow, 2);
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            row++;

            this.Controls.Add(mainTable);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
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
