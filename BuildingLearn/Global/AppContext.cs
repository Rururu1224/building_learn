using BuildingLearn.Data;
using BuildingLearn.Services;
using BuildingLearn.Core;
using Serilog;

namespace BuildingLearn.Global;

/// <summary>
/// 全局胶水层 — 单例，负责装配 Person A/B/C 三个模块。
/// Person B 代码零外部依赖，所有跨模块依赖由本类桥接。
/// </summary>
public class AppGlobal
{
    private static AppGlobal? _instance;
    private static readonly object _lock = new();

    public static AppGlobal Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new AppGlobal();
                }
            }
            return _instance;
        }
    }

    // ===== Person C 服务（数据 / 配置 / 导出） =====
    public ConfigService Config { get; private set; } = null!;
    public DbHelper Db { get; private set; } = null!;
    public ExportService Export { get; private set; } = null!;

    // ===== Person B 核心（仿真 / 状态机 / 采集 / 计算） =====
    public SensorSimulator Simulator { get; private set; } = null!;
    public TestMaster TestMaster { get; private set; } = null!;
    public DaqWorker DaqWorker { get; private set; } = null!;

    // ===== 当前用户信息（Person A 登录后设置） =====
    public string CurrentOperator { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;

    private AppGlobal() { }

    public void Initialize()
    {
        // -------- Person C: 读取配置 --------
        Config = new ConfigService();

        // Person C → Person B 配置 POCO 转换
        var simCfg = new SensorSimulationConfig
        {
            InitialFurnaceTemp = Config.InitialFurnaceTemp,
            TargetFurnaceTemp = Config.TargetFurnaceTemp,
            HeatingRatePerSecond = Config.HeatingRatePerSecond,
            TempFluctuation = Config.TempFluctuation,
            StableThreshold = Config.StableThreshold,
        };

        // -------- Person C: 系统日志 --------
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.LogDirectory);
        if (!Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logDir, "iso11820_.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: Config.RetainedFileCountLimit,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("AppGlobal 初始化开始...");

        // -------- Person C: 数据库 --------
        Db = new DbHelper(Config);

        // -------- Person B: 仿真引擎 --------
        Simulator = new SensorSimulator(simCfg);

        // -------- Person B: 状态机 --------
        TestMaster = new TestMaster(simCfg);

        // -------- Person B: 定时采集 --------
        DaqWorker = new DaqWorker(Simulator, TestMaster, periodMs: 800);

        // -------- 桥接 Person B → Person C: CSV 每秒落盘 --------
        DaqWorker.OnCsvLineReady = (second, temps) =>
        {
            // Person C 负责：建目录、写入 CSV
            var trial = TestMaster.CurrentTrial;
            if (trial == null) return;

            var dir = Path.Combine(Config.TestDataDirectory, trial.ProductId, trial.TestId);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var csvPath = Path.Combine(dir, "sensor_data.csv");

            // 首行写入表头
            if (second == 1 && !File.Exists(csvPath))
            {
                File.WriteAllText(csvPath, "Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration\n");
            }

            var line = $"{second},{temps[0]:F1},{temps[1]:F1},{temps[2]:F1},{temps[3]:F1},{temps[4]:F1}\n";
            File.AppendAllText(csvPath, line);
        };

        // -------- Person C: 导出服务 --------
        Export = new ExportService(Config, Db);

        Log.Information("AppGlobal 初始化完成，操作员：{Operator}", CurrentOperator);
    }

    /// <summary>
    /// 添加系统消息并广播 — 便捷方法，Person A 直接调用。
    /// </summary>
    public void AddMessage(string message)
    {
        TestMaster.AddMessage(message);
    }
}
