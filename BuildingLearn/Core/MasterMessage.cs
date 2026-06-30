namespace BuildingLearn.Core;

/// <summary>
/// 系统消息
/// </summary>
public class MasterMessage
{
    /// <summary>消息时间，格式 HH:mm:ss</summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>消息内容</summary>
    public string Message { get; set; } = string.Empty;

    public MasterMessage() { }

    public MasterMessage(string time, string message)
    {
        Time = time;
        Message = message;
    }
}
