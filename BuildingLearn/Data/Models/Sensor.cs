namespace BuildingLearn.Data.Models;

/// <summary>
/// 传感器配置表
/// </summary>
public class Sensor
{
    public int ChannelId { get; set; }         // 1=TF1, 2=TF2, 3=TS, 4=TC, 5=TCal
    public string ChannelName { get; set; } = string.Empty;
    public double RangeMin { get; set; }
    public double RangeMax { get; set; }
    public string Unit { get; set; } = "°C";
    public string ModbusAddress { get; set; } = string.Empty;
}
