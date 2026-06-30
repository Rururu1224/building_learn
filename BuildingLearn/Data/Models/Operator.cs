namespace BuildingLearn.Data.Models;

/// <summary>
/// 操作员表
/// </summary>
public class Operator
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Pwd { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // admin / experimenter
}
