namespace BuildingLearn.Core;

/// <summary>
/// Person B 仿真参数 POCO — 零外部依赖，仅承载仿真引擎 & 状态机所需的配置值。
/// 由 Person C 的 ConfigService 读出后填充，通过 AppGlobal 胶水层注入。
/// </summary>
public class SensorSimulationConfig
{
    /// <summary>初始炉温 (°C)，默认 720</summary>
    public double InitialFurnaceTemp { get; set; } = 720.0;

    /// <summary>目标炉温 (°C)，默认 750</summary>
    public double TargetFurnaceTemp { get; set; } = 750.0;

    /// <summary>升温速率 (°C/s)，默认 40</summary>
    public double HeatingRatePerSecond { get; set; } = 40.0;

    /// <summary>温度波动幅度 (°C)，默认 0.5</summary>
    public double TempFluctuation { get; set; } = 0.5;

    /// <summary>稳定阈值 (°C)，默认 3.0（即 745~755 视为稳定）</summary>
    public double StableThreshold { get; set; } = 3.0;
}
