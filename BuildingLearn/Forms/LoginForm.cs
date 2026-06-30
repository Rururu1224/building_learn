using BuildingLearn.Global;
using BuildingLearn.Data.Models;
using Serilog;

namespace BuildingLearn;

/// <summary>
/// 登录窗体 — 角色选择 + 密码登录
/// </summary>
public class LoginForm : Form
{
    private RadioButton? _rbAdmin;
    private RadioButton? _rbExperimenter;
    private TextBox? _txtPassword;
    private Button? _btnLogin;
    private Label? _lblTitle;
    private GroupBox? _grpRole;
    private GroupBox? _grpPassword;

    public LoginForm()
    {
        InitializeUI();
        this.Text = "ISO 11820 不燃性试验系统 — 登录";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.Size = new Size(420, 320);
    }

    private void InitializeUI()
    {
        _lblTitle = new Label
        {
            Text = "ISO 11820\n建筑材料不燃性试验系统",
            Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(50, 20),
            Size = new Size(300, 50),
        };

        _grpRole = new GroupBox
        {
            Text = "选择角色",
            Location = new Point(50, 80),
            Size = new Size(300, 60),
        };

        _rbAdmin = new RadioButton
        {
            Text = "管理员 (admin)",
            Location = new Point(20, 25),
            Size = new Size(120, 20),
            Checked = true,
        };

        _rbExperimenter = new RadioButton
        {
            Text = "试验员 (experimenter)",
            Location = new Point(160, 25),
            Size = new Size(140, 20),
        };

        _grpRole.Controls.Add(_rbAdmin);
        _grpRole.Controls.Add(_rbExperimenter);

        _grpPassword = new GroupBox
        {
            Text = "输入密码",
            Location = new Point(50, 150),
            Size = new Size(300, 55),
        };

        _txtPassword = new TextBox
        {
            Location = new Point(20, 22),
            Size = new Size(180, 23),
            PasswordChar = '●',
            UseSystemPasswordChar = true,
        };
        _txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) DoLogin(); };

        _btnLogin = new Button
        {
            Text = "登录",
            Location = new Point(210, 20),
            Size = new Size(75, 28),
            BackColor = Color.LightSteelBlue,
        };
        _btnLogin.Click += (s, e) => DoLogin();

        _grpPassword.Controls.Add(_txtPassword);
        _grpPassword.Controls.Add(_btnLogin);

        this.Controls.Add(_lblTitle);
        this.Controls.Add(_grpRole);
        this.Controls.Add(_grpPassword);
        this.AcceptButton = _btnLogin;
    }

    private void DoLogin()
    {
        string role = _rbAdmin!.Checked ? "admin" : "experimenter";
        string username = role; // 用户名 = 角色名
        string password = _txtPassword!.Text;

        if (string.IsNullOrEmpty(password))
        {
            MessageBox.Show("请输入密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var op = AppGlobal.Instance.Db.GetOperator(username, password);
        if (op == null)
        {
            MessageBox.Show("密码错误，请重新输入", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _txtPassword.Clear();
            _txtPassword.Focus();
            return;
        }

        AppGlobal.Instance.CurrentOperator = op.Username;
        AppGlobal.Instance.CurrentRole = op.Role;
        AppGlobal.Instance.AddMessage($"系统初始化，操作员：{op.Username}");

        Log.Information("登录成功: {User}, 角色: {Role}", op.Username, op.Role);

        // 打开主界面
        var mainForm = new MainForm();
        this.Hide();
        mainForm.Show();
    }
}
