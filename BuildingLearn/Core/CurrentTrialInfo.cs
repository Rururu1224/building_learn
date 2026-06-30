namespace BuildingLearn.Core;

/// <summary>
/// Person B 当前试验信息 POCO — 仅承载状态机需要感知的试验上下文。
/// 由 Person A/C 在新建试验 / 恢复试验时填充，通过 TestMaster.SetCurrentTrial() 注入。
/// </summary>
public class CurrentTrialInfo
{
    public string ProductId { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;

    public double AmbientTemp { get; set; }
    public double AmbientHumidity { get; set; }

    public double PreWeight { get; set; }

    /// <summary>试验模式："Standard60Min" 或 "FixedDuration"</summary>
    public string TestMode { get; set; } = "Standard60Min";

    /// <summary>目标时长（秒），标准模式=3600</summary>
    public int TargetDuration { get; set; } = 3600;

    /// <summary>试验后质量（克），Person A 保存后填入</summary>
    public double PostWeight { get; set; }

    /// <summary>标志位 "10000000"=已完成并保存</summary>
    public string Flag { get; set; } = "00000000";

    /// <summary>总记录时长（秒）</summary>
    public int TotalTestTime { get; set; }

    /// <summary>设备编号</summary>
    public string ApparatusId { get; set; } = string.Empty;

    /// <summary>设备名称</summary>
    public string ApparatusName { get; set; } = string.Empty;
}
