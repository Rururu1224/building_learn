#nullable disable

using System;
using System.Drawing;
using System.Windows.Forms;
using BuildingLearn.Global;

namespace BuildingFireTest.UI
{
    /// <summary>
    /// 参数设置弹窗
    /// 显示当前仿真参数、设备参数（来自 appsettings.json），可查看
    /// </summary>
    public partial class ParameterSettingsDialog : Form
    {
        public ParameterSettingsDialog()
        {
            InitializeComponent();
            LoadParameters();
        }

        private TableLayoutPanel _mainTable;

        private void InitializeComponent()
        {
            this.Text = "参数设置";
            this.Size = new Size(500, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 45);
            this.Font = new Font("Microsoft YaHei", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;

            _mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Color.FromArgb(45, 45, 45),
                Padding = new Padding(20, 15, 20, 15)
            };
            _mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _mainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            this.Controls.Add(_mainTable);
        }

        private void LoadParameters()
        {
            _mainTable.Controls.Clear();
            _mainTable.RowStyles.Clear();
            int row = 0;

            try
            {
                var cfg = AppGlobal.Instance.Config;

                // === 仿真参数 ===
                AddSectionHeader("仿真参数", ref row);
                AddParamRow("目标炉温 (°C)：", cfg.TargetFurnaceTemp.ToString("F1"), ref row);
                AddParamRow("初始炉温 (°C)：", cfg.InitialFurnaceTemp.ToString("F1"), ref row);
                AddParamRow("升温速率 (°C/s)：", cfg.HeatingRatePerSecond.ToString("F1"), ref row);
                AddParamRow("温度波动 (°C)：", cfg.TempFluctuation.ToString("F2"), ref row);
                AddParamRow("稳定阈值 (°C)：", cfg.StableThreshold.ToString("F1"), ref row);
                AddParamRow("仿真模式：", cfg.EnableSimulation ? "启用" : "关闭", ref row);

                // === 设备参数 ===
                AddSectionHeader("设备参数", ref row);
                AddParamRow("恒功率值：", cfg.ConstPower.ToString(), ref row);
                AddParamRow("PID 目标温度 (°C)：", cfg.PidTemperature.ToString(), ref row);

                // === 文件存储 ===
                AddSectionHeader("文件存储", ref row);
                AddParamRow("基础目录：", cfg.BaseDirectory, ref row);
                AddParamRow("数据目录：", cfg.TestDataDirectory, ref row);
                AddParamRow("报告目录：", cfg.OutputDirectory, ref row);
                AddParamRow("PDF 导出：", cfg.EnablePdfExport ? "启用" : "关闭", ref row);
            }
            catch (Exception ex)
            {
                AddParamRow("错误：", ex.Message, ref row);
            }

            // 关闭按钮
            row++;
            _mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
            row++;

            var btnClose = new Button
            {
                Text = "关闭",
                Font = new Font("Microsoft YaHei", 10F),
                Size = new Size(100, 36),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.None
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            _mainTable.Controls.Add(btnClose, 0, row);
            _mainTable.SetColumnSpan(btnClose, 2);
            _mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        }

        private void AddSectionHeader(string text, ref int row)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 220),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 10, 0, 2)
            };
            _mainTable.Controls.Add(lbl, 0, row);
            _mainTable.SetColumnSpan(lbl, 2);
            _mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            row++;
        }

        private void AddParamRow(string label, string value, ref int row)
        {
            var lblName = new Label
            {
                Text = label,
                ForeColor = Color.FromArgb(180, 180, 180),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(0, 4, 8, 0)
            };
            _mainTable.Controls.Add(lblName, 0, row);

            var lblValue = new Label
            {
                Text = value,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 4, 0, 0)
            };
            _mainTable.Controls.Add(lblValue, 1, row);
            _mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            row++;
        }
    }
}
