using BuildingLearn.Global;
using BuildingLearn.Core;
using BuildingLearn.Data.Models;
using BuildingLearn.Services;

namespace BuildingLearn;

/// <summary>
/// Person A: 新建试验对话框
/// </summary>
public class NewTestForm : Form
{
    private TextBox? _txtProductId;
    private TextBox? _txtTestId;
    private TextBox? _txtProductName;
    private TextBox? _txtSpecification;
    private TextBox? _txtHeight;
    private TextBox? _txtDiameter;
    private TextBox? _txtAmbientTemp;
    private TextBox? _txtAmbientHumidity;
    private TextBox? _txtPreWeight;
    private TextBox? _txtTargetMinutes;
    private ComboBox? _cboTestMode;
    private Label? _lblApparatusInfo;
    private Button? _btnCreate;
    private Button? _btnCancel;

    public NewTestForm()
    {
        InitializeUI();
        this.Text = "新建试验";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Size = new Size(480, 580);
    }

    private void InitializeUI()
    {
        int y = 15;
        int labelWidth = 110;
        int ctrlWidth = 200;

        var ctx = AppGlobal.Instance;

        // 样品编号
        AddLabel("样品编号:", 15, y);
        _txtProductId = AddTextBox(labelWidth + 15, y, ctrlWidth);
        y += 30;

        // 试验标识
        AddLabel("试验标识:", 15, y);
        _txtTestId = AddTextBox(labelWidth + 15, y, ctrlWidth);
        _txtTestId.Text = $"T{DateTime.Now:yyyyMMddHHmmss}";
        y += 30;

        // 样品名称
        AddLabel("样品名称:", 15, y);
        _txtProductName = AddTextBox(labelWidth + 15, y, ctrlWidth);
        y += 30;

        // 规格
        AddLabel("规格:", 15, y);
        _txtSpecification = AddTextBox(labelWidth + 15, y, ctrlWidth);
        y += 30;

        // 高度
        AddLabel("高度 (mm):", 15, y);
        _txtHeight = AddTextBox(labelWidth + 15, y, ctrlWidth);
        _txtHeight.Text = "50";
        y += 30;

        // 直径
        AddLabel("直径 (mm):", 15, y);
        _txtDiameter = AddTextBox(labelWidth + 15, y, ctrlWidth);
        _txtDiameter.Text = "45";
        y += 30;

        // 环境温度
        AddLabel("环境温度 (°C):", 15, y);
        _txtAmbientTemp = AddTextBox(labelWidth + 15, y, ctrlWidth);
        _txtAmbientTemp.Text = "25.0";
        y += 30;

        // 环境湿度
        AddLabel("环境湿度 (%):", 15, y);
        _txtAmbientHumidity = AddTextBox(labelWidth + 15, y, ctrlWidth);
        _txtAmbientHumidity.Text = "50.0";
        y += 30;

        // 试验前质量
        AddLabel("试验前质量 (g):", 15, y);
        _txtPreWeight = AddTextBox(labelWidth + 15, y, ctrlWidth);
        _txtPreWeight.Text = "50.0";
        y += 30;

        // 试验模式
        AddLabel("试验模式:", 15, y);
        _cboTestMode = new ComboBox
        {
            Location = new Point(labelWidth + 15, y),
            Size = new Size(ctrlWidth, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cboTestMode.Items.AddRange(new[] { "标准60分钟", "固定时长" });
        _cboTestMode.SelectedIndex = 0;
        _cboTestMode.SelectedIndexChanged += (s, e) =>
        {
            _txtTargetMinutes!.Enabled = _cboTestMode.SelectedIndex == 1;
        };
        this.Controls.Add(_cboTestMode);
        y += 30;

        // 目标时长（分钟）
        AddLabel("目标时长 (分钟):", 15, y);
        _txtTargetMinutes = new TextBox
        {
            Location = new Point(labelWidth + 15, y),
            Size = new Size(ctrlWidth, 23),
            Text = "60",
            Enabled = false,
        };
        this.Controls.Add(_txtTargetMinutes);
        y += 30;

        // 设备信息（自动带入 — 调用 Person C）
        AddLabel("设备信息:", 15, y);
        var apparatus = ctx.Db.GetFirstApparatus();
        _lblApparatusInfo = new Label
        {
            Location = new Point(labelWidth + 15, y),
            Size = new Size(ctrlWidth + 100, 20),
            Text = apparatus != null
                ? $"{apparatus.ApparatusId} {apparatus.ApparatusName}"
                : "ISO-001 不燃性试验炉",
        };
        this.Controls.Add(_lblApparatusInfo);
        y += 40;

        // 按钮
        _btnCreate = new Button
        {
            Text = "创建试验",
            Location = new Point(120, y),
            Size = new Size(100, 35),
            BackColor = Color.LightSteelBlue,
        };
        _btnCreate.Click += BtnCreate_Click;
        this.Controls.Add(_btnCreate);

        _btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(250, y),
            Size = new Size(80, 35),
        };
        _btnCancel.Click += (s, e) => this.Close();
        this.Controls.Add(_btnCancel);
    }

    private void AddLabel(string text, int x, int y)
    {
        this.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x, y + 3),
            Size = new Size(105, 20),
            TextAlign = ContentAlignment.MiddleRight,
        });
    }

    private TextBox AddTextBox(int x, int y, int width)
    {
        var tb = new TextBox { Location = new Point(x, y), Size = new Size(width, 23) };
        this.Controls.Add(tb);
        return tb;
    }

    private void BtnCreate_Click(object? sender, EventArgs e)
    {
        var ctx = AppGlobal.Instance;

        // 验证
        if (string.IsNullOrWhiteSpace(_txtProductId!.Text))
        { MessageBox.Show("请输入样品编号", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrWhiteSpace(_txtTestId!.Text))
        { MessageBox.Show("请输入试验标识", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrWhiteSpace(_txtProductName!.Text))
        { MessageBox.Show("请输入样品名称", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!double.TryParse(_txtPreWeight!.Text, out double preWeight) || preWeight <= 0)
        { MessageBox.Show("请输入有效的试验前质量", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        // -------- Person C: 获取设备信息 & 入库 --------
        var apparatus = ctx.Db.GetFirstApparatus();
        var isStandardMode = _cboTestMode!.SelectedIndex == 0;
        int targetDuration = isStandardMode ? 3600
            : int.TryParse(_txtTargetMinutes!.Text, out int min) ? min * 60 : 3600;

        var record = new TestMasterRecord
        {
            ProductId = _txtProductId.Text.Trim(),
            TestId = _txtTestId.Text.Trim(),
            TestDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Operator = ctx.CurrentOperator,
            ApparatusId = apparatus?.ApparatusId ?? "ISO-001",
            ApparatusName = apparatus?.ApparatusName ?? "不燃性试验炉 ISO11820",
            AmbientTemp = double.TryParse(_txtAmbientTemp!.Text, out double at) ? at : 25.0,
            AmbientHumidity = double.TryParse(_txtAmbientHumidity!.Text, out double ah) ? ah : 50.0,
            ProductName = _txtProductName.Text.Trim(),
            Specification = _txtSpecification!.Text.Trim(),
            Height = double.TryParse(_txtHeight!.Text, out double h) ? h : 50,
            Diameter = double.TryParse(_txtDiameter!.Text, out double d) ? d : 45,
            PreWeight = preWeight,
            TestMode = isStandardMode ? "Standard60Min" : "FixedDuration",
            TargetDuration = targetDuration,
            CalibrationDate = apparatus?.CalibrationDate ?? DateTime.MinValue,
            ConstPowerValue = ctx.Config.ConstPower,
        };

        ctx.Db.InsertTestMaster(record);
        ctx.Db.InsertOrUpdateProduct(new ProductMaster
        {
            ProductId = record.ProductId,
            ProductName = record.ProductName,
            Specification = record.Specification,
            Height = record.Height,
            Diameter = record.Diameter,
            CreateTime = DateTime.Now,
        });

        // -------- Person B: 注入当前试验上下文 --------
        var trialInfo = new CurrentTrialInfo
        {
            ProductId = record.ProductId,
            TestId = record.TestId,
            AmbientTemp = record.AmbientTemp,
            AmbientHumidity = record.AmbientHumidity,
            PreWeight = record.PreWeight,
            TestMode = record.TestMode,
            TargetDuration = record.TargetDuration,
            Flag = record.Flag,
            ApparatusId = record.ApparatusId,
            ApparatusName = record.ApparatusName,
        };
        ctx.TestMaster.SetCurrentTrial(trialInfo);
        ctx.TestMaster.ResetTrialState();
        ctx.TestMaster.State = TestStates.Idle;

        MessageBox.Show($"试验创建成功！\n样品: {record.ProductName}\n编号: {record.ProductId}/{record.TestId}",
            "创建成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
