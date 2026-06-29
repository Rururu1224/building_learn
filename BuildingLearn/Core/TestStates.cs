namespace BuildingLearn.Core;

/// <summary>
/// 试验状态枚举
/// </summary>
public enum TestStates
{
    /// <summary>空闲</summary>
    Idle,
    /// <summary>升温中</summary>
    Preparing,
    /// <summary>就绪（温度已稳定）</summary>
    Ready,
    /// <summary>记录中</summary>
    Recording,
    /// <summary>完成</summary>
    Complete
}

/// <summary>
/// 试验时长模式
/// </summary>
public enum TestMode
{
    /// <summary>标准60分钟模式</summary>
    Standard60Min,
    /// <summary>固定时长模式（自定义分钟）</summary>
    FixedDuration
}
