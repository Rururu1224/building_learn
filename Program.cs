using System;
using System.Windows.Forms;
using BuildingFireTest.Adapters;
using BuildingFireTest.Interfaces;
using BuildingFireTest.UI;
using BuildingLearn.Global;
using BuildingLearn.Data;

namespace BuildingFireTest
{
    /// <summary>
    /// 程序入口
    /// 初始化后端 AppGlobal → 播种初始数据 → 创建适配器 → 进入 UI 流程
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ============================================================
            // 初始化后端（AppGlobal 单例：配置、数据库、仿真引擎、状态机）
            // ============================================================
            try
            {
                AppGlobal.Instance.Initialize();
                SeedInitialData(AppGlobal.Instance.Db);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"系统初始化失败：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ============================================================
            // 创建适配器（桥接前端接口 ↔ 后端实现）
            // ============================================================
            ICoreService coreService = new CoreServiceAdapter();
            IDataService dataService = new DataServiceAdapter();

            // ============================================================
            // 显示登录界面
            // ============================================================
            using (var loginForm = new LoginForm(coreService))
            {
                if (loginForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            // ============================================================
            // 登录成功，进入主界面
            // ============================================================
            Application.Run(new MainForm(coreService, dataService));
        }

        /// <summary>
        /// 播种初始数据：操作员账号 + 设备信息（首次运行时插入，已存在则跳过）
        /// </summary>
        private static void SeedInitialData(DbHelper db)
        {
            // 管理员账号（仅当表中无 admin 时插入）
            var existingAdmin = db.GetOperator("admin", "123456");
            if (existingAdmin == null)
            {
                db.Execute(@"INSERT INTO operators (userid, username, pwd, role)
                             VALUES ('U001', 'admin', '123456', 'admin');");
            }

            // 试验员账号
            var existingExp = db.GetOperator("experimenter", "123456");
            if (existingExp == null)
            {
                db.Execute(@"INSERT INTO operators (userid, username, pwd, role)
                             VALUES ('U002', 'experimenter', '123456', 'experimenter');");
            }

            // 默认设备（仅当 apparatus 表为空时插入）
            var existingApparatus = db.GetFirstApparatus();
            if (existingApparatus == null)
            {
                db.Execute(@"INSERT INTO apparatus (apparatusid, apparatusname, comport, baudrate, constpower, calibrationdate, nextcalibrationdate)
                             VALUES ('DEV-001', '不燃性试验炉', 'COM1', 9600, 2048, date('now','-6 months'), date('now','+6 months'));");
            }

            // 默认传感器（5通道，仅当 sensors 表为空时插入）
            using (var conn = db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM sensors;";
                long count = (long)cmd.ExecuteScalar()!;
                if (count == 0)
                {
                    string[] sensors = {
                        "(1, '炉温1 (TF1)', 0, 1000, '°C', '01')",
                        "(2, '炉温2 (TF2)', 0, 1000, '°C', '02')",
                        "(3, '表面温 (TS)', 0, 1000, '°C', '03')",
                        "(4, '中心温 (TC)', 0, 1000, '°C', '04')",
                        "(5, '校准温 (TCal)', 0, 1000, '°C', '05')"
                    };
                    foreach (var s in sensors)
                    {
                        db.Execute($"INSERT INTO sensors (channelid, channelname, rangemin, rangemax, unit, modbusaddress) VALUES {s};");
                    }
                }
            }
        }
    }
}
