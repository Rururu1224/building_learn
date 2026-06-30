using Microsoft.Extensions.Configuration;

namespace BuildingLearn.Services;

/// <summary>
/// 读取 appsettings.json 配置的服务
/// </summary>
public class ConfigService
{
    private readonly IConfiguration _configuration;

    public ConfigService()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        _configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    // === Database ===
    public string DatabaseProvider => _configuration["Database:Provider"] ?? "Sqlite";
    public string SqlitePath => _configuration["Database:SqlitePath"] ?? "Data\\ISO11820.db";

    // === Hardware ===
    public int ConstPower => int.TryParse(_configuration["Hardware:ConstPower"], out var v) ? v : 2048;
    public int PidTemperature => int.TryParse(_configuration["Hardware:PidTemperature"], out var v) ? v : 750;

    // === Simulation ===
    public bool EnableSimulation => bool.TryParse(_configuration["Simulation:EnableSimulation"], out var v) ? v : true;
    public bool SimulateSensors => bool.TryParse(_configuration["Simulation:SimulateSensors"], out var v) ? v : true;
    public double InitialFurnaceTemp => double.TryParse(_configuration["Simulation:InitialFurnaceTemp"], out var v) ? v : 720.0;
    public double TargetFurnaceTemp => double.TryParse(_configuration["Simulation:TargetFurnaceTemp"], out var v) ? v : 750.0;
    public double HeatingRatePerSecond => double.TryParse(_configuration["Simulation:HeatingRatePerSecond"], out var v) ? v : 40.0;
    public double TempFluctuation => double.TryParse(_configuration["Simulation:TempFluctuation"], out var v) ? v : 0.5;
    public double StableThreshold => double.TryParse(_configuration["Simulation:StableThreshold"], out var v) ? v : 3.0;

    // === File Storage ===
    public string BaseDirectory => _configuration["FileStorage:BaseDirectory"] ?? "D:\\ISO11820";
    public string TestDataDirectory => _configuration["FileStorage:TestDataDirectory"] ?? "D:\\ISO11820\\TestData";

    // === Report ===
    public string OutputDirectory => _configuration["Report:OutputDirectory"] ?? "D:\\ISO11820\\Reports";
    public bool EnablePdfExport => bool.TryParse(_configuration["Report:EnablePdfExport"], out var v) ? v : true;

    // === Logging ===
    public string LogDirectory => _configuration["Logging:LogDirectory"] ?? "Logs";
    public int RetainedFileCountLimit => int.TryParse(_configuration["Logging:RetainedFileCountLimit"], out var v) ? v : 30;
}
