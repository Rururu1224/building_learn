namespace BuildingLearn.Data.Models;

/// <summary>
/// 设备信息表
/// </summary>
public class Apparatus
{
    public string ApparatusId { get; set; } = string.Empty;
    public string ApparatusName { get; set; } = string.Empty;
    public string ComPort { get; set; } = string.Empty;
    public int BaudRate { get; set; }
    public int ConstPower { get; set; }
    public DateTime CalibrationDate { get; set; }
    public DateTime NextCalibrationDate { get; set; }
}
