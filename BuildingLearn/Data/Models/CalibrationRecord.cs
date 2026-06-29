namespace BuildingLearn.Data.Models;

/// <summary>
/// 校准历史记录表
/// </summary>
public class CalibrationRecord
{
    public int Id { get; set; }
    public string CalibrationDate { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string ApparatusId { get; set; } = string.Empty;
    public double ReferenceTemp { get; set; }
    public double MeasuredTemp { get; set; }
    public double Deviation { get; set; }
    public string Remark { get; set; } = string.Empty;
}
