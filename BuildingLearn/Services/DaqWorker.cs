using BuildingLearn.Core;

namespace BuildingLearn.Services;

/// <summary>
/// Person B: 定时采集服务 — 800ms 定时循环后台线程。
/// 驱动仿真更新、温度数据缓存、每秒驱动 TestMaster。
/// 零外部依赖 — CSV 落盘通过 OnCsvLineReady 委托回调给 Person C。
/// </summary>
public class DaqWorker : IDisposable
{
    private System.Threading.Timer? _timer;
    private int _tickCount;
    private double _elapsedMilliseconds;
    private bool _isRunning;
    private readonly int _periodMs;

    // Person B 的核心引用（由 AppGlobal 胶水层注入）
    private readonly SensorSimulator _simulator;
    private readonly TestMaster _testMaster;

    /// <summary>
    /// 温度数据行就绪回调（Person C 订阅）。
    /// 参数：(elapsedSecond, temperatures[5]) → Person C 负责落盘到 CSV/数据库。
    /// </summary>
    public Action<int, double[]>? OnCsvLineReady;

    /// <summary>
    /// DaqWorker 构造。
    /// </summary>
    /// <param name="simulator">仿真引擎（Person B）</param>
    /// <param name="testMaster">状态机（Person B）</param>
    /// <param name="periodMs">定时周期，默认 800ms</param>
    public DaqWorker(SensorSimulator simulator, TestMaster testMaster, int periodMs = 800)
    {
        _simulator = simulator;
        _testMaster = testMaster;
        _periodMs = periodMs;
    }

    /// <summary>启动定时器</summary>
    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _tickCount = 0;
        _elapsedMilliseconds = 0;

        _timer = new System.Threading.Timer(
            callback: OnTimerTick,
            state: null,
            dueTime: 0,
            period: _periodMs);
    }

    /// <summary>停止定时器</summary>
    public void Stop()
    {
        _isRunning = false;
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
        _timer = null;
    }

    private void OnTimerTick(object? state)
    {
        if (!_isRunning) return;

        try
        {
            // 1. 驱动仿真更新
            bool isHeating = _testMaster.State != TestStates.Idle;
            _simulator.Update(_testMaster.State, isHeating);
            var temps = _simulator.GetTemperatures();

            // 2. 累加时间（800ms 粒度）
            _elapsedMilliseconds += _periodMs;

            // 3. 每秒记录一行
            if (_elapsedMilliseconds >= 1000)
            {
                _elapsedMilliseconds -= 1000;
                _testMaster.TickSecond();

                // 驱动 Person C CSV 回调
                if (_testMaster.State == TestStates.Recording)
                {
                    OnCsvLineReady?.Invoke(_testMaster.ElapsedSeconds, temps);
                }
            }

            // 4. 驱动状态机
            _testMaster.DoWork(temps, _tickCount);

            _tickCount++;
        }
        catch
        {
            // 异常由 AppGlobal 的 Serilog 或 Person C 的日志层捕获
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
