#nullable disable

using System;
using System.Drawing;
using System.Windows.Forms;
using BuildingFireTest.Interfaces;

namespace BuildingFireTest.UI
{
    /// <summary>
    /// 登录窗体
    /// 使用 TableLayoutPanel 布局，角色单选、密码校验、错误提示
    /// </summary>
    public partial class LoginForm : Form
    {
        private readonly ICoreService _coreService;

        private RadioButton rbAdmin;
        private RadioButton rbExperimenter;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblError;

        public LoginForm(ICoreService coreService)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // ========== 窗体设置 ==========
            this.Text = "ISO 11820 建筑材料不燃性试验系统 - 登录";
            this.Size = new Size(440, 370);
            this.MinimumSize = new Size(380, 340);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Font = new Font("Microsoft YaHei", 10F, FontStyle.Regular);
            this.AutoScaleMode = AutoScaleMode.Font;

            // ========== 主面板 ==========
            var pnlMain = new Panel
            {
                Size = new Size(370, 290),
                Location = new Point(30, 20),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            // ========== 标题 ==========
            var lblTitle = new Label
            {
                Text = "建筑材料不燃性试验系统",
                Font = new Font("Microsoft YaHei", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = true,
                Location = new Point(50, 22)
            };

            // ========== 角色选择区域 ==========
            var lblRole = new Label
            {
                Text = "选择角色：",
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Regular),
                Location = new Point(55, 85),
                AutoSize = true
            };

            rbAdmin = new RadioButton
            {
                Text = "管理员",
                Font = new Font("Microsoft YaHei", 10F),
                Location = new Point(148, 82),
                AutoSize = true,
                Checked = true,
                Tag = "admin"
            };

            rbExperimenter = new RadioButton
            {
                Text = "试验员",
                Font = new Font("Microsoft YaHei", 10F),
                Location = new Point(238, 82),
                AutoSize = true,
                Tag = "experimenter"
            };

            // ========== 密码输入区域 ==========
            var lblPassword = new Label
            {
                Text = "输入密码：",
                Font = new Font("Microsoft YaHei", 10F),
                Location = new Point(55, 130),
                AutoSize = true
            };

            txtPassword = new TextBox
            {
                Location = new Point(148, 127),
                Size = new Size(170, 25),
                UseSystemPasswordChar = true,
                Font = new Font("Microsoft YaHei", 10F),
                MaxLength = 20,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    BtnLogin_Click(this, EventArgs.Empty);
            };

            // ========== 登录按钮 ==========
            btnLogin = new Button
            {
                Text = "登  录",
                Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
                Size = new Size(130, 40),
                Location = new Point(118, 182),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click!;

            // ========== 错误提示标签 ==========
            lblError = new Label
            {
                Text = "",
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.Red,
                Location = new Point(55, 240),
                AutoSize = true,
                Visible = false
            };

            // ========== 组装界面 ==========
            pnlMain.Controls.AddRange(new Control[] {
                lblTitle, lblRole, rbAdmin, rbExperimenter,
                lblPassword, txtPassword, btnLogin, lblError
            });

            this.Controls.Add(pnlMain);

            // 角色切换时清除错误
            rbAdmin.CheckedChanged += (s, e) => { if (rbAdmin.Checked) lblError.Visible = false; };
            rbExperimenter.CheckedChanged += (s, e) => { if (rbExperimenter.Checked) lblError.Visible = false; };
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(password))
            {
                ShowError("请输入密码");
                return;
            }

            // 转发到核心层验证登录
            string? error = _coreService.Login(GetSelectedRole(), password);

            if (error == null)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                ShowError(error);
            }
        }

        private string GetSelectedRole()
        {
            return rbAdmin.Checked ? "admin" : "experimenter";
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
            txtPassword.SelectAll();
            txtPassword.Focus();
        }

        /// <summary>
        /// 获取当前选择的角色（供MainForm获取用户信息）
        /// </summary>
        public string SelectedRole => GetSelectedRole();
    }
}
