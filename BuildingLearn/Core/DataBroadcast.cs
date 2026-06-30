using BuildingLearn.Core;

namespace BuildingLearn.Core;

/// <summary>
/// 数据广播事件参数 — 后台线程向 UI 推送数据
/// </summary>
public class DataBroadcastEventArgs : EventArgs
{
    /// <summary>5通道温度值 [TF1, TF2, TS, TC, TCal]</summary>
    public double[] Temperatures { get; set; } = new double[5];

    /// <summary>当前状态</summary>
    public TestStates State { get; set; }

    /// <summary>本批次系统消息</summary>
    public List<MasterMessage> Messages { get; set; } = new();

    /// <summary>已记录秒数</summary>
    public int ElapsedSeconds { get; set; }

    /// <summary>温度是否稳定</summary>
    public bool IsStable { get; set; }

    /// <summary>温度漂移 (°C/10min)</summary>
    public double TemperatureDrift { get; set; }

    /// <summary>当前样品编号</summary>
    public string ProductId { get; set; } = string.Empty;

    /// <summary>当前试验标识</summary>
    public string TestId { get; set; } = string.Empty;
}
