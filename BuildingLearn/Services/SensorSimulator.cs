using BuildingLearn.Core;

namespace BuildingLearn.Services;

/// <summary>
/// Person B: 5通道温度仿真引擎。
/// 每 800ms 执行一次 Update()，按当前试验状态生成 5 个通道的温度。
/// 零外部依赖 — 配置通过 SensorSimulationConfig POCO 注入。
/// </summary>
public class SensorSimulator
{
    private readonly SensorSimulationConfig _cfg;
    private readonly Random _rng = new();

    // 当前温度值
    public double TF1 { get; private set; }
    public double TF2 { get; private set; }
    public double TS { get; private set; }
    public double TC { get; private set; }
    public double TCal { get; private set; }

    // 恒功率模式标记
    public bool ConstPowerMode { get; set; }

    public SensorSimulator(SensorSimulationConfig config)
    {
        _cfg = config;

        TF1 = config.InitialFurnaceTemp;
        TF2 = config.InitialFurnaceTemp;
        TS = TF1 * 0.3;
        TC = TF1 * 0.25;
        TCal = TF1;
    }

    /// <summary>
    /// 每 800ms 调用一次，更新 5 个通道温度。
    /// </summary>
    /// <param name="state">当前试验状态</param>
    /// <param name="isHeating">是否在加热（非 Idle 态即为加热或保温）</param>
    public void Update(TestStates state, bool isHeating)
    {
        if (!isHeating && state == TestStates.Idle)
        {
            // ===== 降温阶段 =====
            TF1 -= 0.5 + Noise() * 0.1;
            if (TF1 < 25) TF1 = 25;
            TF2 -= 0.5 + Noise() * 0.1;
            if (TF2 < 25) TF2 = 25;
            TS = TF1 * 0.3 + Noise();
            TC = TF1 * 0.25 + Noise();
            TCal = TF1 + Noise() * 2;
            return;
        }

        if (state == TestStates.Idle || state == TestStates.Preparing)
        {
            // ===== 升温阶段 =====
            if (TF1 < _cfg.TargetFurnaceTemp - _cfg.StableThreshold)
            {
                TF1 += _cfg.HeatingRatePerSecond * 0.8 + Noise();
                TF2 += _cfg.HeatingRatePerSecond * 0.8 + Noise();
                TS = TF1 * 0.3 + Noise();
                TC = TF1 * 0.25 + Noise();
                TCal = TF1 + Noise() * 2;
            }
            else
            {
                // ===== 稳定阶段 (TF1 >= 目标 - 阈值，约 747°C) =====
                TF1 = _cfg.TargetFurnaceTemp + Noise();
                TF2 = _cfg.TargetFurnaceTemp + Noise();
                TS = TF1 * 0.3 + Noise();
                TC = TF1 * 0.25 + Noise();
                TCal = TF1 + Noise() * 2;
            }
        }
        else if (state == TestStates.Ready)
        {
            // ===== 就绪阶段：保持稳定 =====
            TF1 = _cfg.TargetFurnaceTemp + Noise();
            TF2 = _cfg.TargetFurnaceTemp + Noise();
            TS = TF1 * 0.3 + Noise();
            TC = TF1 * 0.25 + Noise();
            TCal = TF1 + Noise() * 2;
        }
        else if (state == TestStates.Recording)
        {
            // ===== 记录阶段：炉温稳定，TS/TC 指数趋近 =====
            TF1 = _cfg.TargetFurnaceTemp + Noise();
            TF2 = _cfg.TargetFurnaceTemp + Noise();

            double surfaceTarget = Math.Min(TF1 * 0.95, 800);
            TS += (surfaceTarget - TS) * 0.02 + Noise();

            double centerTarget = Math.Min(TF1 * 0.85, 750);
            TC += (centerTarget - TC) * 0.01 + Noise();

            TCal = TF1 + Noise() * 2;
        }
        else if (state == TestStates.Complete)
        {
            // ===== 完成阶段：保持稳定 =====
            TF1 = _cfg.TargetFurnaceTemp + Noise();
            TF2 = _cfg.TargetFurnaceTemp + Noise();
            TS = Math.Min(TF1 * 0.95, 800);
            TC = Math.Min(TF1 * 0.85, 750);
            TCal = TF1 + Noise() * 2;
        }
    }

    /// <summary>获取当前 5 通道温度数组 [TF1, TF2, TS, TC, TCal]</summary>
    public double[] GetTemperatures()
    {
        return new[] { TF1, TF2, TS, TC, TCal };
    }

    /// <summary>随机噪声：Random(-1, 1) × TempFluctuation</summary>
    private double Noise()
    {
        return (_rng.NextDouble() * 2 - 1) * _cfg.TempFluctuation;
    }

    /// <summary>重置到初始状态</summary>
    public void Reset()
    {
        TF1 = _cfg.InitialFurnaceTemp;
        TF2 = _cfg.InitialFurnaceTemp;
        TS = TF1 * 0.3;
        TC = TF1 * 0.25;
        TCal = TF1;
        ConstPowerMode = false;
    }
}
