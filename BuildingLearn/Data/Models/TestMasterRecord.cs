namespace BuildingLearn.Data.Models;

/// <summary>
/// 试验主表（核心表）
/// </summary>
public class TestMasterRecord
{
    public string ProductId { get; set; } = string.Empty;
    public string TestId { get; set; } = string.Empty;
    public string TestDate { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string ApparatusId { get; set; } = string.Empty;
    public string ApparatusName { get; set; } = string.Empty;

    // 环境信息
    public double AmbientTemp { get; set; }
    public double AmbientHumidity { get; set; }

    // 样品信息
    public string ProductName { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public double Height { get; set; }
    public double Diameter { get; set; }

    // 质量信息
    public double PreWeight { get; set; }
    public double PostWeight { get; set; }
    public double LostWeight { get; set; }
    public double LostWeightPer { get; set; }   // 失重率 %

    // 温度数据
    public double FinalTF1 { get; set; }
    public double FinalTF2 { get; set; }
    public double FinalTS { get; set; }
    public double FinalTC { get; set; }
    public double Deltatf { get; set; }          // 综合温升 °C
    public double DeltaTF1 { get; set; }
    public double DeltaTF2 { get; set; }
    public double DeltaTS { get; set; }
    public double DeltaTC { get; set; }

    // 火焰现象
    public bool HasFlame { get; set; }
    public int FlameStartTime { get; set; }     // 秒
    public int FlameDuration { get; set; }       // 秒

    // 试验模式与时长
    public string TestMode { get; set; } = "Standard60Min";
    public int TotalTestTime { get; set; }
    public int TargetDuration { get; set; }      // 自定义时长（秒）

    // 恒功率值
    public int ConstPowerValue { get; set; }

    // 状态标记: "10000000" = 已完成并保存
    public string Flag { get; set; } = "00000000";

    // 备注
    public string Remark { get; set; } = string.Empty;

    // 附加信息
    public DateTime CalibrationDate { get; set; }
    public string DataFilePath { get; set; } = string.Empty;
}
