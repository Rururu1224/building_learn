using System;
using System.Windows.Forms;
using BuildingFireTest.Interfaces;
using BuildingFireTest.UI;

namespace BuildingFireTest
{
    /// <summary>
    /// 程序入口
    ///
    /// 联调说明：
    /// 当人员B、C完成各自模块后，将下面 StubCoreService / StubDataService
    /// 替换为真实的实现类即可，UI层代码无需任何修改。
    ///
    /// 例如：
    ///   ICoreService coreService = new Core.TestMaster(...);
    ///   IDataService dataService = new Data.DbHelper(...);
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ============================================================
            // TODO: 联调时替换为B、C层真实实现
            // ============================================================
            ICoreService coreService = new StubCoreService();
            IDataService dataService = new StubDataService();

            // 显示登录界面
            using (var loginForm = new LoginForm(coreService))
            {
                if (loginForm.ShowDialog() != DialogResult.OK)
                {
                    // 用户取消登录，退出程序
                    return;
                }
            }

            // 登录成功，进入主界面
            Application.Run(new MainForm(coreService, dataService));
        }
    }
}