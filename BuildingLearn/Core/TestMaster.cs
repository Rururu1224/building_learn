using BuildingLearn.Services;

namespace BuildingLearn.Core;

/// <summary>
/// Person B: 试验控制器 — 五状态状态机完整实现。
/// Idle → Preparing → Ready → Recording → Complete
/// 零外部依赖 — 配置通过 SensorSimulationConfig POCO 注入，试验信息通过 CurrentTrialInfo 注入。
/// </summary>
public class TestMaster
{
    private readonly SensorSimulationConfig _cfg;

    // ===== 状态机 =====
    public TestStates State { get; set; } = TestStates.Idle;
    public TestMode Mode { get; set; } = TestMode.Standard60Min;

    // ===== 稳定判定 =====
    private int _stableTickCount;
    public bool IsStable { get; private set; }

    // ===== 计时器 =====
    public int ElapsedSeconds { get; private set; }
    public int TotalTestTime => ElapsedSeconds;

    // ===== 终止检查 =====
    private int _checkPointSeconds;
    private bool _terminatedEarly;
    public bool TerminatedEarly => _terminatedEarly;
    public string TerminationReason { get; private set; } = string.Empty;

    // ===== 恒功率队列（最多 600 点） =====
    private readonly Queue<double> _pidOutputQueue = new();
    private const int MaxPidQueue = 600;
    public int ConstPowerValue { get; private set; }

    // ===== 温度数据缓存（时序） =====
    private readonly List<double[]> _temperatureHistory = new();

    // ===== 消息队列 =====
    private readonly List<MasterMessage> _pendingMessages = new();

    // ===== 当前试验上下文（Person B 自己的 POCO） =====
    public CurrentTrialInfo? CurrentTrial { get; private set; }

    public string CurrentProductId => CurrentTrial?.ProductId ?? string.Empty;
    public string CurrentTestId => CurrentTrial?.TestId ?? string.Empty;

    /// <summary>试验保护 — 完成未保存：有记录时间但 Flag 未标记为已完成</summary>
    public bool IsCompleteUnsaved =>
        CurrentTrial != null &&
        CurrentTrial.TotalTestTime > 0 &&
        CurrentTrial.Flag != "10000000";

    /// <summary>数据广播事件（后台线程触发，Person A 订阅）</summary>
    public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;

    /// <summary>每秒 CSV 行回调（Person C 订阅，自己决定如何落盘）</summary>
    public event Action<int, double[]>? OnSecondElapsed;

    public TestMaster(SensorSimulationConfig config)
    {
        _cfg = config;
    }

    // ==================== 试验上下文注入 ====================

    /// <summary>由 Person A/C 在新建试验或恢复试验时调用，注入当前试验信息</summary>
    public void SetCurrentTrial(CurrentTrialInfo info)
    {
        CurrentTrial = info;
        Mode = info.TestMode == "FixedDuration" ? TestMode.FixedDuration : TestMode.Standard60Min;
    }

    /// <summary>由 Person A/C 更新试验后质量</summary>
    public void SetPostWeight(double postWeight)
    {
        if (CurrentTrial != null)
            CurrentTrial.PostWeight = postWeight;
    }

    /// <summary>由 Person A/C 标记已保存</summary>
    public void MarkSaved()
    {
        if (CurrentTrial != null)
            CurrentTrial.Flag = "10000000";

        // Complete → Preparing（保持炉温，节省下次升温时间）
        TransitionTo(TestStates.Preparing);
    }

    /// <summary>由 Person C 更新试验总时长（保存后回填）</summary>
    public void SetTotalTestTime(int seconds)
    {
        if (CurrentTrial != null)
            CurrentTrial.TotalTestTime = seconds;
    }

    // ==================== 状态机核心 ====================

    /// <summary>
    /// 每 800ms 被 DaqWorker 调用一次。
    /// </summary>
    public void DoWork(double[] temperatures, int tickCount)
    {
        // PID 输出队列
        _pidOutputQueue.Enqueue(temperatures[0]);
        if (_pidOutputQueue.Count > MaxPidQueue)
            _pidOutputQueue.Dequeue();

        // 温度历史缓存
        _temperatureHistory.Add((double[])temperatures.Clone());
        int maxHistory = 3600 + 600;
        while (_temperatureHistory.Count > maxHistory)
            _temperatureHistory.RemoveAt(0);

        switch (State)
        {
            case TestStates.Idle:
                DoIdle();
                break;
            case TestStates.Preparing:
                DoPreparing(temperatures);
                break;
            case TestStates.Ready:
                DoReady(temperatures);
                break;
            case TestStates.Recording:
                DoRecording(temperatures);
                break;
            case TestStates.Complete:
                DoComplete();
                break;
        }

        // 广播
        OnDataBroadcast(temperatures);
    }

    private void DoIdle()
    {
        IsStable = false;
    }

    private void DoPreparing(double[] temps)
    {
        double tf1 = temps[0];
        double tf2 = temps[1];
        double target = _cfg.TargetFurnaceTemp;
        double thresh = _cfg.StableThreshold;

        if (tf1 >= target - thresh && tf1 <= target + thresh &&
            tf2 >= target - thresh && tf2 <= target + thresh)
        {
            _stableTickCount++;
            if (_stableTickCount > 3)
            {
                IsStable = true;
                TransitionTo(TestStates.Ready);
                AddMessage("温度已稳定，可以开始记录");
            }
        }
        else
        {
            _stableTickCount = 0;
            IsStable = false;
        }
    }

    private void DoReady(double[] temps)
    {
        double tf1 = temps[0];
        double tf2 = temps[1];
        double target = _cfg.TargetFurnaceTemp;
        double thresh = _cfg.StableThreshold;

        bool inRange = tf1 >= target - thresh && tf1 <= target + thresh &&
                       tf2 >= target - thresh && tf2 <= target + thresh;

        if (!inRange)
        {
            _stableTickCount = 0;
            IsStable = false;
            AddMessage("温度跌落出稳定范围，返回升温中...");
            TransitionTo(TestStates.Preparing);
            return;
        }

        _stableTickCount++;
        if (_stableTickCount > 3)
            IsStable = true;
    }

    private void DoRecording(double[] temps)
    {
        if (ConstPowerValue == 0 && _pidOutputQueue.Count > 0)
            ConstPowerValue = (int)Math.Round(_pidOutputQueue.Average());

        if (Mode == TestMode.Standard60Min)
        {
            if (ElapsedSeconds >= _checkPointSeconds)
            {
                CheckTerminationCriteria();
                _checkPointSeconds += 300;
            }

            if (ElapsedSeconds >= 3600)
            {
                AddMessage("记录时间到达 3600 秒，试验自动结束");
                TransitionTo(TestStates.Complete);
            }
        }
        else if (Mode == TestMode.FixedDuration)
        {
            if (CurrentTrial != null && ElapsedSeconds >= CurrentTrial.TargetDuration)
            {
                AddMessage($"自定义时长 {CurrentTrial.TargetDuration} 秒已到达，试验结束");
                TransitionTo(TestStates.Complete);
            }
        }
    }

    private void DoComplete()
    {
        // 等待 UI 保存
    }

    // ==================== 终止条件检查 ====================

    private void CheckTerminationCriteria()
    {
        var drift = TestMetrics.ComputeDrift(_temperatureHistory);
        double maxDrift = 2.0;

        if (drift.valid && Math.Abs(drift.slopeCPer10Min) <= maxDrift && ElapsedSeconds >= 300)
        {
            _terminatedEarly = true;
            TerminationReason = "满足提前终止条件";
            AddMessage("满足终止条件，试验结束");
            TransitionTo(TestStates.Complete);
        }
    }

    private void TransitionTo(TestStates newState)
    {
        var oldState = State;
        State = newState;
    }

    // ==================== 用户操作（Person A 调用） ====================

    /// <summary>开始升温：Idle → Preparing</summary>
    public bool StartHeating()
    {
        if (State != TestStates.Idle) return false;
        ResetTrialState();
        TransitionTo(TestStates.Preparing);
        AddMessage("开始升温，系统升温中");
        return true;
    }

    /// <summary>停止加热：Preparing / Ready / Complete → Idle</summary>
    public bool StopHeating()
    {
        if (State != TestStates.Preparing && State != TestStates.Ready && State != TestStates.Complete)
            return false;
        TransitionTo(TestStates.Idle);
        _stableTickCount = 0;
        IsStable = false;
        return true;
    }

    /// <summary>开始记录：Ready → Recording</summary>
    public bool StartRecording()
    {
        if (State != TestStates.Ready) return false;
        if (IsCompleteUnsaved) return false;

        TransitionTo(TestStates.Recording);
        ElapsedSeconds = 0;
        _checkPointSeconds = 1800;
        _terminatedEarly = false;
        TerminationReason = string.Empty;

        if (_pidOutputQueue.Count > 0)
            ConstPowerValue = (int)Math.Round(_pidOutputQueue.Average());

        AddMessage("开始记录，计时开始");
        return true;
    }

    /// <summary>停止记录：Recording → Complete / Preparing</summary>
    public bool StopRecording()
    {
        if (State != TestStates.Recording) return false;

        if (ElapsedSeconds > 0)
        {
            AddMessage("用户手动停止记录");
            TransitionTo(TestStates.Complete);
            return true;
        }
        else
        {
            TransitionTo(TestStates.Preparing);
            return true;
        }
    }

    /// <summary>新建试验时调用，重置所有运行时状态</summary>
    public void ResetTrialState()
    {
        ElapsedSeconds = 0;
        _stableTickCount = 0;
        IsStable = false;
        _checkPointSeconds = 1800;
        _terminatedEarly = false;
        TerminationReason = string.Empty;
        _pidOutputQueue.Clear();
        _temperatureHistory.Clear();
        _pendingMessages.Clear();
        ConstPowerValue = 0;
    }

    // ==================== 计时（DaqWorker 按秒调用） ====================

    /// <summary>每秒递增计时，并触发 CSV 回调</summary>
    public void TickSecond()
    {
        if (State != TestStates.Recording) return;

        ElapsedSeconds++;
        if (CurrentTrial != null)
            CurrentTrial.TotalTestTime = ElapsedSeconds;

        // 驱动 Person C 的 CSV 写入回调
        // （DaqWorker 在 TickSecond 之后传入当前温度）
    }

    // ==================== 消息 & 事件广播 ====================

    public void AddMessage(string message)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        _pendingMessages.Add(new MasterMessage(time, message));
    }

    private void OnDataBroadcast(double[] temperatures)
    {
        var handler = DataBroadcast;
        if (handler == null) return;

        var drift = TestMetrics.ComputeDrift(_temperatureHistory);

        var args = new DataBroadcastEventArgs
        {
            Temperatures = (double[])temperatures.Clone(),
            State = State,
            Messages = new List<MasterMessage>(_pendingMessages),
            ElapsedSeconds = ElapsedSeconds,
            IsStable = IsStable,
            TemperatureDrift = drift.slopeCPer10Min,
            ProductId = CurrentProductId,
            TestId = CurrentTestId,
        };

        _pendingMessages.Clear();
        handler(this, args);
    }

    // ==================== 温度历史访问 ====================

    public List<double[]> GetRecentTemperatures(int count)
    {
        if (_temperatureHistory.Count <= count)
            return new List<double[]>(_temperatureHistory);
        return _temperatureHistory.Skip(_temperatureHistory.Count - count).ToList();
    }

    public List<double[]> GetAllTemperatures()
    {
        return new List<double[]>(_temperatureHistory);
    }
}
