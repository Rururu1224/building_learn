using BuildingLearn.Global;
using BuildingLearn.Core;
using BuildingLearn.Services;

namespace BuildingLearn;

/// <summary>
/// Person A: 试验现象记录对话框
/// </summary>
public class TestRecordForm : Form
{
    private CheckBox? _chkFlame;
    private TextBox? _txtFlameStart;
    private TextBox? _txtFlameDuration;
    private TextBox? _txtPostWeight;
    private TextBox? _txtRemark;
    private Label? _lblPreWeight;
    private Label? _lblCalculated;
    private Button? _btnSave;

    public TestRecordForm()
    {
        InitializeUI();
        LoadTestInfo();
        this.Text = "试验现象记录";
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Size = new Size(420, 480);
    }

    private void InitializeUI()
    {
        int y = 15;
        int lw = 130, cw = 200;

        _chkFlame = new CheckBox
        {
            Text = "是否出现持续火焰",
            Location = new Point(20, y), Size = new Size(200, 24),
            Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
        };
        _chkFlame.CheckedChanged += (s, e) =>
        {
            _txtFlameStart!.Enabled = _chkFlame.Checked;
            _txtFlameDuration!.Enabled = _chkFlame.Checked;
        };
        this.Controls.Add(_chkFlame);
        y += 35;

        AddLabelRow("火焰发生时刻 (秒):", lw, y);
        _txtFlameStart = AddTextBox(150, y, cw); _txtFlameStart.Enabled = false;
        y += 30;
        AddLabelRow("火焰持续时间 (秒):", lw, y);
        _txtFlameDuration = AddTextBox(150, y, cw); _txtFlameDuration.Enabled = false;
        y += 35;

        this.Controls.Add(new Label { Location = new Point(20, y), Size = new Size(370, 2), BorderStyle = BorderStyle.Fixed3D });
        y += 15;

        AddLabelRow("试验前质量 (g):", lw, y);
        _lblPreWeight = new Label { Location = new Point(150, y + 3), Size = new Size(cw, 20), Text = "0" };
        this.Controls.Add(_lblPreWeight);
        y += 30;

        AddLabelRow("试验后质量 (g):", lw, y);
        _txtPostWeight = AddTextBox(150, y, cw);
        _txtPostWeight.TextChanged += (s, e) => UpdateCalculated();
        y += 30;

        AddLabelRow("失重量 / 失重率:", lw, y);
        _lblCalculated = new Label { Location = new Point(150, y + 3), Size = new Size(cw, 20), Text = "—" };
        this.Controls.Add(_lblCalculated);
        y += 35;

        this.Controls.Add(new Label { Location = new Point(20, y), Size = new Size(370, 2), BorderStyle = BorderStyle.Fixed3D });
        y += 15;

        AddLabelRow("备注:", lw, y);
        _txtRemark = new TextBox { Location = new Point(150, y), Size = new Size(cw, 60), Multiline = true, ScrollBars = ScrollBars.Vertical };
        this.Controls.Add(_txtRemark);
        y += 70;

        _btnSave = new Button
        {
            Text = "保存试验记录",
            Location = new Point(130, y), Size = new Size(140, 38),
            BackColor = Color.LightGreen, Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
        };
        _btnSave.Click += BtnSave_Click;
        this.Controls.Add(_btnSave);
    }

    private void AddLabelRow(string text, int x, int y) =>
        this.Controls.Add(new Label { Text = text, Location = new Point(20, y + 3), Size = new Size(x, 20) });

    private TextBox AddTextBox(int x, int y, int width)
    {
        var tb = new TextBox { Location = new Point(x, y), Size = new Size(width, 23) };
        this.Controls.Add(tb); return tb;
    }

    private void LoadTestInfo()
    {
        var trial = AppGlobal.Instance.TestMaster.CurrentTrial;
        if (trial != null)
        {
            _lblPreWeight!.Text = trial.PreWeight.ToString("F2");
            _txtPostWeight!.Text = trial.PostWeight > 0 ? trial.PostWeight.ToString("F2") : "";
        }
    }

    private void UpdateCalculated()
    {
        var trial = AppGlobal.Instance.TestMaster.CurrentTrial;
        if (trial == null) return;
        if (double.TryParse(_txtPostWeight!.Text, out double postWeight) && trial.PreWeight > 0)
        {
            double lw = TestMetrics.ComputeLostWeight(trial.PreWeight, postWeight);
            double lp = TestMetrics.ComputeLostWeightPercent(trial.PreWeight, postWeight);
            _lblCalculated!.Text = $"{lw:F2} g / {lp:F2} %";
        }
        else _lblCalculated!.Text = "请输入有效的试验后质量";
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        var ctx = AppGlobal.Instance;
        var trial = ctx.TestMaster.CurrentTrial;
        if (trial == null)
        { MessageBox.Show("没有当前试验记录", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

        if (!double.TryParse(_txtPostWeight!.Text, out double postWeight) || postWeight <= 0)
        { MessageBox.Show("请输入有效的试验后质量", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); _txtPostWeight!.Focus(); return; }

        bool hasFlame = _chkFlame!.Checked;
        int flameStart = 0, flameDuration = 0;
        if (hasFlame)
        {
            int.TryParse(_txtFlameStart!.Text, out flameStart);
            int.TryParse(_txtFlameDuration!.Text, out flameDuration);
        }

        // -------- Person B: 计算指标 --------
        double lostWeight = TestMetrics.ComputeLostWeight(trial.PreWeight, postWeight);
        double lostPer = TestMetrics.ComputeLostWeightPercent(trial.PreWeight, postWeight);

        var temps = ctx.TestMaster.GetAllTemperatures();
        double finalTF1 = temps.Count > 0 ? temps[^1][0] : 750;
        double finalTF2 = temps.Count > 0 ? temps[^1][1] : 750;
        double finalTS  = temps.Count > 0 ? temps[^1][2] : 720;
        double finalTC  = temps.Count > 0 ? temps[^1][3] : 640;

        double deltaTF1 = TestMetrics.ComputeTemperatureRise(finalTF1, trial.AmbientTemp);
        double deltaTF2 = TestMetrics.ComputeTemperatureRise(finalTF2, trial.AmbientTemp);
        double deltaTS  = TestMetrics.ComputeTemperatureRise(finalTS, trial.AmbientTemp);
        double deltaTC  = TestMetrics.ComputeTemperatureRise(finalTC, trial.AmbientTemp);
        double deltatf  = TestMetrics.ComputeDeltaTF(deltaTS, deltaTC);

        int totalTestTime = ctx.TestMaster.TotalTestTime;
        int constPower = ctx.TestMaster.ConstPowerValue;

        // -------- Person B: 更新当前试验信息 --------
        trial.PostWeight = postWeight;
        ctx.TestMaster.SetTotalTestTime(totalTestTime);
        ctx.TestMaster.MarkSaved();

        // -------- Person C: 落库 --------
        ctx.Db.UpdateTestMasterPostWeight(trial.ProductId, trial.TestId, postWeight, lostWeight, lostPer,
            hasFlame, flameStart, flameDuration, _txtRemark!.Text);
        ctx.Db.UpdateTestMasterFinalTemps(trial.ProductId, trial.TestId,
            finalTF1, finalTF2, finalTS, finalTC,
            deltaTF1, deltaTF2, deltaTS, deltaTC, deltatf, totalTestTime, constPower);
        ctx.Db.UpdateTestMasterFlag(trial.ProductId, trial.TestId, "10000000");

        // -------- Person C: 导出 --------
        try
        {
            var allTemps = ctx.TestMaster.GetAllTemperatures();
            // Person C 的 ExportService 需要 TestMasterRecord，这里用 Person B 数据拼装
            var record = ctx.Db.GetTestMaster(trial.ProductId, trial.TestId);
            if (record != null)
            {
                ctx.Export.ExportExcel(record, allTemps);
                if (ctx.Config.EnablePdfExport) ctx.Export.ExportPdf(record, allTemps);
            }
        }
        catch (Exception ex)
        { MessageBox.Show($"报告生成失败: {ex.Message}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning); }

        MessageBox.Show("试验记录保存成功！", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        this.DialogResult = DialogResult.OK;
        this.Close();
    }
}
