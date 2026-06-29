namespace BuildingLearn.Data.Models;

/// <summary>
/// 样品信息表
/// </summary>
public class ProductMaster
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public double Height { get; set; }
    public double Diameter { get; set; }
    public DateTime CreateTime { get; set; }
}
