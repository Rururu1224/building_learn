#nullable disable

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BuildingFireTest.Interfaces;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace BuildingFireTest.UI
{
    /// <summary>
    /// 多试验对比分析 Tab
    /// 支持同时对比多次试验的温度曲线和统计数据
    /// </summary>
    public partial class ComparisonTab : UserControl
    {
        private readonly IDataService _dataService;

        // ========== 控件 ==========
        private CheckedListBox clbTests;
        private Button btnLoadTests;
        private PlotView plotView;
        private PlotModel plotModel;
        private Panel pnlStats;

        // ========== 颜色方案 ==========
        private static readonly Color[] CurveColors = new[]
        {
            Color.FromArgb(255, 80, 80),    // 红
            Color.FromArgb(80, 180, 255),   // 蓝
            Color.FromArgb(80, 255, 120),   // 绿
            Color.FromArgb(255, 180, 60),   // 橙
            Color.FromArgb(200, 100, 255),  // 紫
            Color.FromArgb(255, 100, 200),  // 粉
            Color.FromArgb(100, 255, 255),  // 青
        };

        public ComparisonTab(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Padding = new Padding(10);

            // ========== 顶部：试验选择区 ==========
            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(12)
            };

            var lblTitle = new Label
            {
                Text = "多试验对比分析",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                AutoSize = true
            };

            clbTests = new CheckedListBox
            {
                Location = new Point(15, 45),
                Size = new Size(500, 60),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                CheckOnClick = true,
                MultiColumn = true,
                ColumnWidth = 200
            };
            clbTests.ItemCheck += ClbTests_ItemCheck!;

            btnLoadTests = new Button
            {
                Text = "加载历史试验",
                Font = new Font("Microsoft YaHei", 9F),
                Size = new Size(120, 32),
                Location = new Point(530, 45),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLoadTests.FlatAppearance.BorderSize = 0;
            btnLoadTests.Click += BtnLoadTests_Click!;

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(clbTests);
            pnlTop.Controls.Add(btnLoadTests);

            // ========== 中间：OxyPlot 曲线图 ==========
            plotView = new PlotView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20)
            };

            // ========== 底部：统计摘要 ==========
            pnlStats = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(12)
            };

            InitializePlotModel();

            this.Controls.Add(plotView);
            this.Controls.Add(pnlStats);
            this.Controls.Add(pnlTop);
        }

        private void InitializePlotModel()
        {
            plotModel = new PlotModel
            {
                Title = "温度曲线对比",
                TitleFontSize = 14,
                TitleColor = OxyColor.FromRgb(220, 220, 220),
                TextColor = OxyColor.FromRgb(200, 200, 200),
                PlotAreaBorderColor = OxyColor.FromRgb(100, 100, 100),
                Background = OxyColor.FromRgb(20, 20, 20)
            };

            // 图例
            plotModel.Legends.Add(new OxyPlot.Legends.Legend
            {
                LegendPosition = OxyPlot.Legends.LegendPosition.RightTop,
                LegendBackground = OxyColor.FromRgb(30, 30, 30),
                LegendTextColor = OxyColor.FromRgb(220, 220, 220),
                LegendBorder = OxyColor.FromRgb(80, 80, 80),
                LegendPadding = 8
            });

            // X 轴：时间（分钟）
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "时间 (分钟)",
                TitleColor = OxyColor.FromRgb(200, 200, 200),
                TextColor = OxyColor.FromRgb(180, 180, 180),
                AxislineColor = OxyColor.FromRgb(100, 100, 100),
                Minimum = 0,
                Maximum = 60
            });

            // Y 轴：温度
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "温度 (°C)",
                TitleColor = OxyColor.FromRgb(200, 200, 200),
                TextColor = OxyColor.FromRgb(180, 180, 180),
                AxislineColor = OxyColor.FromRgb(100, 100, 100),
                Minimum = 0,
                Maximum = 800
            });

            plotView.Model = plotModel;
        }

        private void BtnLoadTests_Click(object sender, EventArgs e)
        {
            try
            {
                // 加载最近 30 天的试验
                var dt = _dataService.QueryTestRecords(
                    DateTime.Now.AddDays(-30),
                    DateTime.Now,
                    null, null);

                clbTests.Items.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    string display = $"{row["productid"]} / {row["testid"]} - {row["testdate"]}";
                    clbTests.Items.Add(new TestItem
                    {
                        ProductId = row["productid"].ToString(),
                        TestId = row["testid"].ToString(),
                        DisplayText = display,
                        LostWeightPer = Convert.ToDouble(row["lostweight_per"]),
                        Deltatf = Convert.ToDouble(row["deltatf"])
                    }, false);
                }

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("暂无历史试验数据。", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载试验列表失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class TestItem
        {
            public string ProductId { get; set; }
            public string TestId { get; set; }
            public string DisplayText { get; set; }
            public double LostWeightPer { get; set; }
            public double Deltatf { get; set; }

            public override string ToString() => DisplayText;
        }

        // 当用户勾选试验时，更新图表
        private void ClbTests_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // 延迟执行，等勾选状态更新后再读取
            BeginInvoke(new Action(UpdateComparisonChart));
        }

        private void UpdateComparisonChart()
        {
            // 清除旧曲线
            plotModel.Series.Clear();

            var selectedTests = new List<TestItem>();
            for (int i = 0; i < clbTests.Items.Count; i++)
            {
                if (clbTests.GetItemChecked(i))
                {
                    selectedTests.Add((TestItem)clbTests.Items[i]);
                }
            }

            if (selectedTests.Count == 0)
            {
                plotModel.InvalidatePlot(true);
                UpdateStatistics(selectedTests);
                return;
            }

            // 最多显示 7 条曲线
            int maxTests = Math.Min(selectedTests.Count, 7);

            for (int i = 0; i < maxTests; i++)
            {
                var test = selectedTests[i];
                var color = CurveColors[i % CurveColors.Length];

                // 生成模拟数据（实际应从 CSV 文件读取）
                var series = new LineSeries
                {
                    Title = $"{test.ProductId}/{test.TestId}",
                    Color = OxyColor.FromRgb(color.R, color.G, color.B),
                    StrokeThickness = 1.5,
                    MarkerType = MarkerType.None
                };

                // 模拟温度曲线数据（实际应从文件读取）
                for (int t = 0; t <= 60; t++)
                {
                    double temp = GenerateSimulatedTemp(t, i);
                    series.Points.Add(new DataPoint(t, temp));
                }

                plotModel.Series.Add(series);
            }

            plotModel.InvalidatePlot(true);
            UpdateStatistics(selectedTests);
        }

        private double GenerateSimulatedTemp(int minute, int testIndex)
        {
            // 模拟升温曲线（不同试验略有差异）
            double offset = testIndex * 5; // 每个试验偏移一点
            double noise = (new Random(testIndex * 1000 + minute).NextDouble() - 0.5) * 10;

            if (minute < 5)
            {
                // 升温阶段
                return 25 + (750 - 25) * (minute / 5.0) + noise * 0.5 + offset;
            }
            else
            {
                // 稳定阶段
                return 750 + noise + offset * 0.3;
            }
        }

        private void UpdateStatistics(List<TestItem> tests)
        {
            pnlStats.Controls.Clear();

            if (tests.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = "请选择要对比的试验（最多 7 个）",
                    ForeColor = Color.FromArgb(160, 160, 160),
                    Font = new Font("Microsoft YaHei", 10F),
                    Location = new Point(15, 35),
                    AutoSize = true
                };
                pnlStats.Controls.Add(lblEmpty);
                return;
            }

            // 计算统计值
            double avgLoss = tests.Average(t => t.LostWeightPer);
            double avgTempRise = tests.Average(t => t.Deltatf);
            double maxTempRise = tests.Max(t => t.Deltatf);
            double minTempRise = tests.Min(t => t.Deltatf);

            // 计算标准差
            double lossVariance = tests.Average(t => Math.Pow(t.LostWeightPer - avgLoss, 2));
            double lossStdDev = Math.Sqrt(lossVariance);

            // 显示统计摘要
            var lblStats = new Label
            {
                Text = $"📊 统计摘要  |  试验数量：{tests.Count}\n" +
                       $"平均失重率：{avgLoss:F2}%  ±{lossStdDev:F2}%  |  " +
                       $"平均温升：{avgTempRise:F1}°C\n" +
                       $"最大温升：{maxTempRise:F1}°C  |  " +
                       $"最小温升：{minTempRise:F1}°C  |  " +
                       $"极差：{maxTempRise - minTempRise:F1}°C",
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei", 9F),
                Location = new Point(15, 20),
                AutoSize = true,
                BackColor = Color.FromArgb(40, 40, 40)
            };
            pnlStats.Controls.Add(lblStats);
        }
    }
}
