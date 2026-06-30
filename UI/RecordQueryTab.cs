#nullable disable

using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BuildingFireTest.Interfaces;

namespace BuildingFireTest.UI
{
    /// <summary>
    /// 记录查询Tab页面
    /// 使用 FlowLayoutPanel + Dock 布局，筛选控件自动换行不堆叠
    /// </summary>
    public partial class RecordQueryTab : UserControl
    {
        private readonly IDataService _dataService;

        // ========== 筛选控件 ==========
        private DateTimePicker dtpStartDate;
        private DateTimePicker dtpEndDate;
        private TextBox txtSearchProductId;
        private ComboBox cmbOperator;
        private Button btnSearch;
        private Button btnClearFilter;
        private Button btnExportResults;

        // ========== 结果表格 ==========
        private DataGridView dgvResults;

        // ========== 状态标签 ==========
        private Label lblResultCount;

        public RecordQueryTab(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            InitializeComponent();
            LoadOperators();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Padding = new Padding(10);

            // ========== 上方：筛选面板 ==========
            var pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblFilterTitle = new Label
            {
                Text = "筛选条件",
                Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 使用 FlowLayoutPanel 让筛选控件自动排列、换行
            var flowFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = false,
                Padding = new Padding(0, 6, 0, 0),
                Margin = new Padding(0)
            };

            // 开始日期
            var lblStartDate = new Label
            {
                Text = "开始日期：",
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0)
            };
            dtpStartDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Size = new Size(110, 23),
                Value = DateTime.Now.AddMonths(-1),
                Margin = new Padding(0, 3, 16, 0)
            };

            // 结束日期
            var lblEndDate = new Label
            {
                Text = "结束日期：",
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0)
            };
            dtpEndDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Size = new Size(110, 23),
                Value = DateTime.Now,
                Margin = new Padding(0, 3, 16, 0)
            };

            // 样品编号
            var lblProductId = new Label
            {
                Text = "样品编号：",
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0)
            };
            txtSearchProductId = new TextBox
            {
                Size = new Size(130, 23),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "模糊搜索",
                Margin = new Padding(0, 3, 16, 0)
            };

            // 操作员下拉
            var lblOperator = new Label
            {
                Text = "操作员：",
                ForeColor = Color.FromArgb(180, 180, 180),
                AutoSize = true,
                Margin = new Padding(0, 7, 4, 0)
            };
            cmbOperator = new ComboBox
            {
                Size = new Size(120, 23),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 3, 16, 0)
            };

            // 按钮
            btnSearch = new Button
            {
                Text = "查询",
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 6, 0)
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += BtnSearch_Click!;

            btnClearFilter = new Button
            {
                Text = "清除",
                Font = new Font("Microsoft YaHei", 9F),
                Size = new Size(60, 30),
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 6, 0)
            };
            btnClearFilter.FlatAppearance.BorderSize = 0;
            btnClearFilter.Click += BtnClearFilter_Click!;

            btnExportResults = new Button
            {
                Text = "导出Excel",
                Font = new Font("Microsoft YaHei", 9F),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 150, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 0)
            };
            btnExportResults.FlatAppearance.BorderSize = 0;
            btnExportResults.Click += BtnExportResults_Click!;

            flowFilter.Controls.AddRange(new Control[] {
                lblStartDate, dtpStartDate,
                lblEndDate, dtpEndDate,
                lblProductId, txtSearchProductId,
                lblOperator, cmbOperator,
                btnSearch, btnClearFilter, btnExportResults
            });

            pnlFilter.Controls.Add(flowFilter);
            pnlFilter.Controls.Add(lblFilterTitle);

            // ========== 下方：结果面板 ==========
            var pnlResults = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(15, 10, 15, 10)
            };

            var pnlResultsHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(0)
            };

            var lblResultsTitle = new Label
            {
                Text = "查询结果",
                Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblResultCount = new Label
            {
                Text = "共 0 条记录",
                Font = new Font("Microsoft YaHei", 9F),
                ForeColor = Color.FromArgb(160, 160, 160),
                Dock = DockStyle.Left,
                AutoSize = true,
                Padding = new Padding(8, 5, 0, 0)
            };

            pnlResultsHeader.Controls.Add(lblResultCount);
            pnlResultsHeader.Controls.Add(lblResultsTitle);

            dgvResults = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(50, 50, 50),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(0, 122, 204),
                    SelectionForeColor = Color.White,
                    Font = new Font("Microsoft YaHei", 9F)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(70, 70, 70)
            };

            dgvResults.CellDoubleClick += DgvResults_CellDoubleClick!;

            // 注意添加顺序：Dock从底/右开始排列
            pnlResults.Controls.Add(dgvResults);
            pnlResults.Controls.Add(pnlResultsHeader);

            // 添加到 UserControl（先Fill后Top）
            this.Controls.Add(pnlResults);
            this.Controls.Add(pnlFilter);
        }

        private void LoadOperators()
        {
            try
            {
                var operators = _dataService.GetOperatorNames();
                cmbOperator.Items.Clear();
                cmbOperator.Items.Add("（全部）");
                foreach (var op in operators)
                    cmbOperator.Items.Add(op);
                cmbOperator.SelectedIndex = 0;
            }
            catch
            {
                cmbOperator.Items.Add("（全部）");
                cmbOperator.SelectedIndex = 0;
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            ExecuteSearch();
        }

        private void BtnClearFilter_Click(object sender, EventArgs e)
        {
            dtpStartDate.Value = DateTime.Now.AddMonths(-1);
            dtpEndDate.Value = DateTime.Now;
            txtSearchProductId.Text = string.Empty;
            cmbOperator.SelectedIndex = 0;
            dgvResults.DataSource = null;
            lblResultCount.Text = "共 0 条记录";
        }

        private void ExecuteSearch()
        {
            try
            {
                string? operatorName = null;
                if (cmbOperator.SelectedIndex > 0)
                    operatorName = cmbOperator.SelectedItem?.ToString();

                DataTable dt = _dataService.QueryTestRecords(
                    dtpStartDate.Value.Date,
                    dtpEndDate.Value.Date.AddDays(1).AddSeconds(-1),
                    string.IsNullOrWhiteSpace(txtSearchProductId.Text) ? null : txtSearchProductId.Text.Trim(),
                    operatorName);

                dgvResults.DataSource = dt;
                lblResultCount.Text = $"共 {dt.Rows.Count} 条记录";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvResults_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            try
            {
                var row = dgvResults.Rows[e.RowIndex];
                string productId = row.Cells["productid"].Value?.ToString() ?? "";
                string testId = row.Cells["testid"].Value?.ToString() ?? "";

                if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(testId))
                    return;

                var detail = _dataService.GetTestDetail(productId, testId);
                if (detail != null)
                {
                    ShowTestDetailDialog(detail);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取详情失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowTestDetailDialog(TestDetailInfo detail)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"样品编号：{detail.ProductId}");
            sb.AppendLine($"试验标识：{detail.TestId}");
            sb.AppendLine($"样品名称：{detail.ProductName}");
            sb.AppendLine($"规格型号：{detail.Specification}");
            sb.AppendLine($"尺寸：{detail.Height}mm × {detail.Diameter}mm");
            sb.AppendLine($"操作员：{detail.Operator}");
            sb.AppendLine($"试验日期：{detail.TestDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"试验时长：{detail.TotalTestTime} 秒");
            sb.AppendLine($"环境温度：{detail.EnvironmentTemp:F1}°C");
            sb.AppendLine($"环境湿度：{detail.EnvironmentHumidity:F1}%");
            sb.AppendLine("───────────────");
            sb.AppendLine($"试验前质量：{detail.PreWeight:F2} g");
            sb.AppendLine($"试验后质量：{detail.PostWeight:F2} g");
            sb.AppendLine($"失重量：{detail.LostWeight:F2} g");
            sb.AppendLine($"失重率：{detail.LostWeightPercent:F2}%");
            sb.AppendLine("───────────────");
            sb.AppendLine($"炉温1温升：{detail.TempRiseFurnace1:F1}°C");
            sb.AppendLine($"炉温2温升：{detail.TempRiseFurnace2:F1}°C");
            sb.AppendLine($"表面温升：{detail.TempRiseSurface:F1}°C");
            sb.AppendLine($"中心温升：{detail.TempRiseCenter:F1}°C");
            sb.AppendLine($"综合温升(ΔTf)：{detail.DeltaTf:F1}°C");
            sb.AppendLine("───────────────");
            if (detail.HasFlame)
            {
                sb.AppendLine($"持续火焰：是（{detail.FlameStartTime}秒起，持续{detail.FlameDuration}秒）");
            }
            else
            {
                sb.AppendLine("持续火焰：否");
            }
            sb.AppendLine($"判定标识：{detail.Flag}");
            if (!string.IsNullOrEmpty(detail.Remark))
                sb.AppendLine($"备注：{detail.Remark}");

            MessageBox.Show(sb.ToString(), "试验详情",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnExportResults_Click(object sender, EventArgs e)
        {
            if (dgvResults.DataSource is not DataTable dt || dt.Rows.Count == 0)
            {
                MessageBox.Show("没有可导出的数据。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string path = _dataService.ExportQueryResults(dt);
                MessageBox.Show($"查询结果已导出至：\n{path}", "导出成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
